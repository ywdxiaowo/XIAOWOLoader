# XIAOWOLoader download gateway

This Cloudflare Worker proxies only GitHub download hosts used by MelonLoader. It is not a general-purpose proxy.

## Deploy

1. Install Node.js 20 or later.
2. Authenticate locally with `npx wrangler login`. Do not commit or share an API token.
3. From the repository root, run:

   ```powershell
   npx wrangler deploy --config cloudflare/wrangler.toml
   ```

Wrangler registers `cdn.ywdxiaowo.com` as a Worker Custom Domain. Cloudflare creates the DNS record and TLS certificate automatically. If that hostname already has a CNAME record, remove it before deploying.

Verify it after deployment:

```powershell
Invoke-RestMethod https://cdn.ywdxiaowo.com/health
```

The loader rewrites a supported upstream URL as follows:

```text
https://github.com/LavaGang/example/releases/download/v1/file.zip
https://cdn.ywdxiaowo.com/github.com/LavaGang/example/releases/download/v1/file.zip
```

The loader falls back to the original URL when this gateway is unavailable. To use another gateway, edit `UserData/Loader.cfg` or pass `--melonloader.mirror=https://example.com`.
