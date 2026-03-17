import { PrismaClient } from '@prisma/client';
import bcrypt from 'bcryptjs';
import jwt from 'jsonwebtoken';
import { config } from '../../config/env';
import { AppError } from '../../shared/middleware/error.middleware';
import { logger } from '../../shared/utils/logger';

const prisma = new PrismaClient();

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
}

export interface RegisterData {
  email: string;
  username: string;
  password: string;
}

export interface LoginData {
  email: string;
  password: string;
}

export class AuthService {
  async register(data: RegisterData) {
    const { email, username, password } = data;

    const existingUser = await prisma.user.findFirst({
      where: {
        OR: [{ email }, { username }],
      },
    });

    if (existingUser) {
      throw new AppError(
        existingUser.email === email 
          ? 'Email already registered' 
          : 'Username already taken',
        400,
        'USER_EXISTS'
      );
    }

    const passwordHash = await bcrypt.hash(password, 12);

    const user = await prisma.user.create({
      data: {
        email,
        username,
        passwordHash,
        playerProfile: {
          create: {
            displayName: username,
            level: 1,
            xp: 0,
          },
        },
        currencies: {
          create: {
            coins: 1000,
            gems: 0,
            premiumGems: 0,
          },
        },
      },
      include: {
        playerProfile: true,
        currencies: true,
      },
    });

    const tokens = await this.generateTokens(user.id, user.email);

    logger.info(`User registered: ${user.id}`, { username: user.username });

    return {
      user: this.sanitizeUser(user),
      ...tokens,
    };
  }

  async login(data: LoginData) {
    const { email, password } = data;

    const user = await prisma.user.findUnique({
      where: { email },
      include: {
        playerProfile: true,
        currencies: true,
      },
    });

    if (!user || !user.passwordHash) {
      throw new AppError('Invalid credentials', 401, 'INVALID_CREDENTIALS');
    }

    if (user.isBanned) {
      throw new AppError('Account is banned', 403, 'ACCOUNT_BANNED', {
        reason: user.banReason,
      });
    }

    const isValidPassword = await bcrypt.compare(password, user.passwordHash);

    if (!isValidPassword) {
      throw new AppError('Invalid credentials', 401, 'INVALID_CREDENTIALS');
    }

    await prisma.user.update({
      where: { id: user.id },
      data: { lastLoginAt: new Date() },
    });

    const tokens = await this.generateTokens(user.id, user.email);

    logger.info(`User logged in: ${user.id}`);

    return {
      user: this.sanitizeUser(user),
      ...tokens,
    };
  }

  async refreshToken(refreshToken: string) {
    try {
      const jwt = require('jsonwebtoken');
      const payload = jwt.verify(refreshToken, config.jwt.refreshSecret) as { userId: string };

      const user = await prisma.user.findUnique({
        where: { id: payload.userId },
        include: {
          playerProfile: true,
          currencies: true,
        },
      });

      if (!user) {
        throw new AppError('User not found', 404, 'USER_NOT_FOUND');
      }

      if (user.isBanned) {
        throw new AppError('Account is banned', 403, 'ACCOUNT_BANNED');
      }

      const tokens = await this.generateTokens(user.id, user.email);

      return {
        user: this.sanitizeUser(user),
        ...tokens,
      };
    } catch (error) {
      if (error instanceof AppError) throw error;
      throw new AppError('Invalid refresh token', 401, 'INVALID_TOKEN');
    }
  }

  async logout(userId: string) {
    logger.info(`User logged out: ${userId}`);
    return { success: true };
  }

  async getOAuthUrl(provider: string) {
    const { google, apple } = config.oauth;
    
    switch (provider.toLowerCase()) {
      case 'google':
        const googleUrl = new URL('https://accounts.google.com/o/oauth2/v2/auth');
        googleUrl.searchParams.set('client_id', google?.clientId || '');
        googleUrl.searchParams.set('redirect_uri', google?.callbackUrl || '');
        googleUrl.searchParams.set('response_type', 'code');
        googleUrl.searchParams.set('scope', 'openid email profile');
        googleUrl.searchParams.set('state', provider);
        return { url: googleUrl.toString() };
      
      case 'apple':
        const appleUrl = new URL('https://appleid.apple.com/auth/authorize');
        appleUrl.searchParams.set('client_id', apple?.clientId || '');
        appleUrl.searchParams.set('redirect_uri', 'https://api.socialarcadeuniverse.com/auth/apple/callback');
        appleUrl.searchParams.set('response_type', 'code id_token');
        appleUrl.searchParams.set('scope', 'name email');
        appleUrl.searchParams.set('response_mode', 'form_post');
        return { url: appleUrl.toString() };
      
      default:
        throw new AppError('Unsupported OAuth provider', 400, 'UNSUPPORTED_PROVIDER');
    }
  }

  private async generateTokens(userId: string, email: string): Promise<AuthTokens> {
    const accessToken = jwt.sign(
      { userId, email },
      config.jwt.secret,
      { expiresIn: config.jwt.expiresIn }
    );

    const refreshToken = jwt.sign(
      { userId },
      config.jwt.refreshSecret,
      { expiresIn: config.jwt.refreshExpiresIn }
    );

    return { accessToken, refreshToken };
  }

  private sanitizeUser(user: any) {
    return {
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
      } : null,
      currencies: user.currencies ? {
        coins: user.currencies.coins,
        gems: user.currencies.gems,
        premiumGems: user.currencies.premiumGems,
      } : null,
    };
  }
}

export const authService = new AuthService();
