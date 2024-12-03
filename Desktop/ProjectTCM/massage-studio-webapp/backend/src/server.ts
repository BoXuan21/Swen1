import express from 'express';
import cors from 'cors';
import dotenv from 'dotenv';
import Database from './config/database';
import authRoutes from './routes/authRoutes';

dotenv.config();

const app = express();

// Middleware
app.use(cors());
app.use(express.json());

// Routes
app.use('/api/auth', authRoutes);

// Health check route
app.get('/', (_req, res) => {
  res.json({ message: 'Backend is running!' });
});

// Database Connection
async function startServer() {
  try {
    const database = Database.getInstance();
    await database.connect();

    const PORT = process.env.PORT || 3000;

    app.listen(PORT, () => {
      console.log(`Server running on port ${PORT}`);
    });
  } catch (error) {
    console.error('Failed to start server:', error);
    process.exit(1);
  }
}

startServer();