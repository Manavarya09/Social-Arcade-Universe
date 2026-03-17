import { PrismaClient } from '@prisma/client';
import { logger } from '../shared/utils/logger';
import { redis } from '../config/redis';

const prisma = new PrismaClient();

interface PlayerMetrics {
  userId: string;
  sessionId: string;
  actionsPerSecond: number;
  totalActions: number;
  suspiciousPatterns: string[];
  riskScore: number;
}

interface PositionReport {
  odId: string;
  position: { x: number; y: number; z: number };
  timestamp: number;
  velocity: number;
}

export class AntiCheatService {
  private readonly RATE_LIMIT_ACTIONS = 20;
  private readonly MAX_TELEPORT_DISTANCE = 50;
  private readonly MAX_VELOCITY = 20;
  private readonly SUSPICIOUS_SCORE_THRESHOLD = 50;

  async analyzePlayerAction(userId: string, sessionId: string, action: any): Promise<boolean> {
    const redisClient = redis.getClient();
    const key = `anticheat:${sessionId}:${userId}`;
    
    const actionCount = await redisClient.incr(key);
    if (actionCount === 1) {
      await redisClient.expire(key, 60);
    }

    if (actionCount > this.RATE_LIMIT_ACTIONS) {
      await this.logSuspiciousActivity(userId, sessionId, 'RATE_LIMIT_EXCEEDED', {
        actionCount,
        limit: this.RATE_LIMIT_ACTIONS,
      });
      return false;
    }

    if (action.type === 'movement') {
      return this.validateMovement(userId, sessionId, action.data);
    }

    if (action.type === 'damage') {
      return this.validateDamage(userId, sessionId, action.data);
    }

    if (action.type === 'position') {
      return await this.validatePosition(userId, sessionId, action.data);
    }

    return true;
  }

  private validateMovement(userId: string, sessionId: string, data: any): boolean {
    const velocity = data.velocity || 0;
    
    if (velocity > this.MAX_VELOCITY) {
      this.logSuspiciousActivity(userId, sessionId, 'MAX_VELOCITY_EXCEEDED', { velocity });
      return false;
    }

    return true;
  }

  private validateDamage(userId: string, sessionId: string, data: any): boolean {
    const damage = data.damage || 0;
    const maxDamage = 200;

    if (damage > maxDamage) {
      this.logSuspiciousActivity(userId, sessionId, 'DAMAGE_EXCEEDED', { damage });
      return false;
    }

    return true;
  }

  private async validatePosition(userId: string, sessionId: string, data: PositionReport): Promise<boolean> {
    const redisClient = redis.getClient();
    const key = `position:${sessionId}:${userId}`;
    
    const lastPosition = await redisClient.get(key);
    
    if (lastPosition) {
      const last = JSON.parse(lastPosition);
      const distance = this.calculateDistance(last.position, data.position);
      
      const timeDelta = Math.abs(data.timestamp - last.timestamp) / 1000;
      const impliedVelocity = distance / Math.max(timeDelta, 0.001);
      
      if (impliedVelocity > this.MAX_VELOCITY) {
        await this.logSuspiciousActivity(userId, sessionId, 'TELEPORT_DETECTED', {
          distance,
          timeDelta,
          impliedVelocity,
          from: last.position,
          to: data.position,
        });
        
        await this.addBanVote(userId, sessionId, 'TELEPORT', 30);
        return false;
      }

      if (distance > this.MAX_TELEPORT_DISTANCE) {
        await this.logSuspiciousActivity(userId, sessionId, 'LARGE_TELEPORT', { distance });
      }
    }

    await redisClient.set(key, JSON.stringify(data), 'EX', 300);
    return true;
  }

  private calculateDistance(p1: { x: number; y: number; z: number }, p2: { x: number; y: number; z: number }): number {
    return Math.sqrt(
      Math.pow(p2.x - p1.x, 2) +
      Math.pow(p2.y - p1.y, 2) +
      Math.pow(p2.z - p1.z, 2)
    );
  }

