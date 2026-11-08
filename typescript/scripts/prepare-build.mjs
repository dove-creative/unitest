import { lstat, realpath, rm } from "node:fs/promises";
import { dirname } from "node:path";
import { fileURLToPath } from "node:url";

const packageRoot = await realpath(new URL("../", import.meta.url));
const output = fileURLToPath(new URL("../dist", import.meta.url));
const status = await lstat(output).catch(error => {
    if (error.code === "ENOENT") return undefined;
    throw error;
});

if (status) {
    if (!status.isDirectory() || status.isSymbolicLink() || dirname(await realpath(output)) !== packageRoot) {
        throw new Error("Unexpected build output directory.");
    }

    await rm(output, { recursive: true, force: true });
}
