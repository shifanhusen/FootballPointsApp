# Nginx Configuration for FutPoints

This app is hosted under the subpath `/futpoints`. ASP.NET Core is configured with `UsePathBase("/futpoints")`, so nginx must preserve the `/futpoints` prefix when proxying to the app. Do not rewrite or strip the prefix.

## Recommended Config

Edit `/etc/nginx/sites-available/innovitecho.cloud` (or your vhost):

```nginx
server {
    listen 80;
    server_name innovitecho.cloud www.innovitecho.cloud;

    # Redirect root to the app subpath
    location = / {
        return 302 /futpoints/;
    }

    # Optional: redirect common app routes at root to the subpath
    location ~ ^/(Players|Matches|Leaderboard|Account|Home)(/.*)?$ {
        return 302 /futpoints$uri$is_args$args;
    }

    # Proxy the app under /futpoints and PRESERVE the prefix
    # Note: no trailing slash in proxy_pass (so the URI is kept)
    location /futpoints {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;

        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header X-Forwarded-Prefix /futpoints;

        # WebSocket support (if needed)
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";

        proxy_redirect off;
        proxy_cache_bypass $http_upgrade;
    }
}
```

Why this works:
- Keeping the `/futpoints` path allows ASP.NET Core to set `Request.PathBase` correctly via `UsePathBase`, which makes all tag helpers and `~/` paths generate URLs with the prefix.
- The root and convenience redirects avoid 404s when users visit `innovitecho.cloud/Players` or just the domain root.

## How to Apply

1. **Edit nginx config:**
   ```bash
   sudo nano /etc/nginx/sites-available/innovitecho.cloud
   ```

2. **Test configuration:**
   ```bash
   sudo nginx -t
   ```

3. **Reload nginx:**
   ```bash
   sudo systemctl reload nginx
   ```

## Verification

After applying the config, test these URLs:
- ✅ `http://innovitecho.cloud/futpoints` → Should load home page
- ✅ `http://innovitecho.cloud/futpoints/players` → Should load players page
- ✅ `http://innovitecho.cloud/futpoints/matches` → Should load matches page
- ✅ `http://innovitecho.cloud/futpoints/leaderboard` → Should load leaderboard

## Troubleshooting

### Issue: 404 errors on all pages
**Solution:** Ensure nginx is NOT stripping the `/futpoints` prefix. Use `proxy_pass http://127.0.0.1:5000;` (no trailing slash) and no `rewrite`.

### Issue: CSS/JS not loading
**Solution:** Preserve the prefix and set `X-Forwarded-Prefix /futpoints`. ASP.NET Core generates correct asset URLs when `PathBase` is set.

### Issue: Redirects go to wrong URL
**Solution:** Add `proxy_redirect off;` in the nginx config.

### Issue: Still doesn't work
**Check nginx error logs:**
```bash
sudo tail -f /var/log/nginx/error.log
```

**Check if your app is running:**
```bash
curl http://72.60.42.121:5000
```

## Full Example Configuration

Here's a complete nginx config including SSL (if you have a certificate):

```nginx
# Redirect HTTP to HTTPS (if using TLS)
server {
    listen 80;
    server_name innovitecho.cloud www.innovitecho.cloud;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name innovitecho.cloud www.innovitecho.cloud;

    # ssl_certificate /path/to/cert.pem;
    # ssl_certificate_key /path/to/key.pem;

    location = / { return 302 /futpoints/; }
    location ~ ^/(Players|Matches|Leaderboard|Account|Home)(/.*)?$ { return 302 /futpoints$uri$is_args$args; }

    location /futpoints {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header X-Forwarded-Prefix /futpoints;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_redirect off;
        proxy_cache_bypass $http_upgrade;
    }
}
```

## Testing Your Current Setup

Run these commands on your server to verify:

```bash
# Check nginx config syntax
sudo nginx -t

# Check if your app responds directly
curl http://72.60.42.121:5000

# Check if nginx is forwarding correctly
curl -I http://innovitecho.cloud/futpoints

# View nginx access logs
sudo tail -f /var/log/nginx/access.log
```
