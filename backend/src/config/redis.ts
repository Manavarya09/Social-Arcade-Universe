import Redis from 'ioredis';
import { config } from '../config/env';
import { logger } from '../shared/utils/logger';

class RedisClient {
  private client: Redis | null = null;

  async connect(): Promise<Redis> {
    if (this.client) {
      return this.client;
    }

    this.client = new Redis(config.redis.url, {
      maxRetriesPerRequest: 3,
      retryDelayOnFailover: 100,
      enableReadyCheck: true,
      lazyConnect: true,
      connectionName: 'sau-backend',
    });

    this.client.on('connect', () => {
      logger.info('Redis client connected');
    });

    this.client.on('error', (err) => {
      logger.error('Redis client error:', err);
    });

    this.client.on('ready', () => {
      logger.info('Redis client ready');
    });

    await this.client.connect();
    return this.client;
  }

  getClient(): Redis {
    if (!this.client) {
      throw new Error('Redis client not initialized');
    }
    return this.client;
  }

  async disconnect(): Promise<void> {
    if (this.client) {
      await this.client.quit();
      this.client = null;
    }
  }
}

export const redis = new RedisClient();
