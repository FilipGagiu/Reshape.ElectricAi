# Frontend PWA Deploy (EC2 Ubuntu + Nginx)

Handoff doc for ops/backend team. Deploys Angular PWA (`ec-hackaton`) as static files behind Nginx on the existing EC2 Ubuntu host that already runs the backend.

## Facts about the app

- Angular project name: `ec-hackaton` (see `frontend/angular.json`).
- PWA: enabled (`serviceWorker: "ngsw-config.json"`). Build emits `ngsw-worker.js`, `ngsw.json`, and `manifest.webmanifest` alongside the normal hashed bundles.
- Build output dir: `frontend/dist/ec-hackaton/browser/`.
- Build command: `npm ci && npm run build` (defaults to `--configuration production`).

## Placeholders to substitute

| Placeholder | Meaning | Example |
|---|---|---|
| `<REPO_DIR>` | Repo clone path on the box | `/opt/electricai/repo` |
| `<DOMAIN>` | Public domain for the frontend | `app.electricai.example.com` |
| `<BACKEND_PORT>` | Loopback port the .NET backend listens on | `5000` |
| `<WEBROOT>` | Where Nginx serves static files from | `/var/www/electricai` |
| `<DEPLOY_USER>` | Linux user owning the repo + webroot | `ubuntu` or `deploy` |

## One-time host setup

### 1. Install Node + Nginx + Certbot

```bash
# Node 20 LTS (match angular.json engines if pinned)
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt update
sudo apt install -y nodejs nginx certbot python3-certbot-nginx rsync

node --version   # expect v20.x
npm --version
nginx -v
```

### 2. Create webroot

```bash
sudo mkdir -p <WEBROOT>
sudo chown <DEPLOY_USER>:<DEPLOY_USER> <WEBROOT>
```

### 3. EC2 security group

Open inbound:
- `80/tcp` (HTTP → HTTPS redirect + Certbot challenge)
- `443/tcp` (HTTPS)

Keep backend port (`<BACKEND_PORT>`) closed to public — Nginx proxies it from loopback.

### 4. DNS

Point `<DOMAIN>` A record at the EC2 public IP (or Elastic IP). Verify with `dig <DOMAIN>` before running Certbot.

### 5. Nginx site

Write `/etc/nginx/sites-available/electricai`:

```nginx
server {
    listen 80;
    listen [::]:80;
    server_name <DOMAIN>;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name <DOMAIN>;

    # Certbot fills these in
    ssl_certificate     /etc/letsencrypt/live/<DOMAIN>/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/<DOMAIN>/privkey.pem;
    include /etc/letsencrypt/options-ssl-nginx.conf;
    ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem;

    root <WEBROOT>;
    index index.html;

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    gzip on;
    gzip_types text/plain text/css application/json application/javascript
               application/manifest+json text/xml application/xml image/svg+xml;
    gzip_min_length 1024;

    # Brotli if module available — optional

    # Backend API proxy (adjust path prefix to match your routing)
    location /api/ {
        proxy_pass http://127.0.0.1:<BACKEND_PORT>/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_read_timeout 300s;
    }

    # === PWA-critical cache rules ===
    # NEVER cache these. Service worker uses them to detect updates.
    # Caching them breaks updates permanently for returning users.
    location = /index.html {
        add_header Cache-Control "no-cache, no-store, must-revalidate" always;
        add_header Pragma "no-cache" always;
        expires 0;
    }
    location = /ngsw.json {
        add_header Cache-Control "no-cache, no-store, must-revalidate" always;
        expires 0;
    }
    location = /ngsw-worker.js {
        add_header Cache-Control "no-cache, no-store, must-revalidate" always;
        expires 0;
    }
    location = /safety-worker.js {
        add_header Cache-Control "no-cache, no-store, must-revalidate" always;
        expires 0;
    }
    location = /worker-basic.min.js {
        add_header Cache-Control "no-cache, no-store, must-revalidate" always;
        expires 0;
    }
    location = /manifest.webmanifest {
        types { } default_type application/manifest+json;
        add_header Cache-Control "no-cache" always;
    }

    # Hashed assets — safe to cache forever
    location ~* \.(?:js|css|woff2?|ttf|otf|eot|png|jpg|jpeg|gif|svg|webp|ico|map)$ {
        expires 1y;
        access_log off;
        add_header Cache-Control "public, immutable";
    }

    # SPA fallback — every non-file route returns index.html
    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

Enable + reload:

```bash
sudo ln -s /etc/nginx/sites-available/electricai /etc/nginx/sites-enabled/electricai
sudo nginx -t
sudo systemctl reload nginx
```

### 6. TLS via Certbot

```bash
sudo certbot --nginx -d <DOMAIN>
# Pick "redirect HTTP→HTTPS" if prompted (already in our config, harmless).
```

Auto-renew is installed by the Debian package (`systemctl status certbot.timer`). Verify with:

```bash
sudo certbot renew --dry-run
```

## Deploy script

Save as `<REPO_DIR>/scripts/deploy-frontend.sh`, `chmod +x`:

```bash
#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="<REPO_DIR>"
WEBROOT="<WEBROOT>"
APP_NAME="ec-hackaton"

