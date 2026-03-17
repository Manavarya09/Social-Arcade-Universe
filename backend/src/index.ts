import 'dotenv/config';
import { app } from './app';
import { logger } from './shared/utils/logger';
import { PrismaClient } from '@prisma/client';
import { redis } from './config/redis';
import { socketService } from './websocket/socket.service';

const prisma = new PrismaClient();
const PORT = process.env.PORT || 3000;

async function bootstrap() {
  try {
    await prisma.$connect();
    logger.info('Database connected successfully');

    await redis.connect();
    logger.info('Redis connected successfully');

    const server = app.listen(PORT, () => {
      logger.info(`Server running on port ${PORT}`);
      logger.info(`Environment: ${process.env.NODE_ENV}`);
    });

    socketService.initialize(server);
    logger.info('Socket.io initialized');

    process.on('SIGTERM', async () => {
      logger.info('SIGTERM received, shutting down gracefully');
      await prisma.$disconnect();
      await redis.quit();
      server.close(() => {
        logger.info('Server closed');
        process.exit(0);
      });
    });
  } catch (error) {
    logger.error('Failed to start server:', error);
    process.exit(1);
  }
}

bootstrap();
