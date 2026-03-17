# Social Arcade Universe - Technical Specification

## 🎮 Game Overview

**Project Name:** Social Arcade Universe  
**Type:** Real-time Multiplayer Mobile Game with Social Features  
**Platform:** iOS, Android, WebGL  
**Engine:** Unity 2023 LTS  
**Backend:** Node.js + PostgreSQL + Socket.io

---

## 1. Game Overview

### Core Concept
A hybrid arcade game where players compete in quick mini-games, share gameplay clips (Reels), and engage in a vibrant social ecosystem. Think "Clash Royale meets TikTok" - fast-paced competitive gameplay with deep social sharing.

### Target Audience
- Gen Z gamers (16-28)
- Casual-competitive players
- Content creators and viewers

### Monetization
- Free-to-play with premium currency
- Battle pass system
- Skins and cosmetics
- Rewarded ads

---

## 2. System Architecture

### High-Level Architecture
```
┌─────────────────────────────────────────────────────────────────┐
│                        UNITY CLIENT                              │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐       │
│  │   UI     │  │  Game    │  │ Network  │  │  Store   │       │
│  │  System  │  │  Logic   │  │  Layer   │  │  System  │       │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘       │
└────────────────────────────┬────────────────────────────────────┘
                             │ HTTPS + WebSocket
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                       LOAD BALANCER                              │
│                    (Nginx / AWS ALB)                            │
└────────────────────────────┬────────────────────────────────────┘
                             │
        ┌────────────────────┼────────────────────┐
        ▼                    ▼                    ▼
┌───────────────┐   ┌───────────────┐   ┌───────────────┐
│  API SERVER   │   │ GAME SERVER  │   │  MEDIA SERVER │
│  (Node.js)    │   │  (Node.js)    │   │   (Node.js)   │
│               │   │               │   │               │
│  - Auth       │   │ - Matchmaker  │   │ - S3 Upload   │
│  - REST API   │   │ - Game State  │   │ - Transcode   │
│  - WebSocket  │   │ - Realtime    │   │ - CDN         │
└───────┬───────┘   └───────┬───────┘   └───────────────┘
        │                   │
        ▼                   ▼
┌───────────────┐   ┌───────────────┐
│  POSTGRESQL   │   │    REDIS      │
│  (Primary DB) │   │   (Cache)     │
└───────────────┘   └───────────────┘
```

### Technology Stack

#### Backend
- **Runtime:** Node.js 20 LTS
- **Framework:** Express.js 4.x
- **Database:** PostgreSQL 15
- **Cache:** Redis 7
- **Real-time:** Socket.io 4.x
- **ORM:** Prisma
- **Auth:** JWT + Passport.js

#### Frontend (Unity)
- **Engine:** Unity 2023.2 LTS
- **Language:** C# 11
- **Networking:** Socket.io Client
- **DI:** Zenject
- **UI:** UI Toolkit + Custom Components

#### Infrastructure
- **Cloud:** AWS (EC2, RDS, ElastiCache, S3, CloudFront)
- **CI/CD:** GitHub Actions
- **Monitoring:** Datadog

---

## 3. Backend Design

### 3.1 Project Structure
```
backend/
├── src/
│   ├── config/           # Configuration files
│   │   ├── database.ts
│   │   ├── redis.ts
│   │   ├── s3.ts
│   │   └── env.ts
│   ├── modules/
│   │   ├── auth/        # Authentication module
│   │   │   ├── auth.controller.ts
│   │   │   ├── auth.service.ts
│   │   │   ├── auth.middleware.ts
│   │   │   ├── strategies/
│   │   │   └── auth.routes.ts
│   │   ├── user/        # User management
│   │   ├── player/      # Player data
│   │   ├── game/        # Game sessions
│   │   ├── matchmaking/  # Matchmaking
│   │   ├── social/      # Friends, chat
│   │   ├── reels/       # Video clips
│   │   ├── economy/     # Currency, shop
│   │   └── admin/       # Admin panel
│   ├── shared/          # Shared utilities
│   │   ├── decorators/
│   │   ├── filters/
│   │   ├── guards/
│   │   ├── interceptors/
│   │   └── utils/
│   ├── events/          # Socket.io events
│   └── app.module.ts
├── prisma/
│   ├── schema.prisma
│   └── migrations/
├── uploads/             # Local uploads (dev)
├── tests/
├── docker-compose.yml
├── Dockerfile
├── package.json
└── tsconfig.json
```

