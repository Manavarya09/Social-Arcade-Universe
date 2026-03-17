import { PrismaClient } from '@prisma/client';
import { redis } from '../../config/redis';
import { logger } from '../../shared/utils/logger';

const prisma = new PrismaClient();

interface GameState {
  sessionId: string;
  status: string;
  players: Map<string, any>;
  gameData: any;
}

export class GameService {
  private gameStates: Map<string, GameState> = new Map();

  async createGameSession(gameMode: string, hostId: string, settings?: any) {
    const session = await prisma.gameSession.create({
      data: {
        gameMode,
        status: 'waiting',
        hostId,
        settings: settings || {},
      },
      include: {
        players: {
          include: {
            user: {
              select: {
                id: true,
                username: true,
                avatarUrl: true,
              },
            },
          },
        },
      },
    });

    return session;
  }

  async joinGame(sessionId: string, userId: string) {
    const session = await prisma.gameSession.findUnique({
      where: { id: sessionId },
      include: {
        players: true,
      },
    });

    if (!session) {
      throw new Error('Game session not found');
    }

    if (session.status !== 'waiting') {
      throw new Error('Game already started');
    }

    if (session.players.length >= session.maxPlayers) {
      throw new Error('Game is full');
    }

    const existingPlayer = session.players.find(p => p.userId === userId);
    if (existingPlayer) {
      return session;
    }

    const player = await prisma.gamePlayer.create({
      data: {
        sessionId,
        userId,
        isReady: false,
        isAlive: true,
      },
      include: {
        user: {
          select: {
            id: true,
            username: true,
            avatarUrl: true,
          },
        },
      },
    });

    logger.info(`User ${userId} joined game ${sessionId}`);

    return this.getGameSession(sessionId);
  }

  async leaveGame(sessionId: string, userId: string) {
    await prisma.gamePlayer.deleteMany({
      where: {
        sessionId,
        userId,
      },
    });

    logger.info(`User ${userId} left game ${sessionId}`);
  }

  async getGameSession(sessionId: string) {
    const session = await prisma.gameSession.findUnique({
      where: { id: sessionId },
      include: {
        players: {
          include: {
            user: {
              select: {
                id: true,
                username: true,
                avatarUrl: true,
                playerProfile: {
                  select: {
                    displayName: true,
                    level: true,
                  },
                },
              },
            },
          },
        },
      },
    });

    return session;
  }

  async setPlayerReady(sessionId: string, userId: string, ready: boolean) {
    await prisma.gamePlayer.updateMany({
      where: {
        sessionId,
        userId,
      },
      data: {
        isReady: ready,
      },
    });

    const players = await prisma.gamePlayer.findMany({
      where: { sessionId },
    });

    const allReady = players.every(p => p.isReady);
    
    if (allReady && players.length >= 2) {
      await this.startGame(sessionId);
    }

    return { allReady, readyCount: players.filter(p => p.isReady).length };
  }

  async startGame(sessionId: string) {
    await prisma.gameSession.update({
      where: { id: sessionId },
      data: {
        status: 'in_progress',
        startedAt: new Date(),
      },
    });

    const state: GameState = {
      sessionId,
      status: 'in_progress',
      players: new Map(),
      gameData: {},
    };

    this.gameStates.set(sessionId, state);

    logger.info(`Game ${sessionId} started`);

    return { success: true, startedAt: Date.now() };
  }

  async processPlayerInput(userId: string, data: any) {
    const state = this.getStateForPlayer(userId);
    if (state) {
      // Store input for server-side validation and processing
      // This is where you'd implement anti-cheat checks
    }
  }

  async processPlayerAction(userId: string, data: any) {
    const state = this.getStateForPlayer(userId);
    if (state) {
      // Handle player actions (shooting, using items, etc.)
    }
  }

  private getStateForPlayer(userId: string): GameState | null {
    for (const state of this.gameStates.values()) {
      if (state.players.has(userId)) {
        return state;
      }
    }
    return null;
  }

  async endGame(sessionId: string, results: any) {
    await prisma.gameSession.update({
      where: { id: sessionId },
      data: {
        status: 'ended',
        endedAt: new Date(),
      },
    });

    for (const [userId, result] of Object.entries(results)) {
      await prisma.gamePlayer.updateMany({
        where: {
          sessionId,
          userId,
        },
        data: {
          score: (result as any).score || 0,
          kills: (result as any).kills || 0,
          deaths: (result as any).deaths || 0,
          placement: (result as any).placement,
        },
      });

      await prisma.playerProfile.update({
        where: { userId },
        data: {
          gamesPlayed: { increment: 1 },
          wins: (result as any).placement === 1 ? { increment: 1 } : undefined,
          losses: (result as any).placement !== 1 && (result as any).placement ? { increment: 1 } : undefined,
        },
      });
    }

    this.gameStates.delete(sessionId);

    logger.info(`Game ${sessionId} ended`);
  }

  async getActiveGame(userId: string) {
    const redisClient = redis.getClient();
    const sessionId = await redisClient.get(`player:${userId}:game`);
    
    if (sessionId) {
      return this.getGameSession(sessionId);
    }
    
    return null;
  }
}
