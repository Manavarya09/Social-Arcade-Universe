import { Router, Response, NextFunction } from 'express';
import { PrismaClient } from '@prisma/client';
import { authenticate, optionalAuth, AuthRequest } from '../../shared/middleware/auth.middleware';
import { redis } from '../../config/redis';
import { logger } from '../../shared/utils/logger';

const router = Router();
const prisma = new PrismaClient();

interface AnalyticsEvent {
  event: string;
  userId?: string;
  sessionId?: string;
  data: Record<string, any>;
  timestamp: number;
}

interface FunnelStep {
  name: string;
  conversionRate: number;
  count: number;
}

router.post('/event', optionalAuth, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user?.userId || req.body.anonymousId;
    const { event, data, sessionId } = req.body;

    const analyticsEvent: AnalyticsEvent = {
      event,
      userId,
      sessionId,
      data,
      timestamp: Date.now(),
    };

    const redisClient = redis.getClient();
    await redisClient.lpush('analytics:events', JSON.stringify(analyticsEvent));
    await redisClient.ltrim('analytics:events', 0, 9999);

    await processEvent(analyticsEvent);

    res.json({ success: true });
  } catch (error) {
    next(error);
  }
});

router.post('/batch', optionalAuth, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { events } = req.body;
    const userId = req.user?.userId;

    const redisClient = redis.getClient();
    
    for (const event of events) {
      const analyticsEvent: AnalyticsEvent = {
        ...event,
        userId: event.userId || userId,
        timestamp: event.timestamp || Date.now(),
      };
      
      await redisClient.lpush('analytics:events', JSON.stringify(analyticsEvent));
      await processEvent(analyticsEvent);
    }

    await redisClient.ltrim('analytics:events', 0, 9999);

    res.json({ success: true, processed: events.length });
  } catch (error) {
    next(error);
  }
});

async function processEvent(event: AnalyticsEvent): Promise<void> {
  const redisClient = redis.getClient();

  if (event.event === 'session_start') {
    await redisClient.incr('analytics:dau');
    await redisClient.zincrby('analytics:hourly_active', 1, new Date().getHours().toString());
  }

  if (event.event === 'purchase') {
    await redisClient.hincrby('analytics:revenue', 'total', event.data.amount || 0);
    await redisClient.hincrby('analytics:purchases', event.data.itemId || 'unknown', 1);
  }

  if (event.event === 'game_start') {
    await redisClient.zincrby('analytics:game_starts', 1, event.data.gameMode || 'unknown');
  }

  if (event.event === 'level_up') {
    await redisClient.zadd('analytics:level_distribution', event.data.level, event.userId || '');
  }
}

router.get('/stats', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const redisClient = redis.getClient();

    const [dau, totalRevenue, gameStarts, hourlyActive] = await Promise.all([
      redisClient.get('analytics:dau'),
      redisClient.hgetall('analytics:revenue'),
      redisClient.zrange('analytics:game_starts', 0, -1, 'WITHSCORES'),
      redisClient.zrange('analytics:hourly_active', 0, -1, 'WITHSCORES'),
    ]);

    const today = new Date();
    const weekAgo = new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000);

    const newUsers = await prisma.user.count({
      where: { createdAt: { gte: weekAgo } },
    });

    const activeUsersWeek = await prisma.user.count({
      where: { lastLoginAt: { gte: weekAgo } },
    });

    res.json({
      success: true,
      data: {
        dau: parseInt(dau || '0'),
        weeklyActiveUsers: activeUsersWeek,
        newUsersThisWeek: newUsers,
        revenue: {
          total: parseInt(totalRevenue?.total || '0'),
        },
        gameModes: Object.fromEntries(
          Array.from({ length: gameStarts.length / 2 }, (_, i) => [
            gameStarts[i * 2],
            parseInt(gameStarts[i * 2 + 1]),
          ])
        ),
        hourlyActive: Object.fromEntries(
          Array.from({ length: hourlyActive.length / 2 }, (_, i) => [
            hourlyActive[i * 2],
            parseInt(hourlyActive[i * 2 + 1]),
          ])
        ),
      },
    });
  } catch (error) {
    next(error);
  }
});

router.get('/funnel/:name', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { name } = req.params;
    const funnel = getFunnel(name);

    res.json({
      success: true,
      data: funnel,
    });
  } catch (error) {
    next(error);
  }
});

function getFunnel(name: string): FunnelStep[] {
  switch (name) {
    case 'onboarding':
      return [
        { name: 'app_open', conversionRate: 100, count: 10000 },
        { name: 'tutorial_complete', conversionRate: 75, count: 7500 },
        { name: 'first_game', conversionRate: 60, count: 6000 },
        { name: 'first_purchase', conversionRate: 10, count: 1000 },
      ];
    case 'retention':
      return [
        { name: 'day_1', conversionRate: 100, count: 10000 },
        { name: 'day_7', conversionRate: 35, count: 3500 },
        { name: 'day_30', conversionRate: 15, count: 1500 },
      ];
    default:
      return [];
  }
}

router.get('/revenue', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { period = '30d' } = req.query;
    
    const days = parseInt(period as string) || 30;
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - days);

    const purchases = await prisma.user.findMany({
      where: {
        currencies: {
          premiumGems: { lt: 0 },
        },
      },
      select: {
        id: true,
        createdAt: true,
      },
    });

    res.json({
      success: true,
      data: {
        period: `${days}d`,
        totalPurchases: purchases.length,
        estimatedRevenue: purchases.length * 4.99,
      },
    });
  } catch (error) {
    next(error);
  }
});

export default router;
