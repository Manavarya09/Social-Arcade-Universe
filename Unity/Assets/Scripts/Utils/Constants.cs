using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SocialArcade.Unity.Utils
{
    public static class Constants
    {
        public static class Game
        {
            public const float DEFAULT_HEALTH = 100f;
            public const float DEFAULT_STAMINA = 100f;
            public const float DEFAULT_MOVE_SPEED = 5f;
            public const float DEFAULT_SPRINT_SPEED = 8f;
            public const float DEFAULT_JUMP_HEIGHT = 2f;
            public const float DEFAULT_GRAVITY = -20f;
            
            public const int MAX_PLAYERS_PER_MATCH = 16;
            public const int MIN_PLAYERS_PER_MATCH = 2;
            public const float MATCH_TIMEOUT = 300f;
            public const float PLAYER_SYNC_RATE = 0.05f;
        }
        
        public static class Network
        {
            public const string DEFAULT_SERVER_URL = "http://localhost:3000";
            public const int SOCKET_RECONNECT_ATTEMPTS = 5;
            public const int SOCKET_RECONNECT_DELAY = 1000;
            public const int SOCKET_PING_INTERVAL = 25000;
            public const int SOCKET_PING_TIMEOUT = 60000;
        }
        
        public static class Economy
        {
            public const int STARTING_COINS = 1000;
            public const int STARTING_GEMS = 0;
            public const int DAILY_BONUS_COINS = 100;
            public const int DAILY_BONUS_GEMS = 10;
            
            public static readonly int[] XP_PER_LEVEL = {
                0, 100, 250, 450, 700, 1000, 1350, 1750, 2200, 2700,
                3250, 3850, 4500, 5200, 5950, 6750, 7600, 8500, 9450, 10450
            };
        }
        
        public static class UI
        {
            public const float SCREEN_TRANSITION_DURATION = 0.3f;
            public const float TOAST_DURATION = 3f;
            public const float LOADING_BAR_SMOOTHNESS = 0.1f;
        }
        
        public static class Animation
        {
            public const float DEFAULT_CROSSFADE_DURATION = 0.2f;
            public const float ATTACK_ANIMATION_DURATION = 0.5f;
            public const float HIT_REACTION_DURATION = 0.3f;
            public const float DEATH_ANIMATION_DURATION = 2f;
        }
        
        public static class AntiCheat
        {
            public const float MAX_VELOCITY = 20f;
            public const float MAX_TELEPORT_DISTANCE = 50f;
            public const int MAX_ACTIONS_PER_SECOND = 20;
            public const int SUSPICIOUS_SCORE_THRESHOLD = 50;
        }
    }
    
    public static class GameDataConfig
    {
        [Serializable]
        public class GameModeData
        {
            public string modeId;
            public string name;
            public string description;
            public int minPlayers;
            public int maxPlayers;
            public float duration;
            public bool isTeamBased;
            public string iconPath;
        }
        
        [Serializable]
        public class ItemData
        {
            public string itemId;
            public string name;
            public string description;
            public string type;
            public int priceCoins;
            public int priceGems;
            public string rarity;
            public string iconPath;
            public bool isLimited;
            public DateTime? availableUntil;
        }
        
        [Serializable]
        public class MapData
        {
            public string mapId;
            public string name;
            public string sceneName;
            public int maxPlayers;
            public Vector3[] spawnPoints;
            public Bounds mapBounds;
        }
        
        public static List<GameModeData> GetDefaultGameModes()
        {
            return new List<GameModeData>
            {
                new GameModeData
                {
                    modeId = "battle_royale",
                    name = "Battle Royale",
                    description = "Last player standing wins",
                    minPlayers = 2,
                    maxPlayers = 16,
                    duration = 600,
                    isTeamBased = false,
                    iconPath = "Icons/battle_royale"
                },
                new GameModeData
                {
                    modeId = "deathmatch",
                    name = "Deathmatch",
                    description = "Most kills wins",
                    minPlayers = 2,
                    maxPlayers = 8,
                    duration = 300,
                    isTeamBased = false,
                    iconPath = "Icons/deathmatch"
                },
                new GameModeData
                {
                    modeId = "team_deathmatch",
                    name = "Team Deathmatch",
                    description = "Team vs Team",
                    minPlayers = 4,
                    maxPlayers = 8,
                    duration = 600,
                    isTeamBased = true,
                    iconPath = "Icons/team_deathmatch"
                },
                new GameModeData
                {
                    modeId = "capture_flag",
                    name = "Capture the Flag",
                    description = "Capture enemy flags",
                    minPlayers = 4,
                    maxPlayers = 8,
                    duration = 480,
                    isTeamBased = true,
                    iconPath = "Icons/capture_flag"
                }
            };
        }
    }
    
    public static class ColorPalettes
    {
        public static class Primary
        {
            public static readonly Color Purple = new Color(0.424f, 0.361f, 0.906f);
            public static readonly Color PurpleLight = new Color(0.635f, 0.608f, 0.996f);
            public static readonly Color PurpleDark = new Color(0.282f, 0.204f, 0.831f);
        }
        
        public static class Secondary
        {
            public static readonly Color Teal = new Color(0f, 0.808f, 0.788f);
            public static readonly Color TealLight = new Color(0.506f, 0.925f, 0.925f);
            public static readonly Color TealDark = new Color(0f, 0.71f, 0.69f);
        }
        
        public static class Accent
        {
            public static readonly Color Pink = new Color(0.992f, 0.475f, 0.659f);
            public static readonly Color PinkLight = new Color(0.992f, 0.796f, 0.875f);
            public static readonly Color PinkDark = new Color(0.91f, 0.263f, 0.576f);
        }
        
        public static class Background
        {
            public static readonly Color Dark = new Color(0.051f, 0.051f, 0.102f);
            public static readonly Color Surface = new Color(0.102f, 0.102f, 0.18f);
            public static readonly Color SurfaceElevated = new Color(0.145f, 0.145f, 0.259f);
        }
        
        public static class Semantic
        {
            public static readonly Color Success = new Color(0f, 0.722f, 0.58f);
            public static readonly Color Warning = new Color(0.992f, 0.796f, 0.431f);
            public static readonly Color Error = new Color(0.906f, 0.298f, 0.235f);
            public static readonly Color Info = new Color(0.455f, 0.725f, 1f);
        }
        
        public static class Rarity
        {
            public static readonly Color Common = Color.gray;
            public static readonly Color Uncommon = Color.green;
            public static readonly Color Rare = Color.blue;
            public static readonly Color Epic = new Color(0.643f, 0.078f, 0.878f);
            public static readonly Color Legendary = new Color(1f, 0.647f, 0f);
        }
    }
}
