import { Router, Request, Response, NextFunction } from 'express';
import { authService, RegisterData, LoginData } from './auth.service';
import { validateBody } from '../../shared/decorators/validate.decorator';
import { authenticate } from '../../shared/middleware/auth.middleware';

const router = Router();

class AuthController {
  async register(req: Request, res: Response, next: NextFunction) {
    try {
      const data: RegisterData = req.body;
      const result = await authService.register(data);
      
      res.cookie('refreshToken', result.refreshToken, {
        httpOnly: true,
        secure: process.env.NODE_ENV === 'production',
        sameSite: 'strict',
        maxAge: 7 * 24 * 60 * 60 * 1000,
      });

      res.status(201).json({
        success: true,
        data: {
          user: result.user,
          accessToken: result.accessToken,
        },
      });
    } catch (error) {
      next(error);
    }
  }

  async login(req: Request, res: Response, next: NextFunction) {
    try {
      const data: LoginData = req.body;
      const result = await authService.login(data);
      
      res.cookie('refreshToken', result.refreshToken, {
        httpOnly: true,
        secure: process.env.NODE_ENV === 'production',
        sameSite: 'strict',
        maxAge: 7 * 24 * 60 * 60 * 1000,
      });

      res.json({
        success: true,
        data: {
          user: result.user,
          accessToken: result.accessToken,
        },
      });
    } catch (error) {
      next(error);
    }
  }

  async refresh(req: Request, res: Response, next: NextFunction) {
    try {
      const refreshToken = req.cookies.refreshToken || req.body.refreshToken;
      
      if (!refreshToken) {
        return res.status(401).json({
          success: false,
          error: { message: 'Refresh token required' },
        });
      }

      const result = await authService.refreshToken(refreshToken);

      res.cookie('refreshToken', result.refreshToken, {
        httpOnly: true,
        secure: process.env.NODE_ENV === 'production',
        sameSite: 'strict',
        maxAge: 7 * 24 * 60 * 60 * 1000,
      });

      res.json({
        success: true,
        data: {
          user: result.user,
          accessToken: result.accessToken,
        },
      });
    } catch (error) {
      next(error);
    }
  }

  async logout(req: Request, res: Response, next: NextFunction) {
    try {
      const userId = (req as any).user?.userId;
      if (userId) {
        await authService.logout(userId);
      }

      res.clearCookie('refreshToken');

      res.json({
        success: true,
        data: { message: 'Logged out successfully' },
      });
    } catch (error) {
      next(error);
    }
  }

  async getOAuthUrl(req: Request, res: Response, next: NextFunction) {
    try {
      const { provider } = req.params;
      const result = await authService.getOAuthUrl(provider);
      
      res.json({
        success: true,
        data: result,
      });
    } catch (error) {
      next(error);
    }
  }
}

const authController = new AuthController();

router.post('/register', validateBody(['email', 'username', 'password']), authController.register.bind(authController));
router.post('/login', validateBody(['email', 'password']), authController.login.bind(authController));
router.post('/refresh', authController.refresh.bind(authController));
router.post('/logout', authenticate, authController.logout.bind(authController));
router.get('/oauth/:provider', authController.getOAuthUrl.bind(authController));

export default router;
