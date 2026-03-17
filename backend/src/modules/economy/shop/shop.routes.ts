import { Router, Response, NextFunction } from 'express';
import { PrismaClient } from '@prisma/client';
import { authenticate, AuthRequest } from '../../shared/middleware/auth.middleware';

const router = Router();
const prisma = new PrismaClient();

router.get('/items', async (_req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { type, rarity } = _req.query;

    const where: any = {};
    if (type) where.type = type;
    if (rarity) where.rarity = rarity;

    const items = await prisma.shopItem.findMany({
      where,
      orderBy: { createdAt: 'desc' },
    });

    res.json({
      success: true,
      data: items,
    });
  } catch (error) {
    next(error);
  }
});

router.get('/categories', async (_req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const categories = await prisma.shopItem.groupBy({
      by: ['type'],
      _count: {
        type: true,
      },
    });

    res.json({
      success: true,
      data: categories.map(c => ({
        type: c.type,
        count: c._count.type,
      })),
    });
  } catch (error) {
    next(error);
  }
});

router.post('/purchase', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { itemId } = req.body;

    const item = await prisma.shopItem.findUnique({
      where: { itemId },
    });

    if (!item) {
      return res.status(404).json({
        success: false,
        error: { message: 'Item not found' },
      });
    }

    const currency = await prisma.currency.findUnique({
      where: { userId },
    });

    if (!currency) {
      return res.status(404).json({
        success: false,
        error: { message: 'Currency not found' },
      });
    }

    const priceCoins = item.priceCoins || 0;
    const priceGems = item.priceGems || 0;

    if (currency.coins < priceCoins || currency.gems < priceGems) {
      return res.status(400).json({
        success: false,
        error: { message: 'Insufficient currency' },
      });
    }

    const existingItem = await prisma.inventoryItem.findUnique({
      where: {
        userId_itemId: {
          userId,
          itemId: item.itemId,
        },
      },
    });

    if (existingItem) {
      await prisma.$transaction([
        prisma.currency.update({
          where: { userId },
          data: {
            coins: { decrement: priceCoins },
            gems: { decrement: priceGems },
          },
        }),
        prisma.inventoryItem.update({
          where: { id: existingItem.id },
          data: { quantity: { increment: 1 } },
        }),
      ]);
    } else {
      await prisma.$transaction([
        prisma.currency.update({
          where: { userId },
          data: {
            coins: { decrement: priceCoins },
            gems: { decrement: priceGems },
          },
        }),
        prisma.inventoryItem.create({
          data: {
            userId,
            itemId: item.itemId,
            itemType: item.type,
            quantity: 1,
          },
        }),
      ]);
    }

    const updatedCurrency = await prisma.currency.findUnique({
      where: { userId },
    });

    res.json({
      success: true,
      data: {
        message: 'Purchase successful',
        currency: updatedCurrency,
      },
    });
  } catch (error) {
    next(error);
  }
});

export default router;
