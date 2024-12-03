// receiptController.ts
import { Request, Response } from 'express';
import Receipt from '../models/Receipt';

class ReceiptController {
    async createReceipt(req: Request, res: Response): Promise<void> {
        try {
            const { clientName, services, totalAmount, paymentMethod } = req.body;
            const userId = req.user?.id;

            if (!userId) {
                res.status(401).json({ message: 'User not authenticated' });
                return;
            }

            const newReceipt = await Receipt.create({
                clientName,
                services,
                totalAmount,
                paymentMethod,
                createdBy: userId
            });

            res.status(201).json(newReceipt);
        } catch (error) {
            res.status(500).json({ message: 'Error creating receipt', error });
        }
    }

    async getReceipts(req: Request, res: Response): Promise<void> {
        try {
            const userId = req.user?.id;
            
            if (!userId) {
                res.status(401).json({ message: 'User not authenticated' });
                return;
            }

            const receipts = await Receipt.find({ createdBy: userId }).sort({ date: -1 });
            res.json(receipts);
        } catch (error) {
            res.status(500).json({ message: 'Error fetching receipts', error });
        }
    }

    async getReceipt(req: Request, res: Response): Promise<void> {
        try {
            const receipt = await Receipt.findById(req.params.id);
            if (!receipt) {
                res.status(404).json({ message: 'Receipt not found' });
                return;
            }
            res.json(receipt);
        } catch (error) {
            res.status(500).json({ message: 'Error fetching receipt', error });
        }
    }
}

export default new ReceiptController();