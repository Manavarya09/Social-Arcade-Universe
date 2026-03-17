import { Router, Response, NextFunction } from 'express';
import { PrismaClient } from '@prisma/client';
import { authenticate, AuthRequest } from '../../shared/middleware/auth.middleware';

const router = Router();
const prisma = new PrismaClient();

router.get('/profile', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;

    const profile = await prisma.playerProfile.findUnique({
      where: { userId },
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

    if (!profile) {
      return res.status(404).json({
        success: false,
        error: { message: 'Profile not found' },
      });
    }

    res.json({
      success: true,
      data: profile,
    });
  } catch (error) {
    next(error);
  }
});

router.put('/progress', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { xp, level, wins, losses, gamesPlayed, totalPlaytime } = req.body;

    const updateData: any = {};
    if (xp !== undefined) updateData.xp = xp;
    if (level !== undefined) updateData.level = level;
    if (wins !== undefined) updateData.wins = wins;
    if (losses !== undefined) updateData.losses = losses;
    if (gamesPlayed !== undefined) updateData.gamesPlayed = gamesPlayed;
    if (totalPlaytime !== undefined) updateData.totalPlaytime = totalPlaytime;

    const profile = await prisma.playerProfile.update({
      where: { userId },
      data: updateData,
    });

    res.json({
      success: true,
      data: profile,
    });
  } catch (error) {
    next(error);
  }
});

router.get('/inventory', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;

    const items = await prisma.inventoryItem.findMany({
      where: { userId },
      orderBy: { acquiredAt: 'desc' },
    });

    res.json({
      success: true,
      data: items,
    });
  } catch (error) {
    next(error);
  }
});

router.post('/inventory/add', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { itemId, itemType, quantity } = req.body;

    const existingItem = await prisma.inventoryItem.findUnique({
      where: {
        userId_itemId: {
          userId,
          itemId,
        },
      },
    });

    if (existingItem) {
      const item = await prisma.inventoryItem.update({
        where: { id: existingItem.id },
        data: {
          quantity: existingItem.quantity + (quantity || 1),
        },
      });

      return res.json({
        success: true,
        data: item,
      });
    }

    const item = await prisma.inventoryItem.create({
      data: {
        userId,
        itemId,
        itemType,
        quantity: quantity || 1,
      },
    });

    res.json({
      success: true,
      data: item,
    });
  } catch (error) {
    next(error);
  }
});

router.post('/inventory/:itemId/equip', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { itemId } = req.params;

    await prisma.inventoryItem.updateMany({
      where: {
        userId,
        itemId,
      },
      data: {
        isEquipped: true,
      },
    });

    await prisma.inventoryItem.updateMany({
      where: {
        userId,
        NOT: { itemId },
      },
      data: {
        isEquipped: false,
      },
    });

    res.json({
      success: true,
      data: { message: 'Item equipped' },
    });
  } catch (error) {
    next(error);
  }
});

router.get('/stats', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;

    const profile = await prisma.playerProfile.findUnique({
      where: { userId },
    });

    if (!profile) {
      return res.status(404).json({
        success: false,
        error: { message: 'Profile not found' },
      });
    }

    res.json({
      success: true,
      data: {
        level: profile.level,
        xp: profile.xp,
        wins: profile.wins,
        losses: profile.losses,
        gamesPlayed: profile.gamesPlayed,
        totalPlaytime: profile.totalPlaytime,
      },
    });
  } catch (error) {
    next(error);
  }
});

export default router;
