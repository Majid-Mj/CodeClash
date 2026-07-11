// docker-proxy.js
// Sits between cloudflared and the local Docker daemon to resolve the
// "starting container with non-empty request body" error caused by
// Cloudflare tunnel translating empty POSTs into chunked requests.

const http = require('http');

const TARGET_PORT = 2375;
const PROXY_PORT = 2377;

const server = http.createServer((req, res) => {
    const isStartRequest = req.url.includes('/containers/') && req.url.endsWith('/start') && req.method === 'POST';

    const options = {
        hostname: '127.0.0.1',
        port: TARGET_PORT,
        path: req.url,
        method: req.method,
        headers: { ...req.headers }
    };

    if (isStartRequest) {
        console.log(`[Proxy] Intercepted container start: ${req.url}. Forcing empty body.`);
        delete options.headers['transfer-encoding'];
        options.headers['content-length'] = '0';
    }

    const proxyReq = http.request(options, (proxyRes) => {
        res.writeHead(proxyRes.statusCode, proxyRes.headers);
        proxyRes.pipe(res);
    });

    proxyReq.on('error', (err) => {
        console.error(`[Proxy] Target request error: ${err.message}`);
        res.writeHead(502);
        res.end('Bad Gateway');
    });

    if (isStartRequest) {
        proxyReq.end();
    } else {
        req.pipe(proxyReq);
    }
});

server.listen(PROXY_PORT, '127.0.0.1', () => {
    console.log(`Docker proxy listening on http://127.0.0.1:${PROXY_PORT} -> ${TARGET_PORT}`);
});