### 3.2 API Endpoints

#### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/auth/register | Register new user |
| POST | /api/auth/login | Login with credentials |
| POST | /api/auth/refresh | Refresh access token |
| POST | /api/auth/logout | Logout |
| GET | /api/auth/oauth/:provider | OAuth redirect |
| GET | /api/auth/oauth/:provider/callback | OAuth callback |

#### User
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/users/me | Get current user |
| PUT | /api/users/me | Update profile |
| GET | /api/users/:id | Get user by ID |
| GET | /api/users/:id/stats | Get player stats |
| PUT | /api/users/:id/avatar | Update avatar |

#### Player Data
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/player/profile | Get player profile |
| PUT | /api/player/progress | Update progress |
| GET | /api/player/inventory | Get inventory |
| POST | /api/player/inventory/add | Add item |
| GET | /api/player/stats | Get player stats |
| PUT | /api/player/stats | Update stats |

#### Matchmaking
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/matchmaking/queue | Join queue |
| DELETE | /api/matchmaking/queue | Leave queue |
| GET | /api/matchmaking/status | Get queue status |

#### Game
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/games/modes | Get game modes |
| GET | /api/games/:id | Get game state |
| POST | /api/games/:id/join | Join game |
| POST | /api/games/:id/leave | Leave game |

#### Social
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/friends | Get friends list |
| POST | /api/friends | Send friend request |
| PUT | /api/friends/:id | Accept/reject |
| DELETE | /api/friends/:id | Remove friend |
| GET | /api/chat/:roomId | Get chat messages |
| POST | /api/chat/:roomId | Send message |

#### Reels
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/reels | Get feed |
| POST | /api/reels | Upload reel |
| GET | /api/reels/:id | Get reel |
| DELETE | /api/reels/:id | Delete reel |
| POST | /api/reels/:id/like | Like reel |
| DELETE | /api/reels/:id/like | Unlike reel |
| POST | /api/reels/:id/comment | Comment |

#### Economy
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/shop/items | Get shop items |
| POST | /api/shop/purchase | Purchase item |
| GET | /api/shop/categories | Get categories |
| GET | /api/economy/balance | Get balance |
| POST | /api/economy/add-coins | Add coins |

#### Leaderboard
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/leaderboard/global | Global leaderboard |
| GET | /api/leaderboard/friends | Friends leaderboard |

### 3.3 WebSocket Events

#### Client → Server
| Event | Payload | Description |
|-------|---------|-------------|
| join_lobby | { roomId } | Join game lobby |
| leave_lobby | { roomId } | Leave lobby |
| player_ready | { ready } | Toggle ready |
| game_start | { } | Start game |
| player_input | { input } | Send input |
| player_action | { action } | Send action |
| chat_message | { message } | Send chat |
| position_update | { x, y, z, rotation } | Position sync |

#### Server → Client
| Event | Payload | Description |
|-------|---------|-------------|
| lobby_updated | { players } | Lobby state |
| game_started | { config } | Game starting |
| player_joined | { player } | New player |
| player_left | { playerId } | Player left |
| position_sync | { players } | Position update |
| game_state | { state } | Full game state |
| game_ended | { results } | Game over |
| matchmaking_found | { match } | Match found |
| chat_message | { message } | New message |

---

## 4. Database Schema

### Core Tables

