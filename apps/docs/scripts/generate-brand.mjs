import { fileURLToPath } from "node:url";
import sharp from "sharp";

// The favicon is drawn from the logo, so the two cannot drift apart.
const docsRoot = fileURLToPath(new URL("../", import.meta.url));

await sharp(`${docsRoot}public/logo-mark.svg`).resize(256, 256).png().toFile(`${docsRoot}favicon.png`);

console.log("Generated favicon.png from public/logo-mark.svg.");
