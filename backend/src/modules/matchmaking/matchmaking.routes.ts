import { Router, Response, NextFunction } from 'express';
import { authenticate, AuthRequest } from '../../shared/middleware/auth.middleware';
import { MatchmakingService } from './matchmaking.service';

const router = Router();
const matchmakingService = new MatchmakingService();

router.post('/queue', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { gameMode, rating } = req.body;
    const userId = req.user!.userId;

    if (!gameMode) {
      return res.status(400).json({
        success: false,
        error: { message: 'gameMode is required' },
      });
    }

    const playerRating = rating || 1000;
    const match = await matchmakingService.addToQueue(userId, gameMode, playerRating);

    if (match) {
      return res.json({
        success: true,
        data: { match, status: 'found' },
      });
    }

    const position = await matchmakingService.getQueuePosition(userId, gameMode);

    res.json({
      success: true,
      data: {
        status: 'searching',
        position,
        gameMode,
      },
    });
  } catch (error) {
    next(error);
  }
});

router.delete('/queue', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    await matchmakingService.removeFromQueue(userId);

    res.json({
      success: true,
      data: { message: 'Left matchmaking queue' },
    });
  } catch (error) {
    next(error);
  }
});

router.get('/status', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const { gameMode } = req.query;

    if (!gameMode) {
      return res.status(400).json({
        success: false,
        error: { message: 'gameMode is required' },
      });
    }

    const position = await matchmakingService.getQueuePosition(userId, gameMode as string);

    res.json({
      success: true,
      data: {
        inQueue: position > 0,
        position,
        gameMode,
      },
    });
  } catch (error) {
    next(error);
  }
});

export default router;
