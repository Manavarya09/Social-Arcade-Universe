import { PrismaClient } from '@prisma/client';
import { redis } from '../../config/redis';
import { logger } from '../../shared/utils/logger';
import { v4 as uuidv4 } from 'uuid';

const prisma = new PrismaClient();

interface QueueEntry {
  userId: string;
  gameMode: string;
  rating: number;
  timestamp: number;
}

interface Match {
  matchId: string;
  gameMode: string;
  playerIds: string[];
  roomId: string;
  createdAt: number;
}

export class MatchmakingService {
  private queueKey = 'matchmaking:queue';
  private activeMatchesKey = 'matchmaking:active';

  async addToQueue(userId: string, gameMode: string, rating: number): Promise<Match | null> {
    const redisClient = redis.getClient();
    
    const entry: QueueEntry = {
      userId,
      gameMode,
      rating,
      timestamp: Date.now(),
    };

    await redisClient.zadd(`${this.queueKey}:${gameMode}`, rating, JSON.stringify(entry));

    logger.info(`User ${userId} added to matchmaking queue for ${gameMode}`);

    return this.findMatch(gameMode);
  }

  async removeFromQueue(userId: string): Promise<void> {
    const redisClient = redis.getClient();
    
    const queues = ['ranked', 'casual', 'custom'];
    
    for (const gameMode of queues) {
      const entries = await redisClient.zrange(`${this.queueKey}:${gameMode}`, 0, -1);
      
      for (const entry of entries) {
        const parsed = JSON.parse(entry) as QueueEntry;
        if (parsed.userId === userId) {
          await redisClient.zrem(`${this.queueKey}:${gameMode}`, entry);
          logger.info(`User ${userId} removed from ${gameMode} queue`);
        }
      }
    }
  }

  async getQueuePosition(userId: string, gameMode: string): Promise<number> {
    const redisClient = redis.getClient();
    
    const entries = await redisClient.zrange(`${this.queueKey}:${gameMode}`, 0, -1, 'WITHSCORES');
    
    for (let i = 0; i < entries.length; i += 2) {
      const entry = JSON.parse(entries[i]) as QueueEntry;
      if (entry.userId === userId) {
        return Math.floor(i / 2) + 1;
      }
    }
    
    return -1;
  }

  private async findMatch(gameMode: string): Promise<Match | null> {
    const redisClient = redis.getClient();
    const requiredPlayers = 4;
    const ratingRange = 100;

    const entries = await redisClient.zrange(`${this.queueKey}:${gameMode}`, 0, -1, 'WITHSCORES');
    
    if (entries.length < requiredPlayers * 2) {
      return null;
    }

    const playerEntries: { entry: QueueEntry; score: number }[] = [];
    
    for (let i = 0; i < entries.length; i += 2) {
      playerEntries.push({
        entry: JSON.parse(entries[i]),
        score: parseFloat(entries[i + 1]),
      });
    }

    const sortedByRating = playerEntries.sort((a, b) => a.score - b.score);

    for (let i = 0; i <= sortedByRating.length - requiredPlayers; i++) {
      const candidate = sortedByRating[i];
      const candidates: QueueEntry[] = [candidate.entry];
      let foundMatch = true;

      for (let j = 1; j < requiredPlayers; j++) {
        const next = sortedByRating[i + j];
        
        if (Math.abs(next.score - candidate.score) > ratingRange) {
          foundMatch = false;
          break;
        }
        candidates.push(next.entry);
      }

      if (foundMatch) {
        const match: Match = {
          matchId: uuidv4(),
          gameMode,
          playerIds: candidates.map(c => c.userId),
          roomId: `game_${uuidv4()}`,
          createdAt: Date.now(),
        };

        for (const entry of candidates) {
          await redisClient.zrem(`${this.queueKey}:${gameMode}`, JSON.stringify(entry));
        }

        await redisClient.set(
          `${this.activeMatchesKey}:${match.matchId}`,
          JSON.stringify(match),
          'EX',
          3600
        );

        await this.createGameSession(match);

        logger.info(`Match created: ${match.matchId} with players ${match.playerIds.join(', ')}`);

        return match;
      }
    }

    return null;
  }

  private async createGameSession(match: Match): Promise<void> {
    await prisma.gameSession.create({
      data: {
        id: match.matchId,
        gameMode: match.gameMode,
        status: 'waiting',
        maxPlayers: match.playerIds.length,
        hostId: match.playerIds[0],
        settings: {},
      },
    });

    for (const playerId of match.playerIds) {
      await prisma.gamePlayer.create({
        data: {
          sessionId: match.matchId,
          userId: playerId,
          isReady: false,
          isAlive: true,
        },
      });
    }
  }

  async getMatch(matchId: string): Promise<Match | null> {
    const redisClient = redis.getClient();
    const matchData = await redisClient.get(`${this.activeMatchesKey}:${matchId}`);
    
    if (matchData) {
      return JSON.parse(matchData);
    }

    return null;
  }
}
