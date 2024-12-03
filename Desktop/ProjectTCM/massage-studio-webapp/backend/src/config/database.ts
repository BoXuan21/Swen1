import mongoose from 'mongoose';

class Database {
  private static instance: Database;

  private constructor() {}

  public static getInstance(): Database {
    if (!Database.instance) {
      Database.instance = new Database();
    }
    return Database.instance;
  }

  public async connect() {
    try {
      await mongoose.connect(process.env.MONGODB_URI || '');
      console.log('MongoDB connected successfully');
    } catch (error) {
      console.error('MongoDB connection error:', error);
      process.exit(1);
    }
  }

  public async disconnect() {
    await mongoose.disconnect();
    console.log('MongoDB disconnected');
  }
}

export default Database;