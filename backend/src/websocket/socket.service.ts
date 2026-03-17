import { Server, Socket } from 'socket.io';
import jwt from 'jsonwebtoken';
import { config } from '../config/env';
import { logger } from '../shared/utils/logger';
import { MatchmakingService } from '../modules/matchmaking/matchmaking.service';
import { GameService } from '../modules/game/game.service';

interface AuthenticatedSocket extends Socket {
  userId?: string;
  userData?: any;
}

export class SocketService {
  private io: Server | null = null;
  private matchmakingService: MatchmakingService;
  private gameService: GameService;
  private userSockets: Map<string, Set<string>> = new Map();
  private socketUsers: Map<string, string> = new Map();

  constructor() {
    this.matchmakingService = new MatchmakingService();
    this.gameService = new GameService();
  }

  initialize(server: any) {
    this.io = new Server(server, {
      cors: {
        origin: config.clientUrl,
        methods: ['GET', 'POST'],
        credentials: true,
      },
      pingTimeout: 60000,
      pingInterval: 25000,
    });

    this.io.use(this.authenticateSocket.bind(this));

    this.io.on('connection', this.handleConnection.bind(this));

    logger.info('Socket.io service initialized');
  }

  private async authenticateSocket(socket: AuthenticatedSocket, next: (err?: Error) => void) {
    try {
      const token = socket.handshake.auth.token || socket.handshake.headers.authorization?.split(' ')[1];

      if (!token) {
        return next(new Error('Authentication required'));
      }

      const decoded = jwt.verify(token, config.jwt.secret) as { userId: string; email: string };
      socket.userId = decoded.userId;

      if (!this.userSockets.has(decoded.userId)) {
        this.userSockets.set(decoded.userId, new Set());
      }
      this.userSockets.get(decoded.userId)!.add(socket.id);
      this.socketUsers.set(socket.id, decoded.userId);

      next();
    } catch (error) {
      logger.error('Socket authentication failed:', error);
      next(new Error('Invalid token'));
    }
  }

  private handleConnection(socket: AuthenticatedSocket) {
    logger.info(`Client connected: ${socket.id}, User: ${socket.userId}`);

    socket.on('join_lobby', (data: { roomId: string }) => this.handleJoinLobby(socket, data));
    socket.on('leave_lobby', (data: { roomId: string }) => this.handleLeaveLobby(socket, data));
    socket.on('player_ready', (data: { ready: boolean; roomId: string }) => this.handlePlayerReady(socket, data));
    socket.on('game_start', (data: { roomId: string }) => this.handleGameStart(socket, data));
    socket.on('player_input', (data: any) => this.handlePlayerInput(socket, data));
    socket.on('player_action', (data: any) => this.handlePlayerAction(socket, data));
    socket.on('position_update', (data: any) => this.handlePositionUpdate(socket, data));
    socket.on('chat_message', (data: { roomId: string; message: string }) => this.handleChatMessage(socket, data));
    socket.on('join_matchmaking', (data: { gameMode: string; rating: number }) => this.handleJoinMatchmaking(socket, data));
    socket.on('leave_matchmaking', () => this.handleLeaveMatchmaking(socket));

    socket.on('disconnect', () => this.handleDisconnect(socket));
  }

  private async handleJoinLobby(socket: AuthenticatedSocket, data: { roomId: string }) {
    socket.join(data.roomId);
    logger.info(`User ${socket.userId} joined lobby ${data.roomId}`);
    
    this.io?.to(data.roomId).emit('lobby_updated', {
      event: 'player_joined',
      userId: socket.userId,
      socketId: socket.id,
    });
  }

  private async handleLeaveLobby(socket: AuthenticatedSocket, data: { roomId: string }) {
    socket.leave(data.roomId);
    logger.info(`User ${socket.userId} left lobby ${data.roomId}`);
    
    this.io?.to(data.roomId).emit('lobby_updated', {
      event: 'player_left',
      userId: socket.userId,
    });
  }

  private async handlePlayerReady(socket: AuthenticatedSocket, data: { ready: boolean; roomId: string }) {
    this.io?.to(data.roomId).emit('player_ready', {
      userId: socket.userId,
      ready: data.ready,
    });
  }

  private async handleGameStart(socket: AuthenticatedSocket, data: { roomId: string }) {
    this.io?.to(data.roomId).emit('game_started', {
      timestamp: Date.now(),
    });
  }

  private async handlePlayerInput(socket: AuthenticatedSocket, data: any) {
    if (socket.userId) {
      await this.gameService.processPlayerInput(socket.userId, data);
    }
  }

  private async handlePlayerAction(socket: AuthenticatedSocket, data: any) {
    if (socket.userId) {
      await this.gameService.processPlayerAction(socket.userId, data);
    }
  }

  private async handlePositionUpdate(socket: AuthenticatedSocket, data: any) {
    if (socket.userId && data.roomId) {
      this.io?.to(data.roomId).emit('position_sync', {
        odId: socket.userId,
        position: data.position,
        rotation: data.rotation,
        timestamp: Date.now(),
      });
    }
  }

  private async handleChatMessage(socket: AuthenticatedSocket, data: { roomId: string; message: string }) {
    if (socket.userId) {
      this.io?.to(data.roomId).emit('chat_message', {
        userId: socket.userId,
        message: data.message,
        timestamp: Date.now(),
      });
    }
  }

  private async handleJoinMatchmaking(socket: AuthenticatedSocket, data: { gameMode: string; rating: number }) {
    if (socket.userId) {
      try {
        const match = await this.matchmakingService.addToQueue(socket.userId, data.gameMode, data.rating);
        
        if (match) {
          this.io?.to(socket.id).emit('matchmaking_found', match);
          
          for (const playerId of match.playerIds) {
            const sockets = this.userSockets.get(playerId);
            if (sockets) {
              sockets.forEach(s => {
                this.io?.to(s).emit('match_found', match);
              });
            }
          }
        } else {
          this.io?.to(socket.id).emit('matchmaking_queued', {
            gameMode: data.gameMode,
            position: await this.matchmakingService.getQueuePosition(socket.userId!, data.gameMode),
          });
        }
      } catch (error) {
        logger.error('Matchmaking error:', error);
        socket.emit('matchmaking_error', { message: 'Failed to join matchmaking' });
      }
    }
  }

  private async handleLeaveMatchmaking(socket: AuthenticatedSocket) {
    if (socket.userId) {
      await this.matchmakingService.removeFromQueue(socket.userId);
      socket.emit('matchmaking_left');
    }
  }

  private handleDisconnect(socket: AuthenticatedSocket) {
    logger.info(`Client disconnected: ${socket.id}, User: ${socket.userId}`);

    if (socket.userId) {
      const sockets = this.userSockets.get(socket.userId);
      if (sockets) {
        sockets.delete(socket.id);
        if (sockets.size === 0) {
          this.userSockets.delete(socket.userId);
        }
      }
      this.socketUsers.delete(socket.id);
    }
  }

  emitToUser(userId: string, event: string, data: any) {
    const sockets = this.userSockets.get(userId);
    if (sockets) {
      sockets.forEach(socketId => {
        this.io?.to(socketId).emit(event, data);
      });
    }
  }

  emitToRoom(roomId: string, event: string, data: any) {
    this.io?.to(roomId).emit(event, data);
  }

  getIO(): Server | null {
    return this.io;
  }
}

export const socketService = new SocketService();
