import { Router, Response, NextFunction } from 'express';
import { PrismaClient } from '@prisma/client';
import { authenticate } from '../../shared/middleware/auth.middleware';

const router = Router();
const prisma = new PrismaClient();

router.use(authenticate);

router.get('/users', async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { page = 1, limit = 20, search } = req.query;

    const where: any = {};
    if (search) {
      where.OR = [
        { username: { contains: search as string, mode: 'insensitive' } },
        { email: { contains: search as string, mode: 'insensitive' } },
      ];
    }

    const [users, total] = await Promise.all([
      prisma.user.findMany({
        where,
        select: {
          id: true,
          email: true,
          username: true,
          avatarUrl: true,
          isBanned: true,
          createdAt: true,
          lastLoginAt: true,
        },
        skip: (parseInt(page as string) - 1) * parseInt(limit as string),
        take: parseInt(limit as string),
        orderBy: { createdAt: 'desc' },
      }),
      prisma.user.count({ where }),
    ]);

    res.json({
      success: true,
      data: {
        users,
        pagination: {
          page: parseInt(page as string),
          limit: parseInt(limit as string),
          total,
          pages: Math.ceil(total / parseInt(limit as string)),
        },
      },
    });
  } catch (error) {
    next(error);
  }
});

router.post('/users/:id/ban', async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id } = req.params;
    const { reason } = req.body;

    const user = await prisma.user.update({
      where: { id },
      data: {
        isBanned: true,
        banReason: reason,
      },
    });

    res.json({
      success: true,
      data: { message: 'User banned', userId: user.id },
    });
  } catch (error) {
    next(error);
  }
});

router.post('/users/:id/unban', async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id } = req.params;

    await prisma.user.update({
      where: { id },
      data: {
        isBanned: false,
        banReason: null,
      },
    });

    res.json({
      success: true,
      data: { message: 'User unbanned' },
    });
  } catch (error) {
    next(error);
  }
});

router.get('/reels', async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { page = 1, limit = 20, featured } = req.query;

    const where: any = {};
    if (featured !== undefined) where.isFeatured = featured === 'true';

    const [reels, total] = await Promise.all([
      prisma.reel.findMany({
        where,
        include: {
          user: {
            select: {
              id: true,
              username: true,
            },
          },
        },
        skip: (parseInt(page as string) - 1) * parseInt(limit as string),
        take: parseInt(limit as string),
        orderBy: { createdAt: 'desc' },
      }),
      prisma.reel.count({ where }),
    ]);

    res.json({
      success: true,
      data: {
        reels,
        pagination: {
          page: parseInt(page as string),
          limit: parseInt(limit as string),
          total,
          pages: Math.ceil(total / parseInt(limit as string)),
        },
      },
    });
  } catch (error) {
    next(error);
  }
});

router.post('/reels/:id/feature', async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id } = req.params;
    const { featured } = req.body;

    await prisma.reel.update({
      where: { id },
      data: { isFeatured: featured },
    });

    res.json({
      success: true,
      data: { message: `Reel ${featured ? 'featured' : 'unfeatured'}` },
    });
  } catch (error) {
    next(error);
  }
});

router.delete('/reels/:id', async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id } = req.params;

    await prisma.reel.delete({
      where: { id },
    });

    res.json({
      success: true,
      data: { message: 'Reel deleted' },
    });
  } catch (error) {
    next(error);
  }
});

router.get('/stats', async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const [totalUsers, totalReels, totalGames, activeUsers] = await Promise.all([
      prisma.user.count(),
      prisma.reel.count(),
      prisma.gameSession.count(),
      prisma.user.count({
        where: {
          lastLoginAt: {
            gte: new Date(Date.now() - 24 * 60 * 60 * 1000),
          },
        },
      }),
    ]);

    res.json({
      success: true,
      data: {
        totalUsers,
        totalReels,
        totalGames,
        activeUsers24h: activeUsers,
      },
    });
  } catch (error) {
    next(error);
  }
});

export default router;
