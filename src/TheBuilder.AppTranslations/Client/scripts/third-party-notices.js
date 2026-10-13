// Writes THIRD-PARTY-NOTICES.md from what the built backoffice bundle actually contains, so the
// notices name exactly the npm packages that ship inside the NuGet package. Run after `pnpm build`:
// it reads the source maps, which list every file the bundle was made from. `--check` fails instead
// of writing when the file is out of date, which is how CI keeps it from drifting.
import { existsSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const clientDirectory = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const bundleDirectory = resolve(
  clientDirectory,
  "../wwwroot/App_Plugins/TheBuilder.AppTranslations"
);
const noticesFile = resolve(clientDirectory, "../../../THIRD-PARTY-NOTICES.md");
const check = process.argv.includes("--check");

// The package directory a bundled source file came from: the last node_modules/<name> in its path,
// which under pnpm is the real package rather than a link to it. Matched on forward slashes, so
// paths are normalised first: on Windows, resolve() returns backslashes.
const packageDirectoryPattern = /^(.*\/node_modules\/(?:@[^/]+\/)?[^/]+)\//;

const mapFiles = existsSync(bundleDirectory)
  ? readdirSync(bundleDirectory)
      .filter((file) => file.endsWith(".js.map"))
      .sort()
  : [];
if (mapFiles.length === 0) {
  console.error(`No source maps in ${bundleDirectory}. Run pnpm build first.`);
  process.exit(1);
}

const packageDirectories = new Set();
for (const mapFile of mapFiles) {
  const map = JSON.parse(readFileSync(join(bundleDirectory, mapFile), "utf8"));
  for (const source of map.sources) {
    const match = packageDirectoryPattern.exec(
      resolve(bundleDirectory, source).replaceAll("\\", "/")
    );
    if (match) {
      packageDirectories.add(match[1]);
    }
  }
}

const licenseFilePattern = /^(licen[cs]e|copying)(\.(md|txt))?$/i;
const gitPrefix = /^git\+/;
const gitSuffix = /\.git$/;

// Sorted throughout, here and for license files below, because readdirSync's order differs between
// operating systems: a choice between two copies or two license files must not depend on it, or
// CI would disagree with the file a developer generated.
const packages = [...packageDirectories]
  .sort()
  .map((directory) => {
    const manifest = JSON.parse(readFileSync(join(directory, "package.json"), "utf8"));
    const licenseFile = readdirSync(directory)
      .sort()
      .find((file) => licenseFilePattern.test(file));
    const repository =
      typeof manifest.repository === "string" ? manifest.repository : manifest.repository?.url;
    return {
      name: manifest.name,
      license: manifest.license ?? "UNKNOWN",
      url: manifest.homepage ?? repository?.replace(gitPrefix, "").replace(gitSuffix, ""),
      text: licenseFile ? readFileSync(join(directory, licenseFile), "utf8").trim() : undefined,
    };
  })
  // One entry per package name: two copies of a package in the bundle share one notice.
  .filter((entry, index, all) => all.findIndex((other) => other.name === entry.name) === index)
  // Ordinal, not localeCompare: the order must not depend on the machine's locale, or CI would
  // disagree with the file a developer generated.
  .sort((a, b) => (a.name < b.name ? -1 : 1));

const missing = packages.filter((entry) => !entry.text);
if (missing.length > 0) {
  console.error(`No license file found for: ${missing.map((entry) => entry.name).join(", ")}.`);
  process.exit(1);
}

const notices = `# Third-party notices

TheBuilder.AppTranslations includes the following npm packages in its backoffice bundle
(\`App_Plugins/TheBuilder.AppTranslations\`). Their licenses follow.

The package's NuGet dependencies, Umbraco among them, are not included in it. NuGet installs them
separately, each with its own license.

This file is generated from the bundle by \`pnpm notices\`. Do not edit it by hand.

${packages
  .map(
    (entry) => `## ${entry.name}

License: ${entry.license}${entry.url ? `\\\nSource: ${entry.url}` : ""}

\`\`\`text
${entry.text}
\`\`\`
`
  )
  .join("\n")}`;

if (check) {
  const current = existsSync(noticesFile) ? readFileSync(noticesFile, "utf8") : "";
  if (current !== notices) {
    console.error(
      "THIRD-PARTY-NOTICES.md does not match the bundle. Run pnpm build, then pnpm notices, and commit the result."
    );
    process.exit(1);
  }
  console.log(`THIRD-PARTY-NOTICES.md lists the ${packages.length} bundled packages.`);
} else {
  writeFileSync(noticesFile, notices);
  console.log(`Wrote THIRD-PARTY-NOTICES.md with ${packages.length} bundled packages.`);
}
