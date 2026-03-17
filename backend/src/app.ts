import express, { Application, Request, Response, NextFunction } from 'express';
import cors from 'cors';
import helmet from 'helmet';
import compression from 'compression';
import cookieParser from 'cookie-parser';
import rateLimit from 'express-rate-limit';
import morgan from 'morgan';
import { errorMiddleware } from './shared/middleware/error.middleware';
import { logger } from './shared/utils/logger';

import authRoutes from './modules/auth/auth.routes';
import userRoutes from './modules/user/user.routes';
import playerRoutes from './modules/player/player.routes';
import gameRoutes from './modules/game/game.routes';
import matchmakingRoutes from './modules/matchmaking/matchmaking.routes';
import socialRoutes from './modules/social/social.routes';
import reelsRoutes from './modules/social/reels/reels.routes';
import economyRoutes from './modules/economy/economy.routes';
import shopRoutes from './modules/economy/shop/shop.routes';
import leaderboardRoutes from './modules/leaderboard/leaderboard.routes';
import adminRoutes from './modules/admin/admin.routes';

export const app: Application = express();

app.use(helmet({
  contentSecurityPolicy: false,
  crossOriginEmbedderPolicy: false,
}));

app.use(cors({
  origin: process.env.CLIENT_URL || '*',
  credentials: true,
  methods: ['GET', 'POST', 'PUT', 'DELETE', 'PATCH'],
  allowedHeaders: ['Content-Type', 'Authorization', 'X-Requested-With'],
}));

app.use(compression());

app.use(express.json({ limit: '10mb' }));
app.use(express.urlencoded({ extended: true, limit: '10mb' }));
app.use(cookieParser());

app.use(morgan('combined', {
  stream: {
    write: (message: string) => logger.info(message.trim()),
  },
}));

const apiLimiter = rateLimit({
  windowMs: 15 * 60 * 1000,
  max: 1000,
  message: { error: 'Too many requests, please try again later' },
  standardHeaders: true,
  legacyHeaders: false,
});

app.use('/api', apiLimiter);

app.get('/health', (_req: Request, res: Response) => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() });
});

app.use('/api/auth', authRoutes);
app.use('/api/users', userRoutes);
app.use('/api/player', playerRoutes);
app.use('/api/games', gameRoutes);
app.use('/api/matchmaking', matchmakingRoutes);
app.use('/api/social', socialRoutes);
app.use('/api/reels', reelsRoutes);
app.use('/api/economy', economyRoutes);
app.use('/api/shop', shopRoutes);
app.use('/api/leaderboard', leaderboardRoutes);
app.use('/api/admin', adminRoutes);

app.use((_req: Request, res: Response) => {
  res.status(404).json({ error: 'Endpoint not found' });
});

app.use(errorMiddleware);
