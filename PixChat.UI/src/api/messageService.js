import axios from 'axios';
import db from '../stores/db';

export const extractMessageFromImage = async (user, token, senderId, base64Image, chatId, messageId, isOneTime) => {
  try {
    const isGroupChat = chatId !== null && chatId !== undefined;
    const keySecondPart = isGroupChat ? chatId : user.email;
    const encryptedKey = await generateDynamicKey(senderId, keySecondPart);
    console.log('Generated encryptedKey:', encryptedKey, 'with senderId:', senderId, 'and second part:', keySecondPart);

    const responseEM = await axios.post(
      `http://localhost:5038/api/users/${user.id}/receiveMessage`,
      { base64Image, encryptedKey },
      { headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' } }
    );

    const { message, messageLength, timestamp: serverTimestamp, encryptedAesKey, aesIv } = responseEM.data;

    console.log('encryptedAESKey:', { encryptedAesKey });

      if (!encryptedAesKey || typeof encryptedAesKey !== 'string' || encryptedAesKey.trim() === '') {
        throw new Error('Invalid or missing encryptedAESKey');
      }
      if (!message || typeof message !== 'string') {
        throw new Error('Invalid encryptedFileData');
      }
      if (!aesIv || typeof aesIv !== 'string') {
        throw new Error('Invalid aesIV');
      }
  
      console.log('Decrypting file with:', { encryptedAesKey, message, aesIv });


      const response = await fetch(`http://localhost:5038/api/keys/${user.id}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!response.ok) throw new Error(`Ошибка получения ключей: ${response.status}`);
      const keyData = await response.json();
      const privateKey = keyData.privateKey;
  
      console.log('Private Key:', privateKey);
  
      const aesKeyResponse = await fetch('http://localhost:5038/api/keys/decrypt-aes-key', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          encryptedAESKey: encryptedAesKey,
          privateKey: privateKey,
        }),
      });
  
      if (!aesKeyResponse.ok) {
        const errorText = await aesKeyResponse.text();
        console.error('Server response:', errorText);
        throw new Error(`Ошибка дешифрования AES-ключа: ${aesKeyResponse.status} - ${errorText}`);
      }
      const aesKey = await aesKeyResponse.text();
  
      const decryptedFileResponse = await fetch('http://localhost:5038/api/keys/decrypt-message', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          encryptedData: message,
          key: aesKey,
          iv: aesIv,
        }),
      });
      if (!decryptedFileResponse.ok) throw new Error(`Ошибка дешифрования файла: ${decryptedFileResponse.status}`);
      const decryptedMessage = await decryptedFileResponse.text();

    console.log('Extracted from server:', { decryptedMessage, messageLength, serverTimestamp });



    const newMsg = {
      senderId,
      decryptedMessage,
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
        decryptedMessage,
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
        decryptedMessage,
        timestamp: newMsg.timestamp,
        isRead: false,
        isSent: false,
        messageId: newMsg.messageId,
      });
      console.log('Saved to messages:', newMsg);
    }

    return decryptedMessage;
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