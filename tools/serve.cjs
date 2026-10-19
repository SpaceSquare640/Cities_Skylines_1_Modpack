/* Author: OpenAI-Codex. Loopback-only preview, with optional GitHub Pages path prefix. */
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const prefix = '/Cities_Skylines_1_Modpack';
const types = {'.html':'text/html; charset=utf-8','.css':'text/css; charset=utf-8','.js':'text/javascript; charset=utf-8','.svg':'image/svg+xml','.md':'text/plain; charset=utf-8'};
function createPreviewServer() {
  return http.createServer((request,response) => {
    try {
      let pathname = decodeURIComponent(new URL(request.url,'http://127.0.0.1').pathname);
      if (pathname === prefix || pathname.startsWith(prefix + '/')) pathname = pathname.slice(prefix.length) || '/';
      const candidate = path.resolve(root, '.' + pathname);
      if (!candidate.startsWith(root + path.sep) && candidate !== root) throw new Error('Outside root');
      let file = candidate;
      if (fs.statSync(file).isDirectory()) file = path.join(file,'index.html');
      const real = fs.realpathSync(file);
      if (!real.startsWith(root + path.sep) || !types[path.extname(real)] || path.relative(root,real).split(path.sep).some(part=>part.startsWith('.'))) throw new Error('Not public');
      const body = fs.readFileSync(real);
      response.writeHead(200,{'Content-Type':types[path.extname(real)],'Content-Length':body.length,'Cache-Control':'no-store','X-Content-Type-Options':'nosniff'});
      response.end(body);
    } catch { response.writeHead(404,{'Content-Type':'text/plain'});response.end('Not found'); }
  });
}
module.exports = {createPreviewServer};
if (require.main === module) {
  const server = createPreviewServer();
  const port = Number(process.argv[2] || 4173);
  if (!Number.isInteger(port) || port < 1024 || port > 65535) throw new Error('Choose a port between 1024 and 65535.');
  server.listen(port,'127.0.0.1',()=>console.log(`OpenAI-Codex preview: http://127.0.0.1:${port}/Cities_Skylines_1_Modpack/`));
  server.on('error',error=>{console.error(error.message);process.exitCode=1;});
}
