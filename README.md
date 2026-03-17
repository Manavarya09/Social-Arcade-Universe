# Social Arcade Universe

A production-ready multiplayer mobile game with social features, built with Unity (C#) and Node.js backend.

## 🎮 Game Overview

**Social Arcade Universe** is a hybrid arcade game combining:
- Real-time multiplayer gameplay
- Social/reel-style content sharing (TikTok-like)
- Progression and economy systems
- AI-driven personalization

## 🏗️ Architecture

### Backend (Node.js + PostgreSQL + Socket.io)
- **API Server**: Express.js REST API
- **Real-time**: Socket.io for multiplayer
- **Database**: PostgreSQL with Prisma ORM
- **Cache**: Redis for matchmaking and sessions

### Frontend (Unity 2023 LTS)
- **Language**: C# 11
- **Networking**: Socket.io Client
- **Architecture**: Event-driven with Clean Architecture

## 🚀 Quick Start

### Prerequisites
- Node.js 20+
- PostgreSQL 15+
- Redis 7+
- Unity 2023.2 LTS

### Backend Setup

```bash
cd backend

# Install dependencies
npm install

# Copy environment file
cp .env.example .env

# Update .env with your configuration
# DATABASE_URL=postgresql://user:pass@localhost:5432/sau_dev
# REDIS_URL=redis://localhost:6379

# Generate Prisma client
npm run prisma:generate

# Run database migrations
npm run prisma:migrate

# Start development server
npm run dev
```

### Unity Setup

1. Open Unity Hub and add the `Unity` folder
2. Open the project with Unity 2023.2+
3. Configure `ServerUrl` in `GameManager` to point to your backend
4. Build and run

## 📁 Project Structure

```
Social-Arcade-Universe/
├── SPEC.md                    # Detailed technical specification
├── backend/
│   ├── src/
│   │   ├── config/            # Configuration files
│   │   ├── modules/           # Feature modules
│   │   │   ├── auth/          # Authentication
│   │   │   ├── user/          # User management
│   │   │   ├── player/        # Player data
│   │   │   ├── game/          # Game sessions
│   │   │   ├── matchmaking/   # Matchmaking
│   │   │   ├── social/        # Friends, Chat, Reels
│   │   │   ├── economy/       # Currency, Shop
│   │   │   └── admin/         # Admin panel
│   │   ├── shared/            # Shared utilities
│   │   ├── websocket/         # Socket.io service
│   │   └── index.ts           # Entry point
│   ├── prisma/
│   │   └── schema.prisma      # Database schema
│   ├── package.json
│   ├── tsconfig.json
│   ├── Dockerfile
│   └── docker-compose.yml
│
├── Unity/
│   ├── Assets/
│   │   ├── Scripts/
│   │   │   ├── Core/          # GameManager, Events
│   │   │   ├── Networking/    # NetworkManager, Socket
│   │   │   ├── Player/        # PlayerController, Stats
│   │   │   ├── Game/          # Game modes, rules
│   │   │   ├── AI/            # AI behaviors
│   │   │   ├── Social/        # Friends, Reels
│   │   │   ├── Economy/       # Currency, Shop
│   │   │   ├── UI/            # Screens, Components
│   │   │   └── Utils/          # Helpers
│   │   ├── Prefabs/
│   │   ├── Resources/
│   │   └── Scenes/
│   ├── Packages/
│   └── ProjectSettings/
│
└── README.md
```

## 🔌 API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login
- `POST /api/auth/refresh` - Refresh token
- `POST /api/auth/logout` - Logout

### User
- `GET /api/users/me` - Get current user
- `PUT /api/users/me` - Update profile
- `GET /api/users/:id` - Get user by ID

### Player
- `GET /api/player/profile` - Get player profile
- `GET /api/player/inventory` - Get inventory
- `PUT /api/player/progress` - Update progress

### Game
- `GET /api/games/modes` - Get game modes
- `POST /api/games/create` - Create game
- `POST /api/games/:id/join` - Join game

### Social
- `GET /api/social/friends` - Get friends
- `GET /api/social/chat/:roomId` - Get chat messages
- `GET /api/reels` - Get reels feed
- `POST /api/reels` - Upload reel

### Economy
- `GET /api/economy/balance` - Get balance
- `GET /api/shop/items` - Get shop items
- `POST /api/shop/purchase` - Purchase item

### Leaderboard
- `GET /api/leaderboard/global` - Global leaderboard
- `GET /api/leaderboard/friends` - Friends leaderboard

## 🎮 WebSocket Events

### Client → Server
- `join_matchmaking` - Join matchmaking queue
- `join_lobby` - Join game lobby
- `player_input` - Send input data
- `position_update` - Sync position
- `chat_message` - Send chat message

### Server → Client
- `match_found` - Match found
- `game_started` - Game starting
- `position_sync` - Player position update
- `chat_message` - New chat message
- `game_ended` - Game over

## 🔧 Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `NODE_ENV` | Environment | development |
| `PORT` | Server port | 3000 |
| `DATABASE_URL` | PostgreSQL connection | - |
| `REDIS_URL` | Redis connection | - |
| `JWT_SECRET` | JWT signing secret | - |
| `CLIENT_URL` | Client URL | http://localhost:3001 |
| `AWS_S3_BUCKET` | S3 bucket for media | - |

## 🚢 Deployment

### Docker (Recommended)

```bash
cd backend

# Update .env with production values
cp .env.example .env
# Edit .env

# Build and run
docker-compose up -d
```

### Manual

1. Build the backend:
```bash
cd backend
npm run build
```

2. Run with PM2:
```bash
pm2 start dist/index.js --name sau-backend
```

### Unity Build

1. Open Unity project
2. Go to File > Build Settings
3. Select target platform (iOS/Android)
4. Click Build

## 🎨 UI Design System

The UI follows a modern Gen-Z aesthetic:

### Colors
- Primary: #6C5CE7 (Purple)
- Secondary: #00CEC9 (Teal)
- Accent: #FD79A8 (Pink)
- Background: #0D0D1A (Dark)
- Surface: #1A1A2E

### Typography
- Display: Poppins Bold
- Headings: Poppins SemiBold
- Body: Inter Regular

## 🛡️ Security

- JWT-based authentication
- Password hashing with bcrypt
- Rate limiting on API endpoints
- Input validation
- SQL injection prevention (Prisma)
- XSS protection

## 📊 Analytics

The system tracks:
- User acquisition
- Session events
- Game performance
- Purchase events
- Social interactions

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Open a Pull Request

## 📄 License

MIT License - see LICENSE for details