#### users
```sql
CREATE TABLE users (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  email VARCHAR(255) UNIQUE NOT NULL,
  username VARCHAR(50) UNIQUE NOT NULL,
  password_hash VARCHAR(255),
  avatar_url TEXT,
  bio TEXT,
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP DEFAULT NOW(),
  last_login_at TIMESTAMP,
  is_banned BOOLEAN DEFAULT FALSE,
  ban_reason TEXT,
  oauth_provider VARCHAR(50),
  oauth_id VARCHAR(255)
);
```

#### player_profiles
```sql
CREATE TABLE player_profiles (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID REFERENCES users(id) ON DELETE CASCADE,
  display_name VARCHAR(50) NOT NULL,
  level INTEGER DEFAULT 1,
  xp INTEGER DEFAULT 0,
  rank_id INTEGER REFERENCES ranks(id),
  wins INTEGER DEFAULT 0,
  losses INTEGER DEFAULT 0,
  games_played INTEGER DEFAULT 0,
  total_playtime INTEGER DEFAULT 0,
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP DEFAULT NOW()
);
```

#### currencies
```sql
CREATE TABLE currencies (
  id INTEGER PRIMARY KEY,
  user_id UUID REFERENCES users(id) ON DELETE CASCADE,
  coins INTEGER DEFAULT 1000,
  gems INTEGER DEFAULT 0,
  premium_gems INTEGER DEFAULT 0,
  updated_at TIMESTAMP DEFAULT NOW()
);
```

#### inventory_items
```sql
CREATE TABLE inventory_items (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID REFERENCES users(id) ON DELETE CASCADE,
  item_id VARCHAR(100) NOT NULL,
  item_type VARCHAR(50) NOT NULL,
  quantity INTEGER DEFAULT 1,
  acquired_at TIMESTAMP DEFAULT NOW(),
  is_equipped BOOLEAN DEFAULT FALSE
);
```

#### shop_items
```sql
CREATE TABLE shop_items (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  item_id VARCHAR(100) UNIQUE NOT NULL,
  name VARCHAR(100) NOT NULL,
  description TEXT,
  type VARCHAR(50) NOT NULL,
  price_coins INTEGER,
  price_gems INTEGER,
  rarity VARCHAR(20),
  image_url TEXT,
  is_limited BOOLEAN DEFAULT FALSE,
  available_until TIMESTAMP,
  created_at TIMESTAMP DEFAULT NOW()
);
```

#### friends
```sql
CREATE TABLE friends (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID REFERENCES users(id) ON DELETE CASCADE,
  friend_id UUID REFERENCES users(id) ON DELETE CASCADE,
  status VARCHAR(20) DEFAULT 'pending',
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP DEFAULT NOW(),
  UNIQUE(user_id, friend_id)
);
```

#### reels
```sql
CREATE TABLE reels (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID REFERENCES users(id) ON DELETE CASCADE,
  video_url TEXT NOT NULL,
  thumbnail_url TEXT,
  title VARCHAR(100),
  description TEXT,
  game_mode VARCHAR(50),
  duration INTEGER,
  views INTEGER DEFAULT 0,
  likes INTEGER DEFAULT 0,
  comments_count INTEGER DEFAULT 0,
  is_public BOOLEAN DEFAULT TRUE,
  is_featured BOOLEAN DEFAULT FALSE,
  created_at TIMESTAMP DEFAULT NOW()
);
```

#### reel_likes
```sql
CREATE TABLE reel_likes (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  reel_id UUID REFERENCES reels(id) ON DELETE CASCADE,
  user_id UUID REFERENCES users(id) ON DELETE CASCADE,
  created_at TIMESTAMP DEFAULT NOW(),
  UNIQUE(reel_id, user_id)
);
```

#### reel_comments
```sql
CREATE TABLE reel_comments (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  reel_id UUID REFERENCES reels(id) ON DELETE CASCADE,
  user_id UUID REFERENCES users(id) ON DELETE CASCADE,
  content TEXT NOT NULL,
  created_at TIMESTAMP DEFAULT NOW()
);
```

