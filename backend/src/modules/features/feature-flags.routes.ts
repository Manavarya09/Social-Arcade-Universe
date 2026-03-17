import { Router, Response, NextFunction } from 'express';
import { PrismaClient } from '@prisma/client';
import { authenticate, optionalAuth, AuthRequest } from '../../shared/middleware/auth.middleware';
import { redis } from '../../config/redis';
import { v4 as uuidv4 } from 'uuid';

const router = Router();
const prisma = new PrismaClient();

interface FeatureFlag {
  id: string;
  key: string;
  description: string;
  enabled: boolean;
  rolloutPercentage: number;
  variants: Record<string, any>;
  createdAt: Date;
  updatedAt: Date;
}

interface ABExperiment {
  id: string;
  name: string;
  description: string;
  status: 'draft' | 'running' | 'paused' | 'completed';
  variants: { name: string; weight: number }[];
  startDate?: Date;
  endDate?: Date;
  metrics: Record<string, number>;
}

const DEFAULT_FLAGS: FeatureFlag[] = [
  {
    id: '1',
    key: 'new_home_screen',
    description: 'Redesigned home screen with reels',
    enabled: true,
    rolloutPercentage: 50,
    variants: {},
    createdAt: new Date(),
    updatedAt: new Date(),
  },
  {
    id: '2',
    key: 'premium_shop',
    description: 'New premium shop UI',
    enabled: true,
    rolloutPercentage: 100,
    variants: {},
    createdAt: new Date(),
    updatedAt: new Date(),
  },
  {
    id: '3',
    key: 'matchmaking_v2',
    description: 'Improved matchmaking algorithm',
    enabled: false,
    rolloutPercentage: 0,
    variants: {},
    createdAt: new Date(),
    updatedAt: new Date(),
  },
  {
    id: '4',
    key: 'reels_upload',
    description: 'Enable reel uploads',
    enabled: true,
    rolloutPercentage: 100,
    variants: {},
    createdAt: new Date(),
    updatedAt: new Date(),
  },
];

const DEFAULT_EXPERIMENTS: ABExperiment[] = [
  {
    id: uuidv4(),
    name: 'onboarding_flow',
    description: 'Test different onboarding experiences',
    status: 'running',
    variants: [
      { name: 'control', weight: 50 },
      { name: 'variant_a', weight: 25 },
      { name: 'variant_b', weight: 25 },
    ],
    metrics: {},
  },
];

router.get('/flags', optionalAuth, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const redisClient = redis.getClient();
    const userId = req.user?.userId || req.ip;

    let flags = await redisClient.get('feature_flags:all');
    
    if (!flags) {
      flags = JSON.stringify(DEFAULT_FLAGS);
      await redisClient.set('feature_flags:all', flags, 'EX', 3600);
    }

    const parsedFlags: FeatureFlag[] = JSON.parse(flags);

    const userHash = hashString(userId);
    const enrichedFlags = parsedFlags.map(flag => ({
      ...flag,
      isEnabledForUser: isEnabledForUser(flag, userHash),
      userVariant: getVariant(flag, userHash),
    }));

    res.json({
      success: true,
      data: enrichedFlags,
    });
  } catch (error) {
    next(error);
  }
});

router.get('/flags/:key', optionalAuth, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { key } = req.params;
    const userId = req.user?.userId || req.ip;

    const redisClient = redis.getClient();
    let flags = await redisClient.get('feature_flags:all');
    
    if (!flags) {
      flags = JSON.stringify(DEFAULT_FLAGS);
    }

    const parsedFlags: FeatureFlag[] = JSON.parse(flags);
    const flag = parsedFlags.find(f => f.key === key);

    if (!flag) {
      return res.status(404).json({
        success: false,
        error: { message: 'Flag not found' },
      });
    }

    const userHash = hashString(userId);

    res.json({
      success: true,
      data: {
        ...flag,
        isEnabledForUser: isEnabledForUser(flag, userHash),
        userVariant: getVariant(flag, userHash),
      },
    });
  } catch (error) {
    next(error);
  }
});

