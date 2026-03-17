import { Router, Response, NextFunction } from 'express';
import { PrismaClient } from '@prisma/client';
import { authenticate, optionalAuth, AuthRequest } from '../../shared/middleware/auth.middleware';
import multer from 'multer';
import path from 'path';
import { v4 as uuidv4 } from 'uuid';
import fs from 'fs';

const router = Router();
const prisma = new PrismaClient();

const storage = multer.diskStorage({
  destination: (_req, _file, cb) => {
    const uploadDir = 'uploads/reels';
    if (!fs.existsSync(uploadDir)) {
      fs.mkdirSync(uploadDir, { recursive: true });
    }
    cb(null, uploadDir);
  },
  filename: (_req, file, cb) => {
    const uniqueName = `${uuidv4()}${path.extname(file.originalname)}`;
    cb(null, uniqueName);
  },
});

const upload = multer({
  storage,
  limits: {
    fileSize: 100 * 1024 * 1024,
  },
  fileFilter: (_req, file, cb) => {
    const allowedTypes = ['video/mp4', 'video/webm', 'video/quicktime'];
    if (allowedTypes.includes(file.mimetype)) {
      cb(null, true);
    } else {
      cb(new Error('Invalid file type. Only MP4, WebM, and MOV are allowed.'));
    }
  },
});

router.get('/', optionalAuth, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { page = 1, limit = 20, gameMode, userId } = req.query;
    const userIdAuth = req.user?.userId;

    const where: any = { isPublic: true };

    if (gameMode) {
      where.gameMode = gameMode;
    }

    if (userId) {
      where.userId = userId;
    }

    const skip = (parseInt(page as string) - 1) * parseInt(limit as string);

    const [reels, total] = await Promise.all([
      prisma.reel.findMany({
        where,
        include: {
          user: {
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
        orderBy: [
          { isFeatured: 'desc' },
          { createdAt: 'desc' },
        ],
        skip,
        take: parseInt(limit as string),
      }),
      prisma.reel.count({ where }),
    ]);

    let reelsWithLiked = reels.map(reel => ({
      ...reel,
      isLiked: false,
    }));

    if (userIdAuth) {
      const likedReelIds = await prisma.reelLike.findMany({
        where: {
          userId: userIdAuth,
          reelId: { in: reels.map(r => r.id) },
        },
        select: { reelId: true },
      });

      const likedSet = new Set(likedReelIds.map(l => l.reelId));
      reelsWithLiked = reelsWithLiked.map(reel => ({
        ...reel,
        isLiked: likedSet.has(reel.id),
      }));
    }

    res.json({
      success: true,
      data: {
        reels: reelsWithLiked,
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

router.post('/', authenticate, upload.single('video'), async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { title, description, gameMode, isPublic } = req.body;

    if (!req.file) {
      return res.status(400).json({
        success: false,
        error: { message: 'Video file is required' },
      });
    }

    const videoUrl = `/uploads/reels/${req.file.filename}`;
    const thumbnailUrl = `/uploads/reels/thumbnails/${req.file.filename.replace(path.extname(req.file.filename), '.jpg')}`;

    const reel = await prisma.reel.create({
      data: {
        userId,
        videoUrl,
        thumbnailUrl,
        title,
        description,
        gameMode,
        isPublic: isPublic !== 'false',
        duration: 0,
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
      data: reel,
    });
  } catch (error) {
    next(error);
  }
});

router.get('/:id', optionalAuth, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id } = req.params;
    const userIdAuth = req.user?.userId;

    const reel = await prisma.reel.findUnique({
      where: { id },
      include: {
        user: {
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

    if (!reel) {
      return res.status(404).json({
        success: false,
        error: { message: 'Reel not found' },
      });
    }

    await prisma.reel.update({
      where: { id },
      data: { views: { increment: 1 } },
    });

    let isLiked = false;
    if (userIdAuth) {
      const like = await prisma.reelLike.findUnique({
        where: {
          reelId_userId: {
            reelId: id,
            userId: userIdAuth,
          },
        },
      });
      isLiked = !!like;
    }

    res.json({
      success: true,
      data: {
        ...reel,
        views: reel.views + 1,
        isLiked,
      },
    });
  } catch (error) {
    next(error);
  }
});

router.delete('/:id', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { id } = req.params;

    const reel = await prisma.reel.findUnique({
      where: { id },
    });

    if (!reel) {
      return res.status(404).json({
        success: false,
        error: { message: 'Reel not found' },
      });
    }

    if (reel.userId !== userId) {
      return res.status(403).json({
        success: false,
        error: { message: 'Not authorized to delete this reel' },
      });
    }

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

router.post('/:id/like', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { id } = req.params;

    const reel = await prisma.reel.findUnique({
      where: { id },
    });

    if (!reel) {
      return res.status(404).json({
        success: false,
        error: { message: 'Reel not found' },
      });
    }

    const existingLike = await prisma.reelLike.findUnique({
      where: {
        reelId_userId: {
          reelId: id,
          userId,
        },
      },
    });

    if (existingLike) {
      await prisma.$transaction([
        prisma.reelLike.delete({
          where: { id: existingLike.id },
        }),
        prisma.reel.update({
          where: { id },
          data: { likes: { decrement: 1 } },
        }),
      ]);

      return res.json({
        success: true,
        data: { liked: false },
      });
    }

    await prisma.$transaction([
      prisma.reelLike.create({
        data: {
          reelId: id,
          userId,
        },
      }),
      prisma.reel.update({
        where: { id },
        data: { likes: { increment: 1 } },
      }),
    ]);

    res.json({
      success: true,
      data: { liked: true },
    });
  } catch (error) {
    next(error);
  }
});

router.get('/:id/comments', async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id } = req.params;
    const { page = 1, limit = 20 } = req.query;

    const skip = (parseInt(page as string) - 1) * parseInt(limit as string);

    const [comments, total] = await Promise.all([
      prisma.reelComment.findMany({
        where: { reelId: id },
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
        skip,
        take: parseInt(limit as string),
      }),
      prisma.reelComment.count({ where: { reelId: id } }),
    ]);

    res.json({
      success: true,
      data: {
        comments,
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

router.post('/:id/comments', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { id } = req.params;
    const { content } = req.body;

    if (!content || content.trim().length === 0) {
      return res.status(400).json({
        success: false,
        error: { message: 'Comment content is required' },
      });
    }

    const reel = await prisma.reel.findUnique({
      where: { id },
    });

    if (!reel) {
      return res.status(404).json({
        success: false,
        error: { message: 'Reel not found' },
      });
    }

    const [comment] = await Promise.all([
      prisma.reelComment.create({
        data: {
          reelId: id,
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
      }),
      prisma.reel.update({
        where: { id },
        data: { commentsCount: { increment: 1 } },
      }),
    ]);

    res.json({
      success: true,
      data: comment,
    });
  } catch (error) {
    next(error);
  }
});

export default router;
