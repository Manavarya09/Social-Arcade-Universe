import { Router, Response, NextFunction } from 'express';
import { PrismaClient } from '@prisma/client';
import { authenticate, optionalAuth, AuthRequest } from '../../shared/middleware/auth.middleware';

const router = Router();
const prisma = new PrismaClient();

router.get('/global', async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { gameMode = 'ranked', limit = 100 } = req.query;
    const userId = req.user?.userId;

    const entries = await prisma.leaderboardEntry.findMany({
      where: { gameMode: gameMode as string },
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
      orderBy: { rating: 'desc' },
      take: parseInt(limit as string),
    });

    const entriesWithRank = entries.map((entry, index) => ({
      ...entry,
      rank: index + 1,
    }));

    let userRank = null;
    if (userId) {
      const userEntry = entriesWithRank.find(e => e.userId === userId);
      if (userEntry) {
        userRank = userEntry.rank;
      }
    }

    res.json({
      success: true,
      data: {
        entries: entriesWithRank,
        userRank,
      },
    });
  } catch (error) {
    next(error);
  }
});

router.get('/friends', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { gameMode = 'ranked' } = req.query;

    const friends = await prisma.friend.findMany({
      where: {
        userId,
        status: 'accepted',
      },
      select: { friendId: true },
    });

    const friendIds = friends.map(f => f.friendId);

    const entries = await prisma.leaderboardEntry.findMany({
      where: {
        userId: { in: [userId, ...friendIds] },
        gameMode: gameMode as string,
      },
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
      orderBy: { rating: 'desc' },
    });

    const entriesWithRank = entries.map((entry, index) => ({
      ...entry,
      rank: index + 1,
    }));

    res.json({
      success: true,
      data: entriesWithRank,
    });
  } catch (error) {
    next(error);
  }
});

router.get('/me', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { gameMode = 'ranked' } = req.query;

    const entry = await prisma.leaderboardEntry.findUnique({
      where: {
        userId_gameMode_seasonId: {
          userId,
          gameMode: gameMode as string,
          seasonId: 1,
        },
      },
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
    });

    if (!entry) {
      return res.json({
        success: true,
        data: {
          rating: 1000,
          rank: null,
        },
      });
    }

    const rank = await prisma.leaderboardEntry.count({
      where: {
        gameMode: gameMode as string,
        rating: { gt: entry.rating },
      },
    });

    res.json({
      success: true,
      data: {
        ...entry,
        rank: rank + 1,
      },
    });
  } catch (error) {
    next(error);
  }
});

export default router;
