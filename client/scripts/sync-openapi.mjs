import { mkdir, writeFile } from "node:fs/promises";
import { spawn } from "node:child_process";
import { fileURLToPath } from "node:url";

const source = process.env.OPENAPI_URL ?? "http://localhost:8080/openapi/v1.json";
const snapshotUrl = new URL("../openapi/rudoger-v1.json", import.meta.url);
const generatorUrl = new URL("./generate-openapi.mjs", import.meta.url);
const response = await fetch(source, { headers: { Accept: "application/json" } });

if (!response.ok) {
  throw new Error(
    `OpenAPI document could not be fetched: ${response.status} ${response.statusText}`,
  );
}

const document = await response.json();
if (
  typeof document !== "object" ||
  document === null ||
  !("openapi" in document) ||
  !("paths" in document)
) {
  throw new Error("The response is not an OpenAPI document.");
}

await mkdir(new URL("../openapi/", import.meta.url), { recursive: true });
await mkdir(new URL("../src/api/generated/", import.meta.url), { recursive: true });
await writeFile(snapshotUrl, `${JSON.stringify(document, null, 2)}\n`, "utf8");

await new Promise((resolve, reject) => {
  const child = spawn(process.execPath, [fileURLToPath(generatorUrl)], { stdio: "inherit" });
  child.once("error", reject);
  child.once("exit", (code) => {
    if (code === 0) {
      resolve();
      return;
    }
    reject(new Error(`OpenAPI type generation failed with exit code ${String(code)}.`));
  });
});
