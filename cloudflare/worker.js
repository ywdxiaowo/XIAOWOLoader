const ALLOWED_HOSTS = new Set([
  "github.com",
  "raw.githubusercontent.com",
]);
const REDIRECT_STATUSES = new Set([301, 302, 303, 307, 308]);

function isAllowedHost(hostname) {
  const normalized = hostname.toLowerCase();
  return ALLOWED_HOSTS.has(normalized) || normalized.endsWith(".githubusercontent.com");
}

function json(body, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: {
      "content-type": "application/json; charset=utf-8",
      "cache-control": "no-store",
    },
  });
}

export default {
  async fetch(request) {
    if (request.method !== "GET" && request.method !== "HEAD") {
      return json({ error: "method_not_allowed" }, 405);
    }

    const incoming = new URL(request.url);
    if (incoming.pathname === "/" || incoming.pathname === "/health") {
      return json({ service: "XIAOWOLoader GitHub download gateway", status: "ok" });
    }

    const path = incoming.pathname.slice(1);
    const separator = path.indexOf("/");
    const hostname = separator === -1 ? path : path.slice(0, separator);
    const upstreamPath = separator === -1 ? "/" : path.slice(separator);

    if (!isAllowedHost(hostname)) {
      return json({ error: "host_not_allowed" }, 403);
    }

    const upstream = new URL(`https://${hostname}${upstreamPath}`);
    upstream.search = incoming.search;

    const headers = new Headers();
    for (const name of ["accept", "accept-encoding", "if-modified-since", "if-none-match", "range"]) {
      const value = request.headers.get(name);
      if (value) headers.set(name, value);
    }
    headers.set("user-agent", "XIAOWOLoader-CDN/1.0");

    let response;
    try {
      for (let redirectCount = 0; redirectCount <= 5; redirectCount++) {
        response = await fetch(upstream, {
          method: request.method,
          headers,
          redirect: "manual",
          cf: {
            cacheEverything: true,
            cacheTtlByStatus: {
              "200-299": 86400,
              "404": 60,
              "500-599": 0,
            },
          },
        });

        if (!REDIRECT_STATUSES.has(response.status)) break;

        const location = response.headers.get("location");
        if (!location || redirectCount === 5) {
          return json({ error: "invalid_upstream_redirect" }, 502);
        }

        const next = new URL(location, upstream);
        if (next.protocol !== "https:" || !isAllowedHost(next.hostname)) {
          return json({ error: "redirect_host_not_allowed" }, 502);
        }

        if (response.body) await response.body.cancel();
        upstream.href = next.href;
      }
    } catch {
      return json({ error: "upstream_unavailable" }, 502);
    }

    if (!response) return json({ error: "upstream_unavailable" }, 502);

    const outgoingHeaders = new Headers(response.headers);
    outgoingHeaders.set("access-control-allow-origin", "*");
    outgoingHeaders.set("x-content-type-options", "nosniff");
    outgoingHeaders.delete("set-cookie");

    return new Response(response.body, {
      status: response.status,
      statusText: response.statusText,
      headers: outgoingHeaders,
    });
  },
};
