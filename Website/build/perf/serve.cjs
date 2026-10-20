#!/usr/bin/env node
// A minimal static server for the rendering budget in CI: serves a built site folder on localhost.
//   node Website/build/perf/serve.cjs Website/_site 8080
'use strict';

const fs = require('fs');
const http = require('http');
const path = require('path');

const root = path.resolve(process.argv[2] || '_site');
const port = parseInt(process.argv[3] || '8080', 10);
const types = {
    '.html': 'text/html; charset=utf-8', '.css': 'text/css', '.js': 'text/javascript', '.json': 'application/json',
    '.svg': 'image/svg+xml', '.png': 'image/png', '.jpg': 'image/jpeg', '.webp': 'image/webp', '.ico': 'image/x-icon',
    '.woff2': 'font/woff2', '.txt': 'text/plain; charset=utf-8', '.xml': 'application/xml', '.jsonl': 'application/x-ndjson'
};

http.createServer((request, response) => {
    let file;
    try {
        file = path.join(root, decodeURIComponent(new URL(request.url, 'http://localhost').pathname));
    } catch (e) {
        response.writeHead(400); response.end(); return;
    }
    if (file !== root && !file.startsWith(root + path.sep)) { response.writeHead(403); response.end(); return; }
    if (fs.existsSync(file) && fs.statSync(file).isDirectory()) file = path.join(file, 'index.html');
    if (!fs.existsSync(file) && fs.existsSync(file + '.html')) file += '.html';
    fs.readFile(file, (error, body) => {
        if (error) { response.writeHead(404); response.end('Not found'); return; }
        response.writeHead(200, { 'Content-Type': types[path.extname(file).toLowerCase()] || 'application/octet-stream' });
        response.end(body);
    });
}).listen(port, '127.0.0.1', () => console.log('Serving ' + root + ' on http://localhost:' + port));
