import Dexie from 'dexie';

const db = new Dexie('ChatDatabase');

db.version(5).stores({
  messages: '++id, userId, senderId, receiverId, message, timestamp, isRead, isSent, messageId',
  chatmessages: '++id, userId, senderId, chatId, message, timestamp, isRead, isSent, messageId',
  oneTimeMessages: '++id, messageId, chatId, senderId, timestamp, isRead',
  files: '++id, userId, senderId, receiverId, chatId, fileName, fileType, fileData, encryptedAESKey, aesIV, timestamp, isRead, isSent, messageId, isGroup' // Новая таблица для файлов
});

export default db;
