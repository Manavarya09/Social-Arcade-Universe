import { Router, Response, NextFunction } from 'express';
import { authenticate, AuthRequest } from '../../shared/middleware/auth.middleware';
import { GameService } from './game.service';

const router = Router();
const gameService = new GameService();

router.get('/modes', async (_req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const modes = [
      {
        modeId: 'battle_royale',
        name: 'Battle Royale',
        description: 'Last player standing wins',
        minPlayers: 2,
        maxPlayers: 16,
        isTeamBased: false,
        iconUrl: '/icons/battle-royale.png',
      },
      {
        modeId: 'deathmatch',
        name: 'Deathmatch',
        description: 'Most kills wins',
        minPlayers: 2,
        maxPlayers: 8,
        isTeamBased: false,
        iconUrl: '/icons/deathmatch.png',
      },
      {
        modeId: 'team_deathmatch',
        name: 'Team Deathmatch',
        description: 'Team vs Team - Most kills wins',
        minPlayers: 4,
        maxPlayers: 8,
        isTeamBased: true,
        iconUrl: '/icons/team-deathmatch.png',
      },
      {
        modeId: 'capture_flag',
        name: 'Capture the Flag',
        description: 'Capture enemy flags to score',
        minPlayers: 4,
        maxPlayers: 8,
        isTeamBased: true,
        iconUrl: '/icons/capture-flag.png',
      },
      {
        modeId: 'race',
        name: 'Race',
        description: 'First to the finish line',
        minPlayers: 1,
        maxPlayers: 8,
        isTeamBased: false,
        iconUrl: '/icons/race.png',
      },
    ];

    res.json({
      success: true,
      data: modes,
    });
  } catch (error) {
    next(error);
  }
});

router.post('/create', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { gameMode, settings } = req.body;
    const hostId = req.user!.userId;

    if (!gameMode) {
      return res.status(400).json({
        success: false,
        error: { message: 'gameMode is required' },
      });
    }

    const session = await gameService.createGameSession(gameMode, hostId, settings);

    res.json({
      success: true,
      data: session,
    });
  } catch (error) {
    next(error);
  }
});

router.get('/:id', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id } = req.params;
    const session = await gameService.getGameSession(id);

    if (!session) {
      return res.status(404).json({
        success: false,
        error: { message: 'Game session not found' },
      });
    }

    res.json({
      success: true,
      data: session,
    });
  } catch (error) {
    next(error);
  }
});

router.post('/:id/join', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id } = req.params;
    const userId = req.user!.userId;

    const session = await gameService.joinGame(id, userId);

    res.json({
      success: true,
      data: session,
    });
  } catch (error: any) {
    next(new Error(error.message));
  }
});

router.post('/:id/leave', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id } = req.params;
    const userId = req.user!.userId;

    await gameService.leaveGame(id, userId);

    res.json({
      success: true,
      data: { message: 'Left game' },
    });
  } catch (error) {
    next(error);
  }
});

router.post('/:id/ready', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id } = req.params;
    const { ready } = req.body;
    const userId = req.user!.userId;

    const result = await gameService.setPlayerReady(id, userId, ready);

    res.json({
      success: true,
      data: result,
    });
  } catch (error) {
    next(error);
  }
});

router.post('/:id/start', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const { id } = req.params;
    const userId = req.user!.userId;

    const session = await gameService.getGameSession(id);

    if (!session) {
      return res.status(404).json({
        success: false,
        error: { message: 'Game session not found' },
      });
    }

    if (session.hostId !== userId) {
      return res.status(403).json({
        success: false,
        error: { message: 'Only host can start the game' },
      });
    }

    const result = await gameService.startGame(id);

    res.json({
      success: true,
      data: result,
    });
  } catch (error) {
    next(error);
  }
});

router.get('/active/me', authenticate, async (req: AuthRequest, res: Response, next: NextFunction) => {
  try {
    const userId = req.user!.userId;
    const session = await gameService.getActiveGame(userId);

    res.json({
      success: true,
      data: session,
    });
  } catch (error) {
    next(error);
  }
});

export default router;
