import { readFile, writeFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import openapiTS, { astToString } from "openapi-typescript";

const schemaUrl = new URL("../openapi/rudoger-v1.json", import.meta.url);
const outputUrl = new URL("../src/api/generated/schema.d.ts", import.meta.url);
const checkOnly = process.argv.includes("--check");
const ast = await openapiTS(schemaUrl);
const generated = astToString(ast);

if (checkOnly) {
  let current;
  try {
    current = await readFile(outputUrl, "utf8");
  } catch {
    throw new Error(`Generated OpenAPI types are missing: ${fileURLToPath(outputUrl)}`);
  }

  if (current !== generated) {
    throw new Error("Generated OpenAPI types are stale. Run npm run api:generate.");
  }
} else {
  await writeFile(outputUrl, generated, "utf8");
}
