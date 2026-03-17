# Deployment Guide

This guide covers deploying the Social Arcade Universe backend and building the Unity client.

## 🖥️ Backend Deployment

### Option 1: Docker (Recommended)

The easiest way to deploy the backend:

```bash
cd backend

# Create .env file
cp .env.example .env
# Edit with your production values

# Build and start
docker-compose up -d
```

This will start:
- PostgreSQL database
- Redis cache
- Node.js API server
- Nginx reverse proxy

### Option 2: Manual Deployment

#### Prerequisites
- Node.js 20 LTS
- PostgreSQL 15+
- Redis 7+
- Nginx (for reverse proxy)

#### Steps

1. **Clone and install**:
```bash
git clone <repository>
cd backend
npm install
```

2. **Configure environment**:
```bash
cp .env.example .env
# Edit .env with production values
```

3. **Database setup**:
```bash
# Create database
createdb sau_prod

# Run migrations
npm run prisma:migrate
```

4. **Build**:
```bash
npm run build
```

5. **Run with PM2**:
```bash
npm install -g pm2
pm2 start dist/index.js --name sau-backend
pm2 startup
pm2 save
```

6. **Nginx configuration**:
```nginx
server {
    listen 80;
    server_name api.socialarcadeuniverse.com;

    location / {
        proxy_pass http://localhost:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### Option 3: Cloud Platforms

#### AWS (EC2 + RDS + ElastiCache)

1. **Launch EC2 instance**:
   - Type: t3.medium or larger
   - OS: Ubuntu 22.04 LTS

2. **Set up RDS PostgreSQL**:
   - Instance: db.t3.medium
   - Multi-AZ for production

3. **Set up ElastiCache Redis**:
   - Node: cache.t3.micro

4. **Deploy application**:
```bash
# SSH to instance
ssh ubuntu@<ec2-ip>

# Install Node.js
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt-get install -y nodejs

# Clone and deploy
git clone <repository>
cd backend
npm install
npm run build

# Run with PM2
pm2 start dist/index.js
```

#### Railway

1. Connect GitHub repository to Railway
2. Add PostgreSQL and Redis services
3. Set environment variables
4. Deploy

#### Render

1. Connect GitHub repository
2. Create web service
3. Add PostgreSQL and Redis
4. Set environment variables
5. Deploy

### Option 4: Serverless (Limited)

For small scale, you can use Vercel Serverless Functions:
- Note: WebSocket support is limited
- Consider using Pusher or Ably for real-time

---

## 📱 Unity Client Build

### Android

1. **Project Setup**:
   - Open Unity project
   - Go to File > Build Settings
   - Select Android platform
   - Click "Switch Platform"

2. **Configure Player Settings**:
   - Company Name: YourCompany
   - Product Name: Social Arcade Universe
   - Bundle Identifier: com.yourcompany.socialarcadeuniverse
   - Minimum API Level: 24 (Android 7.0)

3. **Build**:
```bash
# In Unity Editor
File > Build And Run
```

Or use command line:
```bash
/path/to/unity -batchmode -projectPath /path/to/project -buildTarget Android -buildPath /path/to/output
```

4. **APK Signing**:
   - Go to Player Settings > Publishing Settings
   - Create or import keystore
   - Configure signing

5. **Release**:
   - Build AAB for Google Play
   - Or APK for distribution

### iOS

1. **Project Setup**:
   - Open Unity project
   - Go to File > Build Settings
   - Select iOS platform
   - Click "Switch Platform"

2. **Configure Player Settings**:
   - Company Name: YourCompany
   - Product Name: Social Arcade Universe
   - Bundle Identifier: com.yourcompany.socialarcadeuniverse
   - Target Device: iPhone+iPad
   - Target iOS Version: 14.0

3. **Build**:
```bash
# In Unity Editor
File > Build And Run
```

4. **Xcode Setup**:
   - Open generated .xcworkspace
   - Configure signing
   - Add capabilities (Game Center, Push Notifications)
   - Build for simulator or device

5. **App Store**:
   - Create App Store listing
   - Upload via Xcode or Transporter

### WebGL

1. **Project Setup**:
   - Open Unity project
   - Go to File > Build Settings
   - Select WebGL
   - Click "Switch Platform"

2. **Configure**:
   - Player Settings > Resolution and Presentation
   - Set default canvas size
   - Enable WebGL 2.0

3. **Build**:
```bash
# In Unity Editor
File > Build And Run
```

4. **Deploy**:
   - Upload to S3 + CloudFront
   - Or use itch.io, GameJolt

---

## 🔧 Post-Deployment

### Health Checks

Configure monitoring:
```bash
curl https://api.yourdomain.com/health
```

Expected response:
```json
{"status": "ok", "timestamp": "2024-01-01T00:00:00.000Z"}
```

### SSL/TLS

1. **Let's Encrypt (Recommended)**:
```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d api.yourdomain.com
```

2. **Automatic renewal**:
```bash
sudo certbot renew --dry-run
```

### Backups

1. **Database**:
```bash
# Add to crontab
0 2 * * * pg_dump -U postgres sau_prod > /backup/sau_$(date +\%Y\%m\%d).sql
```

2. **Files**:
```bash
# Backup uploads
0 3 * * * tar -czf /backup/uploads_$(date +\%Y\%m\%d).tar.gz uploads/
```

### Logging

1. **Application logs**:
```bash
pm2 logs sau-backend --lines 100
```

2. **System logs**:
```bash
tail -f /var/log/nginx/access.log
tail -f /var/log/nginx/error.log
```

### Monitoring

1. **PM2 Plus** (free tier available):
```bash
pm2 link <secret-key> <public-key>
```

2. **Datadog**:
- Install agent
- Configure traces and logs

---

## 🚨 Troubleshooting

### Backend

**Port already in use**:
```bash
sudo lsof -i :3000
kill <pid>
```

**Database connection failed**:
- Check DATABASE_URL in .env
- Verify PostgreSQL is running
- Check security groups (AWS)

**Redis connection failed**:
- Check REDIS_URL in .env
- Verify Redis is running

### Unity Client

**Build failed**:
- Check Console for errors
- Verify all dependencies installed
- Check Unity version compatibility

**Network issues**:
- Verify ServerUrl in GameManager
- Check firewall settings
- Verify SSL certificates

---

## 📞 Support

For deployment issues:
- Check logs: `pm2 logs sau-backend`
- Run in debug mode: `NODE_ENV=development npm run dev`
- Contact: support@socialarcadeuniverse.com
