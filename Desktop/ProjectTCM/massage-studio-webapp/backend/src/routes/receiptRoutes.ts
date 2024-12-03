import express from 'express';
import receiptController from '../controllers/receiptController';
import { authMiddleware } from '../middleware/authMiddleware';

const router = express.Router();

router.post('/', authMiddleware, receiptController.createReceipt.bind(receiptController));
router.get('/', authMiddleware, receiptController.getReceipts.bind(receiptController));
router.get('/:id', authMiddleware, receiptController.getReceipt.bind(receiptController));

export default router;