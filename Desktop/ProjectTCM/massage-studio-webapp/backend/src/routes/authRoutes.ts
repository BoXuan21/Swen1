import express, { Request, Response } from 'express';
import authController from '../controllers/authController';

const router = express.Router();

router.post('/register', async (req: Request, res: Response) => {
    await authController.registerUser(req, res);
});

router.post('/login', async (req: Request, res: Response) => {
    await authController.loginUser(req, res);
});

export default router;