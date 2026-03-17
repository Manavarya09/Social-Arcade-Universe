import { Router, Response, NextFunction } from 'express';
import { PrismaClient } from '@prisma/client';
import { authenticate, AuthRequest } from '../../../shared/middleware/auth.middleware';

const router = Router();
const prisma = new PrismaClient();

router.get('/:roomId', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { roomId } = req.params;
    const { limit = 50, before } = req.query;

    const messages = await prisma.chatMessage.findMany({
      where: {
        roomId,
        ...(before ? { createdAt: { lt: new Date(before as string) } } : {}),
      },
      include: {
        user: {
          select: {
            id: true,
            username: true,
            avatarUrl: true,
          },
        },
      },
      orderBy: { createdAt: 'desc' },
      take: parseInt(limit as string),
    });

    res.json({
      success: true,
      data: messages.reverse(),
    });
  } catch (error) {
    next(error);
  }
});

router.post('/:roomId', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { roomId } = req.params;
    const { content } = req.body;

    if (!content || content.trim().length === 0) {
      return res.status(400).json({
        success: false,
        error: { message: 'Message content is required' },
      });
    }

    if (content.length > 500) {
      return res.status(400).json({
        success: false,
        error: { message: 'Message too long (max 500 characters)' },
      });
    }

    const message = await prisma.chatMessage.create({
      data: {
        roomId,
        userId,
        content: content.trim(),
      },
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

    res.json({
      success: true,
      data: message,
    });
  } catch (error) {
    next(error);
  }
});

export default router;
