import mongoose from 'mongoose';

const ReceiptSchema = new mongoose.Schema({
  clientName: {
    type: String,
    required: true
  },
  date: {
    type: Date,
    default: Date.now
  },
  services: [{
    serviceName: String,
    price: Number
  }],
  totalAmount: {
    type: Number,
    required: true
  },
  paymentMethod: {
    type: String,
    required: true
  },
  createdBy: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'User',
    required: true
  }
});

export default mongoose.model('Receipt', ReceiptSchema);