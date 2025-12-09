# Nginx Configuration for FutPoints

## Problem
Your app runs on `72.60.42.121:5000` but is accessed via `http://innovitecho.cloud/futpoints`.
Nginx needs to strip the `/futpoints` prefix before forwarding requests to your application.

## Solution

Update your nginx configuration file (usually at `/etc/nginx/sites-available/innovitecho.cloud`):

```nginx
server {
    listen 80;
    server_name innovitecho.cloud www.innovitecho.cloud;

    # FutPoints application
    location /futpoints/ {
        # Strip the /futpoints prefix before proxying
        rewrite ^/futpoints/(.*)$ /$1 break;
        
        proxy_pass http://72.60.42.121:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        
        # Handle root path /futpoints (without trailing slash)
        # This ensures /futpoints redirects to /futpoints/
    }
    
    # Handle exact /futpoints to redirect to /futpoints/
    location = /futpoints {
        return 301 /futpoints/;
    }
}
```

## Alternative Configuration (Recommended)

If the above doesn't work perfectly, use this more explicit configuration:

```nginx
server {
    listen 80;
    server_name innovitecho.cloud www.innovitecho.cloud;

    location /futpoints {
        # Remove /futpoints prefix and forward to backend
        proxy_pass http://72.60.42.121:5000/;
        proxy_http_version 1.1;
        
        # Important headers for ASP.NET Core
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host $host;
        proxy_set_header X-Forwarded-Prefix /futpoints;
        
        # WebSocket support (if needed in future)
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        
        proxy_cache_bypass $http_upgrade;
        proxy_redirect off;
    }
}
```

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
**Solution:** Make sure nginx is stripping the `/futpoints` prefix with the `rewrite` rule.

### Issue: CSS/JS not loading
**Solution:** The `rewrite` rule handles this. Nginx strips `/futpoints` from all requests.

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
# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name innovitecho.cloud www.innovitecho.cloud;
    return 301 https://$server_name$request_uri;
}

# HTTPS server
server {
    listen 443 ssl http2;
    server_name innovitecho.cloud www.innovitecho.cloud;

    # SSL configuration (if you have certificates)
    # ssl_certificate /path/to/cert.pem;
    # ssl_certificate_key /path/to/key.pem;

    # FutPoints application
    location /futpoints {
        rewrite ^/futpoints/(.*)$ /$1 break;
        
        proxy_pass http://72.60.42.121:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_redirect off;
    }
    
    location = /futpoints {
        return 301 /futpoints/;
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
