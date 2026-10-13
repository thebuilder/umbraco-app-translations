import { readFileSync } from "node:fs";
import react from "@vitejs/plugin-react";
import { defineConfig, type Plugin } from "vite";

/**
 * Writes umbraco-package.json with the package's version, so the manifest Umbraco reads and the
 * NuGet package always agree. A release passes the version from its tag as PACKAGE_VERSION; a local
 * build uses the client's package.json.
 */
const packageManifest = (): Plugin => {
  const read = (file: string) => JSON.parse(readFileSync(new URL(file, import.meta.url), "utf8"));
  const version: string = process.env.PACKAGE_VERSION ?? read("./package.json").version;
  return {
    name: "package-manifest",
    generateBundle() {
      this.emitFile({
        type: "asset",
        fileName: "umbraco-package.json",
        source: `${JSON.stringify({ ...read("./umbraco-package.json"), version }, null, 2)}\n`,
      });
    },
  };
};

const rejectNodeEnvironmentReferences = (): Plugin => ({
  name: "reject-node-environment-references",
  generateBundle(_, bundle) {
    for (const output of Object.values(bundle)) {
      if (output.type === "chunk" && output.code.includes("process.env.")) {
        this.error(
          `Browser bundle ${output.fileName} contains an unresolved process.env reference.`
        );
      }
    }
  },
});

export default defineConfig({
  define: {
    "process.env.NODE_ENV": JSON.stringify("production"),
  },
  plugins: [react(), rejectNodeEnvironmentReferences(), packageManifest()],
  build: {
    lib: { entry: "src/bundle.manifests.ts", formats: ["es"], fileName: "translations" },
    outDir: "../wwwroot/App_Plugins/TheBuilder.AppTranslations",
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: { external: [/^@umbraco/] },
  },
});