router.post('/flags', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { key, description, enabled, rolloutPercentage, variants } = req.body;

    const redisClient = redis.getClient();
    let flags = await redisClient.get('feature_flags:all');
    
    if (!flags) {
      flags = JSON.stringify(DEFAULT_FLAGS);
    }

    const parsedFlags: FeatureFlag[] = JSON.parse(flags);

    if (parsedFlags.some(f => f.key === key)) {
      return res.status(400).json({
        success: false,
        error: { message: 'Flag already exists' },
      });
    }

    const newFlag: FeatureFlag = {
      id: uuidv4(),
      key,
      description,
      enabled,
      rolloutPercentage: rolloutPercentage || 0,
      variants: variants || {},
      createdAt: new Date(),
      updatedAt: new Date(),
    };

    parsedFlags.push(newFlag);
    await redisClient.set('feature_flags:all', JSON.stringify(parsedFlags), 'EX', 3600);

    res.json({
      success: true,
      data: newFlag,
    });
  } catch (error) {
    next(error);
  }
});

router.put('/flags/:key', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { key } = req.params;
    const { enabled, rolloutPercentage, variants } = req.body;

    const redisClient = redis.getClient();
    let flags = await redisClient.get('feature_flags:all');
    
    if (!flags) {
      flags = JSON.stringify(DEFAULT_FLAGS);
    }

    const parsedFlags: FeatureFlag[] = JSON.parse(flags);
    const flagIndex = parsedFlags.findIndex(f => f.key === key);

    if (flagIndex === -1) {
      return res.status(404).json({
        success: false,
        error: { message: 'Flag not found' },
      });
    }

    parsedFlags[flagIndex] = {
      ...parsedFlags[flagIndex],
      ...(enabled !== undefined && { enabled }),
      ...(rolloutPercentage !== undefined && { rolloutPercentage }),
      ...(variants !== undefined && { variants }),
      updatedAt: new Date(),
    };

    await redisClient.set('feature_flags:all', JSON.stringify(parsedFlags), 'EX', 3600);

    res.json({
      success: true,
      data: parsedFlags[flagIndex],
    });
  } catch (error) {
    next(error);
  }
});

router.get('/experiments', optionalAuth, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user?.userId || req.ip;
    const userHash = hashString(userId);

    const experiments = DEFAULT_EXPERIMENTS.filter(e => e.status === 'running');

    const userExperiments = experiments.map(exp => ({
      ...exp,
      assignedVariant: getExperimentVariant(exp, userHash),
    }));

    res.json({
      success: true,
      data: userExperiments,
    });
  } catch (error) {
    next(error);
  }
});

router.post('/experiments/:id/conversion', optionalAuth, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id } = req.params;
    const { metric, value = 1 } = req.body;
    const userId = req.user?.userId || req.ip;

    const redisClient = redis.getClient();
    const key = `experiment:${id}:${metric}`;
    await redisClient.zincrby(key, value, userId);

    res.json({
      success: true,
    });
  } catch (error) {
    next(error);
  }
});

function hashString(str: string): number {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    const char = str.charCodeAt(i);
    hash = ((hash << 5) - hash) + char;
    hash = hash & hash;
  }
  return Math.abs(hash);
}

function isEnabledForUser(flag: FeatureFlag, userHash: number): boolean {
  if (!flag.enabled) return false;
  
  const percentage = flag.rolloutPercentage || 100;
  return (userHash % 100) < percentage;
}

function getVariant(flag: FeatureFlag, userHash: number): string | null {
  const variants = flag.variants;
  const keys = Object.keys(variants);
  
  if (keys.length === 0) return null;
  
  const index = userHash % keys.length;
  return keys[index];
}

function getExperimentVariant(experiment: ABExperiment, userHash: number): string {
  const totalWeight = experiment.variants.reduce((sum, v) => sum + v.weight, 0);
  let random = userHash % totalWeight;
  
  for (const variant of experiment.variants) {
    random -= variant.weight;
    if (random < 0) {
      return variant.name;
    }
  }
  
  return experiment.variants[0].name;
}

export default router;
