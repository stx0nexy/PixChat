import axios from 'axios';
import db from '../stores/db';

export const extractMessageFromImage = async (user, token, senderId, base64Image, chatId, messageId, isOneTime) => {
  try {
    const isGroupChat = chatId !== null && chatId !== undefined;
    const keySecondPart = isGroupChat ? chatId : user.email;
    const encryptedKey = await generateDynamicKey(senderId, keySecondPart);
    console.log('Generated encryptedKey:', encryptedKey, 'with senderId:', senderId, 'and second part:', keySecondPart);

    const response = await axios.post(
      `http://localhost:5038/api/users/${user.id}/receiveMessage`,
      { base64Image, encryptedKey },
      { headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' } }
    );

    const { message, messageLength, timestamp: serverTimestamp } = response.data;
    console.log('Extracted from server:', { message, messageLength, serverTimestamp });

    const newMsg = {
      senderId,
      message,
      timestamp: serverTimestamp || new Date().toISOString(),
      messageId: messageId || null,
      chatId,
    };

    if (newMsg.messageId) {
      const messageExists = await db.messages.where('messageId').equals(newMsg.messageId).count();
      const chatMessageExists = chatId ? await db.chatmessages.where('messageId').equals(newMsg.messageId).count() : 0;
      if (messageExists > 0 || chatMessageExists > 0) {
        console.log('Message with this ID already exists:', newMsg.messageId);
        return message;
      }
    }

    if (chatId) {
      await db.chatmessages.add({
        userId: user.id,
        senderId,
        chatId,
        message,
        timestamp: newMsg.timestamp,
        isRead: false,
        isSent: false,
        messageId: newMsg.messageId,
      });
      console.log('Saved to chatmessages:', newMsg);
    }else if(isOneTime){
      console.log('Not Saved to chatmessages:', newMsg);
    } else {
      await db.messages.add({
        userId: user.id,
        senderId,
        receiverId: user.email,
        message,
        timestamp: newMsg.timestamp,
        isRead: false,
        isSent: false,
        messageId: newMsg.messageId,
      });
      console.log('Saved to messages:', newMsg);
    }

    return message;
  } catch (err) {
    console.error('Error extracting message:', err);
    if (err.response) {
      console.error('Server response:', err.response.data);
    }
    return 'Failed to extract message';
  }
};

async function generateDynamicKey(senderId, receiverIdOrChatId) {
  const input = `${senderId}${receiverIdOrChatId}`;
  console.log('Generating key with input:', input);
  const encoder = new TextEncoder();
  const data = encoder.encode(input);
  const hashBuffer = await crypto.subtle.digest('SHA-256', data);
  const hashArray = new Uint8Array(hashBuffer);
  return btoa(String.fromCharCode(...hashArray));
}