#### game_sessions
```sql
CREATE TABLE game_sessions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  game_mode VARCHAR(50) NOT NULL,
  status VARCHAR(20) DEFAULT 'waiting',
  max_players INTEGER DEFAULT 4,
  host_id UUID REFERENCES users(id),
  started_at TIMESTAMP,
  ended_at TIMESTAMP,
  created_at TIMESTAMP DEFAULT NOW()
);
```

#### game_players
```sql
CREATE TABLE game_players (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  session_id UUID REFERENCES game_sessions(id) ON DELETE CASCADE,
  user_id UUID REFERENCES users(id) ON DELETE CASCADE,
  team_id INTEGER,
  score INTEGER DEFAULT 0,
  kills INTEGER DEFAULT 0,
  deaths INTEGER DEFAULT 0,
  placement INTEGER,
  is_ready BOOLEAN DEFAULT FALSE,
  joined_at TIMESTAMP DEFAULT NOW()
);
```

#### leaderboard_entries
```sql
CREATE TABLE leaderboard_entries (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID REFERENCES users(id) ON DELETE CASCADE,
  game_mode VARCHAR(50) NOT NULL,
  rating INTEGER DEFAULT 1000,
  rank INTEGER,
  season_id INTEGER REFERENCES seasons(id),
  updated_at TIMESTAMP DEFAULT NOW(),
  UNIQUE(user_id, game_mode, season_id)
);
```

#### seasons
```sql
CREATE TABLE seasons (
  id INTEGER PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  start_date TIMESTAMP NOT NULL,
  end_date TIMESTAMP NOT NULL,
  is_active BOOLEAN DEFAULT FALSE
);
```

#### daily_challenges
```sql
CREATE TABLE daily_challenges (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID REFERENCES users(id) ON DELETE CASCADE,
  challenge_type VARCHAR(50) NOT NULL,
  target_value INTEGER NOT NULL,
  current_value INTEGER DEFAULT 0,
  reward_coins INTEGER,
  reward_xp INTEGER,
  is_completed BOOLEAN DEFAULT FALSE,
  expires_at TIMESTAMP NOT NULL,
  created_at TIMESTAMP DEFAULT NOW()
);
```

#### chat_messages
```sql
CREATE TABLE chat_messages (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  room_id VARCHAR(100) NOT NULL,
  user_id UUID REFERENCES users(id) ON DELETE CASCADE,
  content TEXT NOT NULL,
  created_at TIMESTAMP DEFAULT NOW()
);
```

---

## 5. Unity Implementation

### 5.1 Project Structure
```
Unity/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs
│   │   │   ├── SceneManager.cs
│   │   │   ├── EventSystem.cs
│   │   │   └── UIManager.cs
│   │   ├── Networking/
│   │   │   ├── NetworkManager.cs
│   │   │   ├── SocketManager.cs
│   │   │   ├── NetworkPlayer.cs
│   │   │   └── Messages/
│   │   ├── Player/
│   │   │   ├── PlayerController.cs
│   │   │   ├── PlayerAnimation.cs
│   │   │   ├── PlayerStats.cs
│   │   │   └── PlayerInventory.cs
│   │   ├── Game/
│   │   │   ├── GameMode.cs
│   │   │   ├── MatchManager.cs
│   │   │   ├── Matchmaking.cs
│   │   │   └── GameRules.cs
│   │   ├── AI/
│   │   │   ├── AIPlayer.cs
│   │   │   ├── AIBehaviorTree.cs
│   │   │   └── AINodes/
│   │   ├── Social/
│   │   │   ├── FriendsManager.cs
│   │   │   ├── ChatManager.cs
│   │   │   └── ReelsManager.cs
│   │   ├── Economy/
│   │   │   ├── CurrencyManager.cs
│   │   │   ├── ShopManager.cs
│   │   │   └── InventoryManager.cs
│   │   ├── UI/
│   │   │   ├── Screens/
│   │   │   ├── Components/
│   │   │   └── Animations/
│   │   └── Utils/
│   │       ├── Extensions.cs
│   │       ├── Helpers.cs
│   │       └── Constants.cs
│   ├── Prefabs/
│   │   ├── Player/
│   │   ├── UI/
│   │   ├── Effects/
│   │   └── AI/
│   ├── Resources/
│   │   ├── Data/
│   │   │   ├── GameData/
│   │   │   ├── ItemData/
│   │   │   └── Config/
│   │   └── Shaders/
│   ├── Scenes/
│   ├── Audio/
│   ├── Materials/
│   ├── Textures/
│   ├── Animation/
│   ├── Editor/
│   └── Plugins/
├── Packages/
├── ProjectSettings/
└── README.md
```

