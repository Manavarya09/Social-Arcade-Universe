import { Router, Response, NextFunction } from 'express';
import { PrismaClient } from '@prisma/client';
import { authenticate, AuthRequest } from '../../shared/middleware/auth.middleware';
import { logger } from '../../shared/utils/logger';

const router = Router();
const prisma = new PrismaClient();

interface LiveEvent {
  id: string;
  name: string;
  description: string;
  type: 'tournament' | 'challenge' | 'season' | 'limited_time';
  startDate: Date;
  endDate: Date;
  rewards: EventReward[];
  requirements: EventRequirement;
  status: 'upcoming' | 'active' | 'completed';
  maxParticipants?: number;
  currentParticipants?: number;
}

interface EventReward {
  type: 'coins' | 'gems' | 'xp' | 'item' | 'skin';
  amount: number;
  tier?: number;
}

interface EventRequirement {
  minLevel?: number;
  gameMode?: string;
  minWins?: number;
}

const DEFAULT_EVENTS: LiveEvent[] = [
  {
    id: 'weekly_tournament',
    name: 'Weekly Tournament',
    description: 'Compete for the top spot in our weekly tournament!',
    type: 'tournament',
    startDate: new Date(),
    endDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000),
    rewards: [
      { type: 'coins', amount: 1000, tier: 1 },
      { type: 'gems', amount: 100, tier: 2 },
      { type: 'xp', amount: 500, tier: 3 },
    ],
    requirements: { minLevel: 5 },
    status: 'active',
    maxParticipants: 100,
    currentParticipants: 45,
  },
  {
    id: 'double_xp_weekend',
    name: 'Double XP Weekend',
    description: 'Earn double XP this weekend!',
    type: 'limited_time',
    startDate: new Date(),
    endDate: new Date(Date.now() + 2 * 24 * 60 * 60 * 1000),
    rewards: [{ type: 'xp', amount: 2 }],
    requirements: {},
    status: 'active',
  },
  {
    id: 'season_1',
    name: 'Season 1: Launch',
    description: 'The first season of Social Arcade Universe!',
    type: 'season',
    startDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000),
    endDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
    rewards: [
      { type: 'skin', amount: 1 },
      { type: 'coins', amount: 5000 },
      { type: 'gems', amount: 500 },
    ],
    requirements: {},
    status: 'active',
  },
];

router.get('/events', async (_req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const now = new Date();
    
    const events = DEFAULT_EVENTS.map(event => ({
      ...event,
      status: now < event.startDate ? 'upcoming' : 
               now > event.endDate ? 'completed' : 'active',
    }));

    res.json({
      success: true,
      data: events,
    });
  } catch (error) {
    next(error);
  }
});

router.get('/events/active', async (_req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const now = new Date();
    
    const activeEvents = DEFAULT_EVENTS
      .filter(event => now >= event.startDate && now <= event.endDate)
      .map(event => ({
        ...event,
        status: 'active',
      }));

    res.json({
      success: true,
      data: activeEvents,
    });
  } catch (error) {
    next(error);
  }
});

router.get('/events/:id', async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id } = req.params;
    const event = DEFAULT_EVENTS.find(e => e.id === id);

    if (!event) {
      return res.status(404).json({
        success: false,
        error: { message: 'Event not found' },
      });
    }

    const now = new Date();
    const status = now < event.startDate ? 'upcoming' : 
                   now > event.endDate ? 'completed' : 'active';

    res.json({
      success: true,
      data: { ...event, status },
    });
  } catch (error) {
    next(error);
  }
});

router.post('/events/:id/join', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id } = req.params;
    const userId = req.user!.userId;

    const event = DEFAULT_EVENTS.find(e => e.id === id);

    if (!event) {
      return res.status(404).json({
        success: false,
        error: { message: 'Event not found' },
      });
    }

    const now = new Date();
    if (now < event.startDate || now > event.endDate) {
      return res.status(400).json({
        success: false,
        error: { message: 'Event is not active' },
      });
    }

    if (event.requirements.minLevel) {
      const profile = await prisma.playerProfile.findUnique({ where: { userId } });
      
      if (!profile || profile.level < event.requirements.minLevel) {
        return res.status(400).json({
          success: false,
          error: { message: `Minimum level ${event.requirements.minLevel} required` },
        });
      }
    }

    logger.info(`User ${userId} joined event ${id}`);

    res.json({
      success: true,
      data: { message: 'Successfully joined event' },
    });
  } catch (error) {
    next(error);
  }
});

