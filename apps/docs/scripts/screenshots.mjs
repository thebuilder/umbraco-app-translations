// Takes the documentation's backoffice screenshots from a running sample site, so they can be
// retaken whenever the UI changes. Start the sample first:
//
//   dotnet run --project samples/TheBuilder.AppTranslations.Example
//
// then run `pnpm docs:screenshots` (or set UMBRACO_URL for a sample on another address). The script
// signs in with the sample's own unattended login, gives the site the same small set of content
// every time, and writes the images to content/screenshots/.
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { chromium } from "playwright";
import sharp from "sharp";

const umbracoUrl = (process.env.UMBRACO_URL ?? "https://localhost:44389").replace(/\/$/, "");
const docsRoot = fileURLToPath(new URL("../", import.meta.url));
const sampleSettings = JSON.parse(
  readFileSync(
    fileURLToPath(new URL("../../../samples/TheBuilder.AppTranslations.Example/appsettings.json", import.meta.url)),
    "utf8"
  )
);
const login = sampleSettings.Umbraco.CMS.Unattended;

const api = `${umbracoUrl}/umbraco/management/api/v1`;
const sourceAlias = "website";

const browser = await chromium.launch();
const context = await browser.newContext({
  ignoreHTTPSErrors: true,
  viewport: { width: 1440, height: 760 },
  deviceScaleFactor: 2,
  colorScheme: "light",
});
const page = await context.newPage();

// The backoffice sends a bearer token with every management request. Borrowing it is simpler than
// running the OAuth flow a second time.
let authorization;
page.on("request", (request) => {
  const header = request.headers().authorization;
  if (header && request.url().startsWith(api)) authorization = header;
});

try {
  await page.goto(`${umbracoUrl}/umbraco`);
} catch (error) {
  console.error(`Could not reach ${umbracoUrl}. Start the sample site first, or set UMBRACO_URL.`);
  throw error;
}
await page.locator("#username-input").fill(login.UnattendedUserEmail);
await page.locator("#password-input").fill(login.UnattendedUserPassword);
await page.keyboard.press("Enter");
await page.waitForURL(/\/section\//, { timeout: 60_000 });
for (let attempt = 0; !authorization && attempt < 50; attempt++) await page.waitForTimeout(200);
if (!authorization) throw new Error("Signed in, but saw no management API request to borrow a token from.");

const call = async (method, path, body) => {
  const response = await page.request.fetch(`${api}${path}`, {
    method,
    headers: { Authorization: authorization, "Content-Type": "application/json" },
    data: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!response.ok()) throw new Error(`${method} ${path} answered ${response.status()}: ${await response.text()}`);
  const text = await response.text();
  return text ? JSON.parse(text) : undefined;
};

// The same content every run: Danish next to English, the sample's messages, and one edit.
const languages = await call("GET", "/language?skip=0&take=100");
if (!languages.items.some((language) => language.isoCode === "da-DK")) {
  await call("POST", "/language", { isoCode: "da-DK", name: "Danish", isDefault: false, isMandatory: false });
}
const sources = await call("GET", "/app-translations/sources");
const source =
  sources.find((candidate) => candidate.alias === sourceAlias) ??
  (await call("POST", "/app-translations/sources", {
    alias: sourceAlias,
    displayName: "Website",
    enabled: true,
    endpointTemplate: `${umbracoUrl}/sample/messages/{locale}.json`,
    timeoutSeconds: 30,
    maximumResponseBytes: 5_242_880,
    locales: ["en-US", "da-DK"],
    namespace: sourceAlias,
    namespaceMode: "Fixed",
    messageFormat: "Icu",
    headers: [],
    sourceFormat: "NestedJson",
  }));
await call("POST", `/app-translations/sources/${source.id}/sync`);
await call("PUT", "/app-translations/messages/override", {
  sourceId: source.id,
  namespace: sourceAlias,
  key: "greeting",
  locale: "da-DK",
  value: "Hej {name}, godt at se dig igen!",
});

const settle = async () => {
  await page.waitForTimeout(2500);
  // Away from anything with a hover state.
  await page.mouse.move(5, page.viewportSize().height - 5);
};

// The Settings tree and the header take a third of the width in the settings shots, which leaves
// the dashboard's text small on a docs page, so those are cropped to the dashboard and its tabs.
const settingsDashboard = { x: 300, y: 60 };

const capture = async (name, height, crop) => {
  await page.setViewportSize({ width: 1440, height });
  await settle();
  const raw = await page.screenshot(
    crop ? { clip: { x: crop.x, y: crop.y, width: 1440 - crop.x, height: height - crop.y } } : undefined
  );
  const output = `${docsRoot}content/screenshots/${name}.png`;
  // A palette PNG keeps the UI's flat colours exact at a fraction of the size.
  await sharp(raw).png({ compressionLevel: 9, palette: true, quality: 90 }).toFile(output);
  console.log(`Wrote content/screenshots/${name}.png`);
};

// The editor, Danish against English, with the edited message open.
await page.goto(`${umbracoUrl}/umbraco/section/app-translations/view/overview?locale=da-DK&reference=en-US`);
await settle();
await page.getByText(`${sourceAlias}.greeting`).first().click();
await capture("editor", 700);

// Settings: the source, and the delivery URL for it.
await page.goto(`${umbracoUrl}/umbraco/section/settings/dashboard/app-translations`);
await capture("settings", 760, settingsDashboard);

// The source's settings.
await page.getByRole("button", { name: "Edit" }).first().click();
await capture("source-editor", 1000, settingsDashboard);

await browser.close();