### 5.2 Core Systems

#### Event System
```csharp
public class GameEvents
{
    public static readonly Event OnPlayerConnected = new("OnPlayerConnected");
    public static readonly Event OnPlayerDisconnected = new("OnPlayerDisconnected");
    public static readonly Event OnGameStart = new("OnGameStart");
    public static readonly Event OnGameEnd = new("OnGameEnd");
    public static readonly Event OnMatchFound = new("OnMatchFound");
    public static readonly Event OnLobbyUpdated = new("OnLobbyUpdated");
    public static readonly Event OnChatMessage = new("OnChatMessage");
    public static readonly Event OnReelUploaded = new("OnReelUploaded");
    public static readonly Event OnCurrencyUpdated = new("OnCurrencyUpdated");
    public static readonly Event OnLevelUp = new("OnLevelUp");
}
```

#### Network Manager
```csharp
public class NetworkManager : MonoBehaviour
{
    private static NetworkManager _instance;
    public static NetworkManager Instance => _instance;
    
    public Socket Socket { get; private set; }
    public bool IsConnected => Socket?.Connected ?? false;
    
    private readonly Dictionary<string, Action<SocketResponse>> _callbacks = new();
    
    public void Connect(string serverUrl);
    public void Disconnect();
    public void Emit(string eventName, object data, Action<SocketResponse> callback = null);
    public void On(string eventName, Action<SocketResponse> handler);
    public void Off(string eventName, Action<SocketResponse> handler);
}
```

#### Player Controller
```csharp
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float rotationSpeed = 720f;
    public float acceleration = 50f;
    
    [Header("Components")]
    public CharacterController controller;
    public PlayerAnimation animator;
    public PlayerStats stats;
    
    private Vector3 _velocity;
    private Vector3 _input;
    private bool _isGrounded;
    private bool _isSprinting;
    
    public void Initialize();
    public void SetInput(Vector2 input, bool sprint);
    public void SetPosition(Vector3 position);
    public void SetRotation(Quaternion rotation);
    public void PlayAnimation(string stateName, float crossFade = 0.2f);
    public void TakeDamage(float damage);
    public void Die();
    public void Respawn(Vector3 position);
}
```

---

## 6. UI/UX Design

### Design System

#### Color Palette
```css
:root {
  /* Primary */
  --primary: #6C5CE7;
  --primary-light: #A29BFE;
  --primary-dark: #4834D4;
  
  /* Secondary */
  --secondary: #00CEC9;
  --secondary-light: #81ECEC;
  --secondary-dark: #00B5B0;
  
  /* Accent */
  --accent: #FD79A8;
  --accent-light: #FDCBDF;
  --accent-dark: #E84393;
  
  /* Neutrals */
  --background: #0D0D1A;
  --surface: #1A1A2E;
  --surface-elevated: #252542;
  --text-primary: #FFFFFF;
  --text-secondary: #B2B2CC;
  --text-muted: #6C6C8A;
  
  /* Semantic */
  --success: #00B894;
  --warning: #FDCB6E;
  --error: #E74C3C;
  --info: #74B9FF;
}
```

#### Typography
- **Display:** Poppins Bold
- **Headings:** Poppins SemiBold
- **Body:** Inter Regular
- **Mono:** JetBrains Mono

