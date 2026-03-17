import { Request, Response, NextFunction } from 'express';
import { logger } from '../utils/logger';

export class AppError extends Error {
  public statusCode: number;
  public isOperational: boolean;
  public errorCode?: string;

  constructor(message: string, statusCode: number, errorCode?: string) {
    super(message);
    this.statusCode = statusCode;
    this.isOperational = true;
    this.errorCode = errorCode;
    Error.captureStackTrace(this, this.constructor);
  }
}

export const errorMiddleware = (
  err: Error | AppError,
  _req: Request,
  res: Response,
  _next: NextFunction
) => {
  if (err instanceof AppError) {
    logger.warn(`Operational error: ${err.message}`, {
      statusCode: err.statusCode,
      errorCode: err.errorCode,
    });

    return res.status(err.statusCode).json({
      success: false,
      error: {
        message: err.message,
        errorCode: err.errorCode,
      },
    });
  }

  logger.error('Unhandled error:', {
    message: err.message,
    stack: err.stack,
  });

  return res.status(500).json({
    success: false,
    error: {
      message: process.env.NODE_ENV === 'production' 
        ? 'Internal server error' 
        : err.message,
    },
  });
};