router.get('/leaderboard/:eventId', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { eventId } = req.params;
    const { limit = 10 } = req.query;

    const mockLeaderboard = [];
    for (let i = 0; i < parseInt(limit as string); i++) {
      mockLeaderboard.push({
        rank: i + 1,
        odId: `player_${i + 1}`,
        username: `Player${i + 1}`,
        score: (10 - i) * 1000,
        isCurrentUser: i === 2,
      });
    }

    res.json({
      success: true,
      data: mockLeaderboard,
    });
  } catch (error) {
    next(error);
  }
});

router.get('/seasons/current', async (_req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const season = await prisma.season.findFirst({
      where: { isActive: true },
    });

    if (!season) {
      return res.json({
        success: true,
        data: {
          id: 1,
          name: 'Season 1: Launch',
          startDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000),
          endDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
          isActive: true,
          daysRemaining: 30,
        },
      });
    }

    const daysRemaining = Math.ceil((season.endDate.getTime() - Date.now()) / (1000 * 60 * 60 * 24));

    res.json({
      success: true,
      data: {
        ...season,
        daysRemaining: Math.max(0, daysRemaining),
      },
    });
  } catch (error) {
    next(error);
  }
});

router.get('/seasons/:id/rewards', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id } = req.params;
    const userId = req.user!.userId;

    const rewards = [
      { tier: 1, requiredXP: 0, reward: { type: 'coins', amount: 100 } },
      { tier: 2, requiredXP: 1000, reward: { type: 'coins', amount: 250 } },
      { tier: 3, requiredXP: 2500, reward: { type: 'gems', amount: 25 } },
      { tier: 4, requiredXP: 5000, reward: { type: 'coins', amount: 500 } },
      { tier: 5, requiredXP: 10000, reward: { type: 'skin', amount: 1 } },
      { tier: 6, requiredXP: 20000, reward: { type: 'gems', amount: 100 } },
      { tier: 7, requiredXP: 35000, reward: { type: 'coins', amount: 1000 } },
      { tier: 8, requiredXP: 50000, reward: { type: 'emote', amount: 1 } },
      { tier: 9, requiredXP: 75000, reward: { type: 'gems', amount: 250 } },
      { tier: 10, requiredXP: 100000, reward: { type: 'legendary_skin', amount: 1 } },
    ];

    const profile = await prisma.playerProfile.findUnique({ where: { userId } });
    const currentXP = profile?.xp || 0;

    const rewardsWithStatus = rewards.map(r => ({
      ...r,
      claimed: currentXP >= r.requiredXP,
      canClaim: currentXP >= r.requiredXP + 1000,
    }));

    res.json({
      success: true,
      data: {
        seasonId: id,
        currentXP,
        rewards: rewardsWithStatus,
      },
    });
  } catch (error) {
    next(error);
  }
});

router.post('/seasons/:id/claim/:tier', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id, tier } = req.params;
    const userId = req.user!.userId;

    const profile = await prisma.playerProfile.findUnique({ where: { userId } });
    
    if (!profile) {
      return res.status(404).json({
        success: false,
        error: { message: 'Profile not found' },
      });
    }

    const tierInt = parseInt(tier);
    const tierXP = [0, 1000, 2500, 5000, 10000, 20000, 35000, 50000, 75000, 100000];
    
    if (profile.xp < tierXP[tierInt - 1]) {
      return res.status(400).json({
        success: false,
        error: { message: 'Not enough XP to claim this reward' },
      });
    }

    const rewards = [
      { type: 'coins', amount: 100 },
      { type: 'coins', amount: 250 },
      { type: 'gems', amount: 25 },
      { type: 'coins', amount: 500 },
      { type: 'skin', amount: 1 },
    ];

    const reward = rewards[tierInt - 1];

    if (reward.type === 'coins') {
      await prisma.currency.update({
        where: { userId },
        data: { coins: { increment: reward.amount } },
      });
    } else if (reward.type === 'gems') {
      await prisma.currency.update({
        where: { userId },
        data: { gems: { increment: reward.amount } },
      });
    }

    logger.info(`User ${userId} claimed season reward tier ${tier}`);

    res.json({
      success: true,
      data: {
        message: 'Reward claimed!',
        reward,
      },
    });
  } catch (error) {
    next(error);
  }
});

export default router;
