import { once } from "node:events";
import { readFile } from "node:fs/promises";
import { createServer } from "node:http";
import { extname } from "node:path";

const contentTypes = new Map([
    [".html", "text/html; charset=utf-8"],
    [".js", "text/javascript; charset=utf-8"],
    [".mjs", "text/javascript; charset=utf-8"],
    [".json", "application/json; charset=utf-8"],
    [".map", "application/json; charset=utf-8"],
]);

export async function withStaticServer({ pages = {}, files = {}, mounts = {} }, action) {
    const server = createServer(async (request, response) => {
        try {
            const pathname = new URL(request.url, "http://127.0.0.1").pathname;
            const type = contentTypes.get(extname(pathname));

            if (request.method !== "GET" || !type) {
                response.writeHead(404).end();
                return;
            }

            if (Object.hasOwn(pages, pathname)) {
                response.writeHead(200, { "Content-Type": type }).end(pages[pathname]);
                return;
            }

            let file = Object.hasOwn(files, pathname) ? files[pathname] : undefined;

            for (const [prefix, directory] of Object.entries(mounts)) {
                if (file || !pathname.startsWith(prefix)) continue;

                const candidate = new URL(pathname.slice(prefix.length), directory);
                if (candidate.href.startsWith(directory.href)) file = candidate;
            }

            if (!file) {
                response.writeHead(404).end();
                return;
            }

            const content = await readFile(file);
            response.writeHead(200, { "Content-Type": type }).end(content);
        } catch {
            response.writeHead(404).end();
        }
    });
    server.listen(0, "127.0.0.1");
    await once(server, "listening");

    try {
        return await action("http://127.0.0.1:" + server.address().port);
    } finally {
        await new Promise((resolve, reject) => server.close(error => error ? reject(error) : resolve()));
    }
}
