import { Router, Response, NextFunction } from 'express';
import { PrismaClient } from '@prisma/client';
import { authenticate, AuthRequest } from '../../shared/middleware/auth.middleware';
import { redis } from '../../config/redis';
import { logger } from '../../shared/utils/logger';
import { v4 as uuidv4 } from 'uuid';

const router = Router();
const prisma = new PrismaClient();

interface DailyChallenge {
  id: string;
  userId: string;
  challengeType: string;
  description: string;
  targetValue: number;
  currentValue: number;
  rewardCoins: number;
  rewardXp: number;
  isCompleted: boolean;
  expiresAt: Date;
}

const CHALLENGE_TEMPLATES = [
  { type: 'wins', description: 'Win {value} games', defaultTarget: 3, coins: 500, xp: 100 },
  { type: 'kills', description: 'Get {value} kills', defaultTarget: 10, coins: 300, xp: 50 },
  { type: 'games_played', description: 'Play {value} games', defaultTarget: 5, coins: 200, xp: 30 },
  { type: 'friends_added', description: 'Add {value} friends', defaultTarget: 3, coins: 150, xp: 25 },
  { type: 'reels_watched', description: 'Watch {value} reels', defaultTarget: 20, coins: 100, xp: 20 },
  { type: 'social_share', description: 'Share {value} reels', defaultTarget: 3, coins: 250, xp: 40 },
  { type: 'collection', description: 'Collect {value} items', defaultTarget: 5, coins: 400, xp: 75 },
  { type: 'streak', description: 'Login {value} days in a row', defaultTarget: 7, coins: 1000, xp: 200 },
];

router.get('/daily', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const challenges = await prisma.dailyChallenge.findMany({
      where: {
        userId,
        expiresAt: { gte: today },
      },
      orderBy: { createdAt: 'desc' },
    });

    res.json({
      success: true,
      data: challenges,
    });
  } catch (error) {
    next(error);
  }
});

router.post('/daily/claim/:id', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { id } = req.params;

    const challenge = await prisma.dailyChallenge.findUnique({
      where: { id },
    });

    if (!challenge || challenge.userId !== userId) {
      return res.status(404).json({
        success: false,
        error: { message: 'Challenge not found' },
      });
    }

    if (challenge.isCompleted) {
      return res.status(400).json({
        success: false,
        error: { message: 'Already claimed' },
      });
    }

    if (challenge.currentValue < challenge.targetValue) {
      return res.status(400).json({
        success: false,
        error: { message: 'Challenge not completed' },
      });
    }

    await prisma.$transaction([
      prisma.dailyChallenge.update({
        where: { id },
        data: { isCompleted: true },
      }),
      prisma.currency.update({
        where: { userId },
        data: {
          coins: { increment: challenge.rewardCoins },
          gems: { increment: Math.floor(challenge.rewardXp / 10) },
        },
      }),
      prisma.playerProfile.update({
        where: { userId },
        data: { xp: { increment: challenge.rewardXp } },
      }),
    ]);

    const currency = await prisma.currency.findUnique({ where: { userId } });
    const profile = await prisma.playerProfile.findUnique({ where: { userId } });

    res.json({
      success: true,
      data: {
        message: 'Reward claimed!',
        rewards: {
          coins: challenge.rewardCoins,
          xp: challenge.rewardXp,
        },
        newBalance: currency,
        newLevel: profile?.level,
      },
    });
  } catch (error) {
    next(error);
  }
});

router.post('/progress', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { type, amount = 1 } = req.body;

    await prisma.dailyChallenge.updateMany({
      where: {
        userId,
        challengeType: type,
        isCompleted: false,
        currentValue: { lt: prisma.dailyChallenge.fields.targetValue },
      },
      data: {
        currentValue: { increment: amount },
      },
    });

    const completedChallenges = await prisma.dailyChallenge.updateMany({
      where: {
        userId,
        isCompleted: false,
      },
      data: {},
    });

    await prisma.dailyChallenge.updateMany({
      where: {
        userId,
        currentValue: { gte: prisma.dailyChallenge.fields.targetValue },
        isCompleted: false,
      },
      data: { isCompleted: true },
    });

    res.json({
      success: true,
      data: { updated: true },
    });
  } catch (error) {
    next(error);
  }
});

router.post('/generate', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    
    const existingToday = await prisma.dailyChallenge.findFirst({
      where: {
        userId,
        createdAt: {
          gte: new Date(new Date().setHours(0, 0, 0, 0)),
        },
      },
    });

    if (existingToday) {
      return res.json({
        success: true,
        data: { message: 'Challenges already generated for today' },
      });
    }

    const shuffled = [...CHALLENGE_TEMPLATES].sort(() => Math.random() - 0.5);
    const selected = shuffled.slice(0, 3);

    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    tomorrow.setHours(0, 0, 0, 0);

    for (const template of selected) {
      await prisma.dailyChallenge.create({
        data: {
          userId,
          challengeType: template.type,
          description: template.description.replace('{value}', template.defaultTarget.toString()),
          targetValue: template.defaultTarget,
          currentValue: 0,
          rewardCoins: template.coins,
          rewardXp: template.xp,
          expiresAt: tomorrow,
        },
      });
    }

    const challenges = await prisma.dailyChallenge.findMany({
      where: {
        userId,
        expiresAt: tomorrow,
      },
    });

    res.json({
      success: true,
      data: challenges,
    });
  } catch (error) {
    next(error);
  }
});

export default router;