  private async logSuspiciousActivity(userId: string, sessionId: string, type: string, data: any): Promise<void> {
    logger.warn(`Suspicious activity: ${type}`, { userId, sessionId, data });

    const redisClient = redis.getClient();
    const key = `suspicious:${userId}`;
    
    await redisClient.zincrby(key, 1, type);
    await redisClient.expire(key, 86400);

    const totalScore = await redisClient.zscore(key, type);
    if (totalScore && parseFloat(totalScore) > this.SUSPICIOUS_SCORE_THRESHOLD) {
      await this.flagUserForReview(userId, type, data);
    }
  }

  private async addBanVote(userId: string, sessionId: string, reason: string, weight: number): Promise<void> {
    const redisClient = redis.getClient();
    const key = `ban_votes:${userId}`;
    
    await redisClient.zincrby(key, weight, reason);
    await redisClient.expire(key, 86400 * 7);

    const totalVotes = await redisClient.zscore(key, reason);
    if (totalVotes && parseFloat(totalVotes) > 100) {
      await this.autobanUser(userId, reason);
    }
  }

  private async flagUserForReview(userId: string, reason: string, data: any): Promise<void> {
    logger.error(`User ${userId} flagged for review: ${reason}`, { data });
  }

  private async autobanUser(userId: string, reason: string): Promise<void> {
    logger.error(`Auto-banning user ${userId} for: ${reason}`);
  }

  async getPlayerMetrics(userId: string, sessionId: string): Promise<PlayerMetrics> {
    const redisClient = redis.getClient();
    
    const actionKey = `anticheat:${sessionId}:${userId}`;
    const positionKey = `position:${sessionId}:${userId}`;
    const banVotesKey = `ban_votes:${userId}`;

    const [actionCount, suspiciousScores, banVotes] = await Promise.all([
      redisClient.get(actionKey),
      redisClient.zrange(actionKey, 0, -1, 'WITHSCORES'),
      redisClient.zrange(banVotesKey, 0, -1, 'WITHSCORES'),
    ]);

    let totalRiskScore = 0;
    const patterns: string[] = [];

    for (let i = 0; i < suspiciousScores.length; i += 2) {
      const type = suspiciousScores[i];
      const score = parseFloat(suspiciousScores[i + 1]);
      
      if (score > 20) {
        patterns.push(type);
        totalRiskScore += score;
      }
    }

    return {
      userId,
      sessionId,
      actionsPerSecond: parseInt(actionCount || '0') / 60,
      totalActions: parseInt(actionCount || '0'),
      suspiciousPatterns: patterns,
      riskScore: totalRiskScore,
    };
  }

  async generateReport(userId: string): Promise<any> {
    const redisClient = redis.getClient();
    const banVotesKey = `ban_votes:${userId}`;
    const suspiciousKey = `suspicious:${userId}`;

    const [banVotes, suspiciousActivity] = await Promise.all([
      redisClient.zrange(banVotesKey, 0, -1, 'WITHSCORES'),
      redisClient.zrange(suspiciousKey, 0, -1, 'WITHSCORES'),
    ]);

    const report = {
      userId,
      generatedAt: new Date(),
      banVotes: {} as Record<string, number>,
      suspiciousActivity: {} as Record<string, number>,
      totalRiskScore: 0,
      shouldReview: false,
    };

    for (let i = 0; i < banVotes.length; i += 2) {
      report.banVotes[banVotes[i]] = parseFloat(banVotes[i + 1]);
      report.totalRiskScore += parseFloat(banVotes[i + 1]);
    }

    for (let i = 0; i < suspiciousActivity.length; i += 2) {
      report.suspiciousActivity[suspiciousActivity[i]] = parseFloat(suspiciousActivity[i + 1]);
    }

    report.shouldReview = report.totalRiskScore > this.SUSPICIOUS_SCORE_THRESHOLD;

    return report;
  }
}

export const antiCheatService = new AntiCheatService();