### Screen Flow
```
Splash → Login/Register → Home
                              ├── Quick Play → Matchmaking → Game → Results → Home
                              ├── Game Modes → Mode Select → Matchmaking → Game
                              ├── Social → Friends/Chat/Reels
                              ├── Profile → Stats/Inventory/Settings
                              ├── Shop → Categories → Item Detail → Purchase
                              └── Leaderboard → Global/Friends
```

---

## 7. Multiplayer Implementation

### Matchmaking Algorithm
```typescript
class MatchmakingService {
  private queue: Map<string, MatchQueue> = new();
  private matchTimeout = 30000; // 30 seconds
  
  async addToQueue(userId: string, gameMode: string, rating: number): Promise<Match> {
    const queue = this.getOrCreateQueue(gameMode);
    const entry: QueueEntry = { userId, rating, timestamp: Date.now() };
    
    // Find suitable match within skill range
    const opponents = queue.findOpponents(rating, 100); // ±100 rating
    if (opponents.length >= this.getRequiredPlayers(gameMode) - 1) {
      return this.createMatch([userId, ...opponents], gameMode);
    }
    
    queue.add(entry);
    return this.waitForMatch(entry, queue);
  }
  
  private async waitForMatch(entry: QueueEntry, queue: MatchQueue): Promise<Match> {
    return new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        queue.remove(entry.userId);
        // Expand skill range and retry
        resolve(this.expandAndRetry(entry));
      }, this.matchTimeout);
      
      queue.on('match-found', (match) => {
        clearTimeout(timeout);
        resolve(match);
      });
    });
  }
}
```

### Game State Synchronization
```csharp
public class GameStateManager : MonoBehaviour
{
    private Dictionary<string, NetworkPlayer> _players = new();
    private float _syncInterval = 0.05f; // 20 ticks/sec
    private float _lastSync;
    
    public void RegisterPlayer(string odId, NetworkPlayer player);
    public void UnregisterPlayer(string odId);
    public void UpdatePlayerPosition(string odId, Vector3 position, Quaternion rotation);
    public void UpdatePlayerState(string odId, PlayerState state);
    public void SyncGameState();
    public void HandleGameEvent(string eventName, object data);
}
```

---

## 8. Economy System

### Currency Types
| Currency | Type | Acquisition | Usage |
|----------|------|-------------|-------|
| Coins | Soft | Gameplay, Daily | Shop, Repairs |
| Gems | Hard | IAP, Rewards | Premium Shop |
| Premium Gems | Hard | IAP Only | Battle Pass, Special |

### Shop Categories
- **Skins:** Character/Weapon visuals
- **Emotes:** Social expressions
- **Effects:** Trail/Death effects
- **Boosts:** XP/Coin multipliers

### Battle Pass
- 100 tiers (free + premium)
- Daily/Weekly challenges
- Season duration: 60 days

---

## 9. Deployment

### Backend Deployment (AWS)
1. **EC2:** t3.medium (auto-scaling group)
2. **RDS:** db.t3.medium PostgreSQL
3. **ElastiCache:** cache.t3.micro Redis
4. **S3:** Media storage + CloudFront CDN
5. **ALB:** Application Load Balancer
6. **Route53:** DNS management

### Unity Build
- **iOS:** Xcode project + TestFlight
- **Android:** APK/AAB + Google Play
- **WebGL:** Build to S3 + CloudFront

---

## 10. Security

### Anti-Cheat
- Server-side validation
- Input rate limiting
- Position verification
- Signature verification

### Data Protection
- Password hashing (bcrypt)
- JWT with short expiry
- Rate limiting
- SQL injection prevention (Prisma)
- XSS prevention

---

## 11. Analytics & Monitoring

### Events Tracked
- User acquisition
- Session start/end
- Game events
- Purchase events
- Social interactions

### Dashboards
- DAU/MAU
- Retention curves
- Revenue metrics
- Game performance
- Error tracking
