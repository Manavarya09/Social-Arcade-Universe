import { Router, Response, NextFunction } from 'express';
import { PrismaClient } from '@prisma/client';
import { authenticate, AuthRequest } from '../../shared/middleware/auth.middleware';

const router = Router();
const prisma = new PrismaClient();

router.get('/balance', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;

    let currency = await prisma.currency.findUnique({
      where: { userId },
    });

    if (!currency) {
      currency = await prisma.currency.create({
        data: {
          userId,
          coins: 1000,
          gems: 0,
          premiumGems: 0,
        },
      });
    }

    res.json({
      success: true,
      data: {
        coins: currency.coins,
        gems: currency.gems,
        premiumGems: currency.premiumGems,
      },
    });
  } catch (error) {
    next(error);
  }
});

router.post('/add-coins', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { amount } = req.body;

    if (!amount || amount <= 0) {
      return res.status(400).json({
        success: false,
        error: { message: 'Valid amount required' },
      });
    }

    const currency = await prisma.currency.update({
      where: { userId },
      data: {
        coins: { increment: amount },
      },
    });

    res.json({
      success: true,
      data: { coins: currency.coins },
    });
  } catch (error) {
    next(error);
  }
});

router.post('/add-gems', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { amount, isPremium } = req.body;

    if (!amount || amount <= 0) {
      return res.status(400).json({
        success: false,
        error: { message: 'Valid amount required' },
      });
    }

    const currency = await prisma.currency.update({
      where: { userId },
      data: isPremium ? {
        premiumGems: { increment: amount },
      } : {
        gems: { increment: amount },
      },
    });

    res.json({
      success: true,
      data: isPremium ? { premiumGems: currency.premiumGems } : { gems: currency.gems },
    });
  } catch (error) {
    next(error);
  }
});

router.post('/spend', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { coins, gems, reason } = req.body;

    const currency = await prisma.currency.findUnique({
      where: { userId },
    });

    if (!currency) {
      return res.status(404).json({
        success: false,
        error: { message: 'Currency not found' },
      });
    }

    if (coins && currency.coins < coins) {
      return res.status(400).json({
        success: false,
        error: { message: 'Insufficient coins' },
      });
    }

    if (gems && currency.gems < gems) {
      return res.status(400).json({
        success: false,
        error: { message: 'Insufficient gems' },
      });
    }

    const updateData: any = {};
    if (coins) updateData.coins = { decrement: coins };
    if (gems) updateData.gems = { decrement: gems };

    const updated = await prisma.currency.update({
      where: { userId },
      data: updateData,
    });

    res.json({
      success: true,
      data: {
        coins: updated.coins,
        gems: updated.gems,
        premiumGems: updated.premiumGems,
      },
    });
  } catch (error) {
    next(error);
  }
});

export default router;
