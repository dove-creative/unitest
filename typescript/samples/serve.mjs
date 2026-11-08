import { readFile } from "node:fs/promises";
import { createServer } from "node:http";

const pages = new Map([
    ["/samples/index.html", "text/html; charset=utf-8"],
    ["/samples/browser.mjs", "text/javascript; charset=utf-8"],
    ["/samples/scenarios.mjs", "text/javascript; charset=utf-8"],
]);

const server = createServer(async (request, response) => {
    const path = new URL(request.url, "http://127.0.0.1").pathname;
    const type = pages.get(path) ?? (/^\/dist\/[a-z-]+\.js$/.test(path) ? "text/javascript; charset=utf-8" : null);

    if (request.method !== "GET" || type === null) {
        response.writeHead(404).end();
        return;
    }

    try {
        const file = await readFile(new URL(`..${path}`, import.meta.url));
        response.writeHead(200, { "Content-Type": type }).end(file);
    } catch (error) {
        response.writeHead(error.code === "ENOENT" ? 404 : 500).end();
    }
});

server.listen(0, "127.0.0.1", () => {
    console.log(`UniTest samples: http://127.0.0.1:${server.address().port}/samples/index.html`);
});
