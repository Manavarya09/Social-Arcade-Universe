import { Router } from 'express';
import friendsRoutes from './friends/friends.routes';
import chatRoutes from './chat/chat.routes';

const router = Router();

router.use('/friends', friendsRoutes);
router.use('/chat', chatRoutes);

export default router;
