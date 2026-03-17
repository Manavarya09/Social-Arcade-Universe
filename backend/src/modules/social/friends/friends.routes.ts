import { Router, Response, NextFunction } from 'express';
import { PrismaClient } from '@prisma/client';
import { authenticate, AuthRequest } from '../../shared/middleware/auth.middleware';

const router = Router();
const prisma = new PrismaClient();

router.get('/friends', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;

    const friends = await prisma.friend.findMany({
      where: {
        userId,
        status: 'accepted',
      },
      include: {
        friend: {
          select: {
            id: true,
            username: true,
            avatarUrl: true,
            playerProfile: {
              select: {
                displayName: true,
                level: true,
              },
            },
          },
        },
      },
    });

    res.json({
      success: true,
      data: friends.map(f => f.friend),
    });
  } catch (error) {
    next(error);
  }
});

router.post('/request', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { friendId } = req.body;

    if (userId === friendId) {
      return res.status(400).json({
        success: false,
        error: { message: 'Cannot send friend request to yourself' },
      });
    }

    const existing = await prisma.friend.findFirst({
      where: {
        OR: [
          { userId, friendId },
          { userId: friendId, friendId: userId },
        ],
      },
    });

    if (existing) {
      return res.status(400).json({
        success: false,
        error: { message: 'Friend request already exists' },
      });
    }

    const friendRequest = await prisma.friendRequest.create({
      data: {
        senderId: userId,
        receiverId: friendId,
      },
    });

    res.json({
      success: true,
      data: friendRequest,
    });
  } catch (error) {
    next(error);
  }
});

router.get('/requests', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;

    const requests = await prisma.friendRequest.findMany({
      where: {
        receiverId: userId,
        status: 'pending',
      },
      include: {
        sender: {
          select: {
            id: true,
            username: true,
            avatarUrl: true,
            playerProfile: {
              select: {
                displayName: true,
                level: true,
              },
            },
          },
        },
      },
      orderBy: { createdAt: 'desc' },
    });

    res.json({
      success: true,
      data: requests,
    });
  } catch (error) {
    next(error);
  }
});

router.put('/requests/:id', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { id } = req.params;
    const { status } = req.body;

    if (!['accepted', 'rejected'].includes(status)) {
      return res.status(400).json({
        success: false,
        error: { message: 'Invalid status' },
      });
    }

    const request = await prisma.friendRequest.findUnique({
      where: { id },
    });

    if (!request || request.receiverId !== userId) {
      return res.status(404).json({
        success: false,
        error: { message: 'Request not found' },
      });
    }

    if (status === 'accepted') {
      await prisma.$transaction([
        prisma.friendRequest.update({
          where: { id },
          data: { status },
        }),
        prisma.friend.create({
          data: {
            userId: request.senderId,
            friendId: userId,
            status: 'accepted',
          },
        }),
        prisma.friend.create({
          data: {
            userId,
            friendId: request.senderId,
            status: 'accepted',
          },
        }),
      ]);
    } else {
      await prisma.friendRequest.update({
        where: { id },
        data: { status },
      });
    }

    res.json({
      success: true,
      data: { message: `Request ${status}` },
    });
  } catch (error) {
    next(error);
  }
});

router.delete('/friends/:friendId', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { friendId } = req.params;

    await prisma.friend.deleteMany({
      where: {
        OR: [
          { userId, friendId },
          { userId: friendId, friendId: userId },
        ],
      },
    });

    res.json({
      success: true,
      data: { message: 'Friend removed' },
    });
  } catch (error) {
    next(error);
  }
});

export default router;