cd "$REPO_DIR"
git fetch --prune
git checkout main
git pull --ff-only

cd "$REPO_DIR/frontend"
npm ci
npm run build  # outputs dist/$APP_NAME/browser/

# Atomic swap via rsync — keeps old files until new ones land
sudo rsync -a --delete \
    "dist/${APP_NAME}/browser/" \
    "${WEBROOT}/"

# Nginx config didn't change — no reload needed for static swap
echo "Frontend deployed: $(date -Iseconds)"
echo "Build hash:        $(git rev-parse --short HEAD)"
```

Run with:

```bash
/opt/electricai/repo/scripts/deploy-frontend.sh
```

## Memory note

EC2 t2.micro / t3.micro will OOM during `npm run build` (Angular prod build can hit 1.5+ GB). Options:

1. Use t3.small or larger for the deploy host.
2. Add swap (cheap fix):
   ```bash
   sudo fallocate -l 2G /swapfile
   sudo chmod 600 /swapfile
   sudo mkswap /swapfile
   sudo swapon /swapfile
   echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
   ```
3. Build in CI and upload `dist/` as an artifact; deploy script just rsyncs.

## Smoke test after first deploy

```bash
# 1. Static files served
curl -I https://<DOMAIN>/                 # 200, Content-Type: text/html
curl -I https://<DOMAIN>/ngsw.json        # 200, Cache-Control: no-cache
curl -I https://<DOMAIN>/ngsw-worker.js   # 200, Cache-Control: no-cache
curl -I https://<DOMAIN>/manifest.webmanifest  # Content-Type: application/manifest+json

# 2. SPA fallback works
curl -I https://<DOMAIN>/some/deep/route   # 200 (returns index.html, not 404)

# 3. Hashed asset cached
curl -I https://<DOMAIN>/main-XXXXXX.js    # Cache-Control: public, immutable

# 4. Backend reachable through proxy
curl -I https://<DOMAIN>/api/health        # whatever your health endpoint is

# 5. TLS grade
curl https://<DOMAIN>/ -v 2>&1 | grep -i "ssl\|tls"
```

In browser DevTools → Application → Service Workers: `ngsw-worker.js` should be `activated and running`. Application → Manifest should parse cleanly.

## Update flow (returning users)

1. User opens app → SW serves cached old version instantly (offline-first).
2. SW fetches new `ngsw.json` → sees hash change → downloads new hashed bundles.
3. New version activates on next full navigation, OR via in-app `SwUpdate.versionUpdates` prompt if the frontend implements one.

**Implication:** users see stale version once after each deploy. This is correct PWA behavior, not a bug. If you need instant updates, frontend must add an update banner that calls `SwUpdate.activateUpdate()` + reloads.

## Rollback

```bash
cd <REPO_DIR>
git checkout <PREVIOUS_COMMIT_SHA>
cd frontend
npm ci
npm run build
sudo rsync -a --delete "dist/ec-hackaton/browser/" "<WEBROOT>/"
```

Bumping the build (even a no-op commit) is preferred over reverting in place — SW versioning works off content hashes, and rebuilding generates a fresh `ngsw.json` users will pick up.

## Common pitfalls — fix list

| Symptom | Cause | Fix |
|---|---|---|
| Users stuck on old version forever after deploy | `ngsw.json` or `index.html` got cached by Nginx / CDN | Verify no-cache headers (see smoke test 1). Hard-flush any CDN. |
| `manifest.webmanifest` 404s or wrong MIME | Nginx default MIME missing | The `types { } default_type ...` block above fixes it. |
| Service worker won't register | App served over HTTP, not HTTPS | Cert + redirect must be live before SW will install. |
| `npm run build` killed | EC2 OOM | Add swap or upgrade instance (see Memory note). |
| API calls 502 | Backend not listening on `<BACKEND_PORT>` or bound to non-loopback | `ss -tlnp | grep <BACKEND_PORT>` to confirm. |
| Routes 404 on hard refresh | SPA fallback missing | `try_files $uri $uri/ /index.html;` in `location /`. |

## Optional improvements (later, not blocking)

- **CI/CD**: GitHub Action builds on push to `main`, SSHes to EC2, runs `deploy-frontend.sh`. Removes "build on prod box" entirely.
- **CloudFront in front**: cheaper egress + global edge cache. Must invalidate `/index.html` and `/ngsw.json` on every deploy.
- **Blue/green webroots**: deploy to `<WEBROOT>-next/`, atomic `mv` swap, instant rollback by swapping back.
- **HTTP/3 (QUIC)**: Nginx 1.25+ with `--with-http_v3_module`. Niche.

## Open questions for ops

- Repo clone path on the box?
- Backend listening port + binding (loopback only?)?
- Domain decided + DNS access available?
- Anything in front of EC2 (ALB / CloudFront)?
- Who owns CI/CD setup, or is manual `git pull` acceptable for now?
