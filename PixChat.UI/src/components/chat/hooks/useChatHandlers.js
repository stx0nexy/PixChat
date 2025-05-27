import { useCallback } from 'react';
import { extractMessageFromImage } from '../../../api/messageService';
import db from '../../../stores/db';
import axios from 'axios';
import { Box, Typography } from '@mui/material';


export const useChatHandlers = (user, token, contacts, chats, receiverProfile, receiverChatId, setMessages, setFiles,
     setNewMessageState, setNewOneTimeMessageState, showSnackbar, fetchContacts, scrollToBottom, setFriendRequests) => {

  const decryptFile = useCallback(async (encryptedFileData, encryptedAESKey, aesIV) => {
    try {
      if (!encryptedAESKey || typeof encryptedAESKey !== 'string' || encryptedAESKey.trim() === '') {
        throw new Error('Invalid or missing encryptedAESKey');
      }
      if (!encryptedFileData || typeof encryptedFileData !== 'string') {
        throw new Error('Invalid encryptedFileData');
      }
      if (!aesIV || typeof aesIV !== 'string') {
        throw new Error('Invalid aesIV');
      }

      console.log('Decrypting file with:', { encryptedAESKey, encryptedFileData, aesIV });

      const response = await fetch(`http://localhost:5038/api/keys/${user.id}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!response.ok) throw new Error(`Error getting keys: ${response.status}`);
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
          encryptedAESKey: encryptedAESKey,
          privateKey: privateKey,
        }),
      });

      if (!aesKeyResponse.ok) {
        const errorText = await aesKeyResponse.text();
        console.error('Server response:', errorText);
        throw new Error(`Error decrypting AES key:: ${aesKeyResponse.status} - ${errorText}`);
      }
      const aesKey = await aesKeyResponse.text();

      const decryptedFileResponse = await fetch('http://localhost:5038/api/keys/decrypt-data', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          encryptedData: encryptedFileData,
          key: aesKey,
          iv: aesIV,
        }),
      });
      if (!decryptedFileResponse.ok) throw new Error(`Error decrypting file: ${decryptedFileResponse.status}`);
      const decryptedFile = await decryptedFileResponse.text();

      return decryptedFile;
    } catch (error) {
      console.error('Error decrypting file:', error);
      return encryptedFileData;
    }
  }, [user.id, token]);


  const handleNewMessage = useCallback(
    async (chatId, senderId, base64Image, encryptedKey, messageLength, timestamp) => {
      console.log('MESSAGE PNG:', base64Image);
      if (senderId !== user.email) {
        const response = await fetch(`http://localhost:5038/api/users/${senderId}/email`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        const data = await response.json();
        const senderUsername = data.username || senderId;
        console.log('User data:', data);

        let currentContacts = contacts;
        if (contacts.length === 0) {
          console.warn('Contacts list is empty. Fetching contacts...');
          currentContacts = await fetchContacts();
        }

        const isContact = currentContacts.some((contact) => {
          console.log('Checking contact:', contact);
          return contact.contactUserId === data.id;
        });

        if (!isContact) {
          console.warn(`Sender ${senderId} is not in contacts. Message will not be processed.`);
          return;
        }

        const decodedMessage = await extractMessageFromImage(
           user,
          token,
          senderId,
          base64Image,
          encryptedKey,
          messageLength,
          null,
          timestamp
        );
        const newMsg = {
          senderId,
          receiverId: receiverProfile?.email || user.email,
          decryptedMessage: decodedMessage,
          timestamp: timestamp || new Date().toISOString(),
        };
        console.log('New message received:', newMsg);
        setMessages((prev) => [...prev, newMsg]);
        setNewMessageState(newMsg);

        scrollToBottom();

        const messageTime = new Date(newMsg.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        const messageDate = new Date(newMsg.timestamp).toLocaleDateString();
        const snackbarContent = (
          <Box sx={{ display: 'flex', flexDirection: 'column', width: '100%' }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%' }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>
                {senderUsername}
              </Typography>
              <Typography variant="caption" sx={{ ml: 1 }}>
                {messageDate} {messageTime}
              </Typography>
            </Box>
            <Typography variant="body2">
              {newMsg.decryptedMessage?.substring(0, 50) || ''}
              {newMsg.decryptedMessage?.length > 50 ? '...' : ''}
            </Typography>
          </Box>
        );
        showSnackbar(snackbarContent, 'info');
      }
    },
    [user, token, contacts, receiverProfile, setMessages, setNewMessageState, scrollToBottom, showSnackbar, fetchContacts]
  );

  const handleNewOneTimeMessage = useCallback(async (messageId, chatId, senderId, timestamp) => {
    if (senderId !== user.email) {
      const fetchedContacts = await fetchContacts();
      console.log('Fetched contacts:', fetchedContacts);

      if (!fetchedContacts.length) {
        console.warn('Contacts list is empty. Cannot process message.');
        return;
      }

      const response = await fetch(`http://localhost:5038/api/users/${senderId}/email`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      const data = await response.json();
      console.log('User data:', data);

      const isContact = fetchedContacts.some(contact => contact.contactUserId === data.id);
      if (!isContact) {
        console.warn(`Sender ${senderId} is not in contacts. Message will not be processed.`);
        return;
      }

      const existingMessage = await db.oneTimeMessages.where('messageId').equals(messageId).first();
      if (!existingMessage) {
        await db.oneTimeMessages.add({
          messageId,
          chatId,
          senderId,
          timestamp,
          isRead: false,
        });
        console.log('One-time message info saved:', { messageId, chatId, senderId, timestamp });
        const newMsg = {
          messageId,
          chatId,
          senderId,
          timestamp,
          isRead: false,
        };
        setNewOneTimeMessageState(newMsg);
      }
    } else if (senderId === user.email) {
      const existingMessage = await db.oneTimeMessages.where('messageId').equals(messageId).first();
      if (!existingMessage) {
        await db.oneTimeMessages.add({
          messageId,
          chatId,
          senderId,
          timestamp,
          isRead: false,
        });
        console.log('One-time message info saved:', { messageId, chatId, senderId, timestamp });
        const newMsg = {
          messageId,
          chatId,
          senderId,
          timestamp,
          isRead: false,
        };
        setNewOneTimeMessageState(newMsg);
      }
    }
  }, [user, token, contacts, setNewOneTimeMessageState, fetchContacts]);

  const handleNewOneTimePendingMessage = useCallback(async (senderId, chatId, messageId, timestamp) => {
    console.log('NewOneTimePendingMessage data:', senderId, chatId, messageId, timestamp);

    try {
      await axios.post(
        `http://localhost:5038/api/users/confirmOneTimeMessageReceived`,
        { messageId },
        { headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' } }
      );
      console.log('confirmOneTimeMessageReceived:', messageId);
    } catch (error) {
      console.error('Message acknowledgement error:', error);
    }
    if (senderId !== user.email) {
      fetchContacts();
      const response = await fetch(`http://localhost:5038/api/users/${senderId}/email`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      const data = await response.json();
      console.log('User data:', data);
      const isContact = contacts.some(contact => contact.contactUserId === data.id);
      if (!isContact) {
        console.warn(`Sender ${senderId} is not in contacts. Pending message will not be processed.`);
        return;
      }
      const existingMessage = await db.oneTimeMessages.get({ messageId });
      if (!existingMessage) {
        await db.oneTimeMessages.add({
          messageId,
          chatId,
          senderId,
          timestamp: timestamp,
          isRead: false,
        });
        const newMsg = {
          messageId,
          chatId,
          senderId,
          timestamp,
          isRead: false,
        };
        setNewOneTimeMessageState(newMsg);
        console.log('One-time pending message info saved:', { messageId, chatId, senderId, timestamp });
      }
    }
  }, [user, token, contacts, fetchContacts, setNewOneTimeMessageState]);

  const handleNewFullOneTimePendingMessage = useCallback(async (senderId, chatId, base64Image, messageLength, messageId, timestamp) => {
    if (senderId !== user.email) {
      const fetchedContacts = await fetchContacts();
      console.log('Fetched contacts:', fetchedContacts);

      if (!fetchedContacts.length) {
        console.warn('Contacts list is empty. Cannot process message.');
        return;
      }
      const response = await fetch(`http://localhost:5038/api/users/${senderId}/email`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      const data = await response.json();
      console.log('User data:', data);
      const isContact = fetchedContacts.some(contact => contact.contactUserId === data.id);
      if (!isContact) {
        console.warn(`Sender ${senderId} is not in contacts. Full pending message will not be processed.`);
        return;
      }
      const decodedMessage = await extractMessageFromImage(
       user,
        token,
        senderId,
        base64Image,
        null,
        timestamp,
        true
      );

      const extractedMessage = decodedMessage;

      const newMsg = {
        senderId,
        message: extractedMessage,
        timestamp: timestamp || new Date().toISOString(),
        messageId: messageId || null,
        chatId,
      };

      // setOneTimeMessage(newMsg);
      console.log('Full one-time pending message info saved:', { extractedMessage, messageId, chatId, senderId, timestamp });
    } else if (senderId === user.email) {
      const decodedMessage = await extractMessageFromImage(
         user,
        token,
        senderId,
        base64Image,
        null,
        timestamp,
        true
      );

      const extractedMessage = decodedMessage;
      const newMsg = {
        senderId,
        message: extractedMessage,
        timestamp: timestamp || new Date().toISOString(),
        messageId: messageId || null,
        chatId,
      };

      // setOneTimeMessage(newMsg);
      console.log('Full one-time pending message info saved:', { extractedMessage, messageId, chatId, senderId, timestamp });
    }
  }, [user, token, contacts, fetchContacts]);

  const handleNewGroupMessage = useCallback(async (chatId, senderId, base64Image, timestamp) => {
    if (senderId !== user.email) {
      const decodedMessage = await extractMessageFromImage(
        user,
        token,
        senderId,
        base64Image,
        chatId,
      );
      const newMsg = {
        senderId,
        decryptedMessage: decodedMessage,
        timestamp: timestamp || new Date().toISOString(),
        chatId: chatId || null,
      };
      console.log('New group message received:', newMsg);
      setMessages(prev => [...prev, newMsg]);
      setNewMessageState(newMsg);

      scrollToBottom();

      const chatName = (await axios.get(`http://localhost:5038/api/chat/${chatId}`, { headers: { Authorization: `Bearer ${token}` } })).data.name;
      const senderUsername = (await axios.get(`http://localhost:5038/api/users/${senderId}/email`, { headers: { Authorization: `Bearer ${token}` } })).data.username;
      const messageTime = new Date(newMsg.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
      const messageDate = new Date(newMsg.timestamp).toLocaleDateString();

      const snackbarContent = (
        <Box sx={{ display: 'flex', flexDirection: 'column', width: '100%' }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%' }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>{senderUsername} in {chatName}</Typography>
            <Typography variant="caption" sx={{ ml: 1 }}>{messageDate} {messageTime}</Typography>
          </Box>
          <Typography variant="body2">{newMsg.decryptedMessage.substring(0, 50)}{newMsg.decryptedMessage.length > 50 ? '...' : ''}</Typography>
        </Box>
      );
      showSnackbar(snackbarContent, 'info');
    }
  }, [user, token, setMessages, setNewMessageState, scrollToBottom, showSnackbar]);


  const handlePendingMessage = async (senderId, chatId, base64Image, encryptedKey, messageLength, messageId, timestamp, isGroup) => {
    await axios.post(
      `http://localhost:5038/api/users/confirmMessageReceived`,
      { messageId },
      { headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' } }
    );
    console.log('confirmMessageReceived:', messageId);

    console.log('messageId before saving:', messageId, typeof messageId);

    if (senderId !== user.email) {
      if (messageId) {
        const messageExists = await db.messages.where('messageId').equals(messageId).count();
        const messageChatExists = await db.chatmessages.where('messageId').equals(messageId).count();
        if (messageExists > 0) {
          console.log('Message already received:', messageId);
          return;
        } else if (messageChatExists > 0) {
          console.log('Message already received:', messageId);
          return;
        }
      }

      if (isGroup) {
        const decodedMessage = await extractMessageFromImage(
          user,
          token,
          senderId,
          base64Image,
          encryptedKey,
          messageLength,
          messageId,
          timestamp,
          chatId
        );
        // setDecodedRarMessage(decodedMessage);
      } else {
        const decodedMessage = await extractMessageFromImage(
          user,
          token,
          senderId,
          base64Image,
          null,
          messageId,
        );
        // setDecodedRarMessage(decodedMessage);
      }
      const newMsg = {
        senderId,
        decryptedMessage: extractMessageFromImage,
        timestamp: timestamp || new Date().toISOString(),
        messageId
      };
      console.log('New pending message: ', newMsg.decryptedMessage);
      console.log('New message received:', newMsg);
      setMessages(prev => [...prev, newMsg]);
      setNewMessageState(newMsg);

      scrollToBottom();

      const senderUsername = (await axios.get(`http://localhost:5038/api/users/${senderId}/email`, { headers: { Authorization: `Bearer ${token}` } })).data.username;
      const chatName = isGroup ? (chats.find(c => c.id === chatId)?.name || 'Unknown Group') : senderUsername;
      const messageTime = new Date(newMsg.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
      const messageDate = new Date(newMsg.timestamp).toLocaleDateString();

      const snackbarContent = (
        <Box sx={{ display: 'flex', flexDirection: 'column', width: '100%' }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%' }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>{senderUsername} {isGroup ? `in ${chatName}` : ''}</Typography>
            <Typography variant="caption" sx={{ ml: 1 }}>{messageDate} {messageTime}</Typography>
          </Box>
          <Typography variant="body2">{newMsg.decryptedMessage?.substring(0, 50) || ''}{newMsg.decryptedMessage?.length > 50 ? '...' : ''}</Typography>
        </Box>
      );
      showSnackbar(snackbarContent, 'info');
    }
  };

  const handleFileReceived = useCallback(async (chatId, senderId, fileName, fileType, fileData, encryptedAESKey, aesIV, timestamp) => {
    console.log('File received:', { chatId, senderId, fileName, fileType, fileData, encryptedAESKey, aesIV, timestamp });
    if (senderId !== user.email) {
      const response = await fetch(`http://localhost:5038/api/users/${senderId}/email`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      const data = await response.json();
      const senderUsername = data.username || senderId;

      let currentContacts = contacts;
      if (contacts.length === 0) {
        currentContacts = await fetchContacts();
      }

      const isContact = currentContacts.some((contact) => contact.contactUserId === data.id);
      if (!isContact && !chatId) {
        console.warn(`Sender ${senderId} is not in contacts. File will not be processed.`);
        return;
      }

      const existingFile = await db.files
        .where({ senderId, fileName, timestamp })
        .first();
      if (existingFile) {
        console.log('File already exists:', fileName);
        return;
      }

      const decryptedFileData = await decryptFile(fileData, encryptedAESKey, aesIV);

      const newFile = {
        senderId,
        receiverId: receiverProfile?.email || user.email,
        chatId: chatId || null,
        fileName,
        fileType,
        fileData: decryptedFileData,
        encryptedAESKey,
        aesIV,
        timestamp: timestamp || new Date().toISOString(),
      };

      if (chatId) {
        await db.files.add({
          userId: user.id,
          senderId,
          chatId: chatId,
          fileName,
          fileType,
          fileData: decryptedFileData,
          encryptedAESKey,
          aesIV,
          timestamp: newFile.timestamp,
          isRead: true,
          isSent: true,
          isGroup: true,
        });
      } else {
        await db.files.add({
          userId: user.id,
          senderId,
          receiverId: receiverProfile?.email || user.email,
          fileName,
          fileType,
          fileData: decryptedFileData,
          encryptedAESKey,
          aesIV,
          timestamp: newFile.timestamp,
          isRead: true,
          isSent: true,
          isGroup: false,
        });
      }

      setFiles((prev) => [...prev, newFile]);
      setNewMessageState(fileName);

      scrollToBottom();

      const chatDisplayName = chatId ? (chats.find(c => c.id === chatId)?.name || 'Unknown Group') : senderUsername;
      const messageTime = new Date(newFile.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
      const messageDate = new Date(newFile.timestamp).toLocaleDateString();

      const snackbarContent = (
        <Box sx={{ display: 'flex', flexDirection: 'column', width: '100%' }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%' }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>{senderUsername} {chatId ? `in ${chatDisplayName}` : ''}</Typography>
            <Typography variant="caption" sx={{ ml: 1 }}>{messageDate} {messageTime}</Typography>
          </Box>
          <Typography variant="body2">New file: "{fileName}"</Typography>
        </Box>
      );
      showSnackbar(snackbarContent, 'info');
    }
  }, [user, token, contacts, fetchContacts, receiverProfile, setFiles, setNewMessageState, scrollToBottom, showSnackbar, decryptFile, chats]);

  const handlePendingFileReceived = useCallback(async (senderId, chatId, fileName, fileType, fileData, encryptedAESKey, aesIV, messageId, timestamp, isGroup) => {
    await axios.post(
      `http://localhost:5038/api/users/confirmMessageReceived`,
      { messageId },
      { headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' } }
    );
    console.log('confirmMessageReceived:', messageId);

    if (senderId !== user.email) {
      if (messageId) {
        const fileExists = await db.files.where('messageId').equals(messageId).count();
        if (fileExists > 0) {
          console.log('File already received:', messageId);
          return;
        }
      }

      const decryptedFileData = await decryptFile(fileData, encryptedAESKey, aesIV);

      const newFile = {
        senderId,
        receiverId: receiverProfile?.email || user.email,
        chatId: chatId || null,
        fileName,
        fileType,
        fileData: decryptedFileData,
        encryptedAESKey,
        aesIV,
        timestamp: timestamp || new Date().toISOString(),
        messageId,
        isGroup,
      };

      if (isGroup) {
        await db.files.add({
          userId: user.id,
          senderId,
          chatId: chatId,
          fileName,
          fileType,
          fileData: decryptedFileData,
          encryptedAESKey,
          aesIV,
          timestamp: newFile.timestamp,
          isRead: true,
          isSent: true,
          messageId,
          isGroup: true,
        });
      } else {
        await db.files.add({
          userId: user.id,
          senderId,
          receiverId: receiverProfile?.email || user.email,
          fileName,
          fileType,
          fileData: decryptedFileData,
          encryptedAESKey,
          aesIV,
          timestamp: newFile.timestamp,
          isRead: true,
          isSent: true,
          messageId,
          isGroup: false,
        });
      }

      setFiles((prev) => [...prev, newFile]);
      setNewMessageState(fileName);

      scrollToBottom();

      const senderUsername = (await axios.get(`http://localhost:5038/api/users/${senderId}/email`, { headers: { Authorization: `Bearer ${token}` } })).data.username;
      const chatDisplayName = isGroup ? (chats.find(c => c.id === chatId)?.name || 'Unknown Group') : senderUsername;
      const messageTime = new Date(newFile.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
      const messageDate = new Date(newFile.timestamp).toLocaleDateString();

      const snackbarContent = (
        <Box sx={{ display: 'flex', flexDirection: 'column', width: '100%' }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%' }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>{senderUsername} {isGroup ? `in ${chatDisplayName}` : ''}</Typography>
            <Typography variant="caption" sx={{ ml: 1 }}>{messageDate} {messageTime}</Typography>
          </Box>
          <Typography variant="body2">New pending file: "{fileName}"</Typography>
        </Box>
      );
      showSnackbar(snackbarContent, 'info');
    }
  }, [user, token, receiverProfile, setFiles, setNewMessageState, scrollToBottom, showSnackbar, decryptFile, chats]);

  const handleFriendRequestReceived = useCallback((requesterId) => {
    // Logic for handling new friend requests
  }, []);

  const handleNewFriendRequest = useCallback((requesterId) => {
      setFriendRequests(prev => {
        if (!prev.includes(requesterId)) {
          return [...prev, requesterId];
        }
        return prev;
      });
    }, []);

  const handleFriendRequestConfirmed = useCallback((userId) => {
      alert(`User ${userId} has confirmed your friend request`);
      fetchContacts();
    }, [fetchContacts]);

  const handleFriendRequestRejected = useCallback((userId) => {
      alert(`User ${userId} has rejected your friend request.`);
    }, []);

  return {
    handleNewMessage,
    handleNewGroupMessage,
    handlePendingMessage,
    handleNewFriendRequest,
    handleFriendRequestConfirmed,
    handleFriendRequestRejected,
    handleNewOneTimeMessage,
    handleNewOneTimePendingMessage,
    handleNewFullOneTimePendingMessage,
    handleFileReceived,
    handlePendingFileReceived,
    decryptFile
  };
};