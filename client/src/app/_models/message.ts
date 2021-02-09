export interface Message {
    id: number;
    senderId: number;
    senderUsername: string;
    senderPhotoUrl: string;
    recipientId: number;
    recipientUsername?: any;
    recipientPhotoUrl: string;
    content: string;
    dateRead?: Date;
    messageSent: string;
}