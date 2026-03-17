import { Request, Response, NextFunction } from 'express';

export function validateBody(fields: string[]) {
  return (req: Request, res: Response, next: NextFunction) => {
    const missing: string[] = [];
    
    for (const field of fields) {
      if (req.body[field] === undefined || req.body[field] === null || req.body[field] === '') {
        missing.push(field);
      }
    }

    if (missing.length > 0) {
      return res.status(400).json({
        success: false,
        error: {
          message: `Missing required fields: ${missing.join(', ')}`,
          fields: missing,
        },
      });
    }

    next();
  };
}

export function validateQuery(fields: string[]) {
  return (req: Request, res: Response, next: NextFunction) => {
    const missing: string[] = [];
    
    for (const field of fields) {
      if (req.query[field] === undefined) {
        missing.push(field);
      }
    }

    if (missing.length > 0) {
      return res.status(400).json({
        success: false,
        error: {
          message: `Missing required query parameters: ${missing.join(', ')}`,
          fields: missing,
        },
      });
    }

    next();
  };
}

export function validateParams(fields: string[]) {
  return (req: Request, res: Response, next: NextFunction) => {
    const missing: string[] = [];
    
    for (const field of fields) {
      if (req.params[field] === undefined) {
        missing.push(field);
      }
    }

    if (missing.length > 0) {
      return res.status(400).json({
        success: false,
        error: {
          message: `Missing required URL parameters: ${missing.join(', ')}`,
          fields: missing,
        },
      });
    }

    next();
  };
}
