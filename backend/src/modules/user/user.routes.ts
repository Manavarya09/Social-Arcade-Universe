import { Router, Response, NextFunction } from 'express';
import { PrismaClient } from '@prisma/client';
import { authenticate, AuthRequest } from '../../shared/middleware/auth.middleware';

const router = Router();
const prisma = new PrismaClient();

router.get('/me', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;

    const user = await prisma.user.findUnique({
      where: { id: userId },
      include: {
        playerProfile: true,
        currencies: true,
      },
    });

    if (!user) {
      return res.status(404).json({
        success: false,
        error: { message: 'User not found' },
      });
    }

    res.json({
      success: true,
      data: {
        id: user.id,
        email: user.email,
        username: user.username,
        avatarUrl: user.avatarUrl,
        bio: user.bio,
        createdAt: user.createdAt,
        playerProfile: user.playerProfile ? {
          displayName: user.playerProfile.displayName,
          level: user.playerProfile.level,
          xp: user.playerProfile.xp,
          wins: user.playerProfile.wins,
          losses: user.playerProfile.losses,
          gamesPlayed: user.playerProfile.gamesPlayed,
          totalPlaytime: user.playerProfile.totalPlaytime,
        } : null,
        currencies: user.currencies ? {
          coins: user.currencies.coins,
          gems: user.currencies.gems,
          premiumGems: user.currencies.premiumGems,
        } : null,
      },
    });
  } catch (error) {
    next(error);
  }
});

router.put('/me', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { username, avatarUrl, bio, displayName } = req.body;

    const user = await prisma.user.update({
      where: { id: userId },
      data: {
        username: username || undefined,
        avatarUrl: avatarUrl || undefined,
        bio: bio || undefined,
        playerProfile: displayName ? {
          update: { displayName },
        } : undefined,
      },
      include: {
        playerProfile: true,
        currencies: true,
      },
    });

    res.json({
      success: true,
      data: {
        id: user.id,
        email: user.email,
        username: user.username,
        avatarUrl: user.avatarUrl,
        bio: user.bio,
        playerProfile: user.playerProfile,
      },
    });
  } catch (error: any) {
    if (error.code === 'P2002') {
      return res.status(400).json({
        success: false,
        error: { message: 'Username already taken' },
      });
    }
    next(error);
  }
});

router.get('/:id', async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id } = req.params;

    const user = await prisma.user.findUnique({
      where: { id },
      select: {
        id: true,
        username: true,
        avatarUrl: true,
        bio: true,
        createdAt: true,
        playerProfile: {
          select: {
            displayName: true,
            level: true,
            wins: true,
            losses: true,
            gamesPlayed: true,
          },
        },
      },
    });

    if (!user) {
      return res.status(404).json({
        success: false,
        error: { message: 'User not found' },
      });
    }

    res.json({
      success: true,
      data: user,
    });
  } catch (error) {
    next(error);
  }
});

router.get('/:id/stats', async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id } = req.params;

    const profile = await prisma.playerProfile.findUnique({
      where: { userId: id },
    });

    if (!profile) {
      return res.status(404).json({
        success: false,
        error: { message: 'Player profile not found' },
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
        winRate: profile.gamesPlayed > 0 
          ? ((profile.wins / profile.gamesPlayed) * 100).toFixed(1) 
          : 0,
      },
    });
  } catch (error) {
    next(error);
  }
});

router.put('/me/avatar', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { avatarUrl } = req.body;

    const user = await prisma.user.update({
      where: { id: userId },
      data: { avatarUrl },
    });

    res.json({
      success: true,
      data: { avatarUrl: user.avatarUrl },
    });
  } catch (error) {
    next(error);
  }
});

export default router;
