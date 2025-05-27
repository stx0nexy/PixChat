import React, { useState, useEffect, useRef, useCallback } from 'react';
import { Box, Snackbar, Alert } from '@mui/material';
import { useSignalR } from '../../stores/useSignalR';
import db from '../../stores/db';
import axios from 'axios';
import { ChatContainer } from '../../components/chat/ChatContainer';
import { useChatHandlers } from '../../components/chat/hooks/useChatHandlers';

const Chat = ({ user, token, onLogout }) => {
  const messagesEndRef = useRef(null);

  const [message, setMessage] = useState('');
  const [isOneTimeMessage, setIsOneTimeMessage] = useState(false);

  const [receiverEmail, setReceiverEmail] = useState(null);
  const [receiverChatId, setReceiverChatId] = useState(null);
  const [notesSelect, setNotesSelect] = useState(false);

  const [messages, setMessages] = useState([]);
  const [files, setFiles] = useState([]);
  const [oneTimeMessage, setOneTimeMessage] = useState([]);
  const [contacts, setContacts] = useState([]);
  const [chats, setChats] = useState([]);
  const [friendRequests, setFriendRequests] = useState([]);
  const [contactStatus, setContactStatus] = useState({});

  const [selectedProfile, setSelectedProfile] = useState(null);
  const [isBlockedByUser, setIsBlockedByUser] = useState(false);
  const [isBlockedByContact, setIsBlockedByContact] = useState(false);
  const [blockMessage, setBlockMessage] = useState('');
  const [snackbarOpen, setSnackbarOpen] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState('');
  const [snackbarSeverity, setSnackbarSeverity] = useState('info');

  const [newMessageState, setNewMessageState] = useState(null);
  const [newOneTimeMessageState, setNewOneTimeMessageState] = useState(null);

  const [receiverProfile, setReceiverProfile] = useState(null);
  const [receiverChat, setReceiverChat] = useState(null);
  const [receiverNotes, setReceiverNotes] = useState(null);

  const scrollToBottom = useCallback(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, []);

  const showSnackbar = useCallback((message, severity = 'info') => {
    setSnackbarMessage(message);
    setSnackbarSeverity(severity);
    setSnackbarOpen(true);
  }, []);

  const handleSnackbarClose = useCallback((event, reason) => {
    if (reason === 'clickaway') {
      return;
    }
    setSnackbarOpen(false);
  }, []);

  const fetchContacts = useCallback(async () => {
    if (!user?.id || !token) return [];

    try {
      const response = await fetch(`http://localhost:5038/api/contacts/${user.id}/contacts`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (response.status === 401) {
        console.error('Unauthorized: Token is invalid or expired');
        onLogout();
        return [];
      }
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      const data = await response.json();
      setContacts(data);
      return data;
    } catch (error) {
      console.error('Error fetching contacts list:', error);
      return [];
    }
  }, [user?.id, token, onLogout]);

  const fetchChats = useCallback(async () => {
    if (!user?.id || !token) return;

    try {
      const response = await fetch(`http://localhost:5038/api/chat/user/${user.id}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (response.status === 401) {
        console.error('Unauthorized: Token is invalid or expired');
        onLogout();
        return;
      }
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      const data = await response.json();
      const groupChats = data.filter((chat) => chat.isGroup);
      setChats(groupChats);
    } catch (error) {
      console.error('Error fetching chats list:', error);
    }
  }, [user?.id, token, onLogout]);

  const handleUserOnline = useCallback((userId) => {
    setContactStatus((prev) => ({
      ...prev,
      [userId]: true,
    }));
  }, []);

  const handleUserOffline = useCallback((userId) => {
    setContactStatus((prev) => ({
      ...prev,
      [userId]: false,
    }));
  }, []);

  const handleReceiveOnlineContacts = useCallback((onlineContactEmails) => {
    const newStatus = {};
    onlineContactEmails.forEach((email) => {
      newStatus[email] = true;
    });
    setContactStatus((prev) => ({
      ...prev,
      ...newStatus,
    }));
  }, []);

  const {
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
  } = useChatHandlers(
    user,
    token,
    contacts,
    chats,
    receiverProfile,
    receiverChatId,
    setMessages,
    setFiles,
    setNewMessageState,
    setNewOneTimeMessageState,
    showSnackbar,
    fetchContacts,
    scrollToBottom
  );

  const { connection } = useSignalR(
    token,
    user,
    handleNewMessage,
    handleNewGroupMessage,
    handlePendingMessage,
    handleNewFriendRequest,
    handleFriendRequestConfirmed,
    handleFriendRequestRejected,
    handleNewOneTimeMessage,
    handleNewOneTimePendingMessage,
    handleNewFullOneTimePendingMessage,
    handleUserOnline,
    handleUserOffline,
    handleReceiveOnlineContacts,
    handleFileReceived,
    handlePendingFileReceived
  );

  useEffect(() => {
    const loadInitialData = async () => {
      await fetchContacts();
      await fetchChats();
    };
    loadInitialData();
  }, [fetchContacts, fetchChats]);

  useEffect(() => {
    const fetchMessagesAndFiles = async () => {
      let fetchedMessages = [];
      let fetchedFiles = [];

      if (receiverEmail) {
        fetchedMessages = await db.messages
          .where('userId').equals(user.id)
          .and(msg => (msg.senderId === receiverEmail && msg.receiverId === user.email) || (msg.senderId === user.email && msg.receiverId === receiverEmail))
          .toArray();
        fetchedFiles = await db.files
          .where('userId').equals(user.id)
          .and(file => (file.senderId === receiverEmail && file.receiverId === user.email) || (file.senderId === user.email && file.receiverId === receiverEmail))
          .toArray();
      } else if (receiverChatId) {
        fetchedMessages = await db.chatmessages
          .where('chatId').equals(receiverChatId)
          .toArray();
        fetchedFiles = await db.files
          .where('chatId').equals(receiverChatId)
          .toArray();
      } else if (notesSelect) {
        fetchedMessages = await db.messages
          .where('userId').equals(user.id)
          .and(msg => msg.senderId === user.email && msg.receiverId === user.email)
          .toArray();
        fetchedFiles = await db.files
          .where('userId').equals(user.id)
          .and(file => file.senderId === user.email && file.receiverId === user.email)
          .toArray();
      }

      const decryptedFiles = await Promise.all(
        fetchedFiles.map(async (file) => {
          const decryptedFileData = await decryptFile(file.fileData, file.encryptedAESKey, file.aesIV);
          return { ...file, fileData: decryptedFileData };
        })
      );

      const sortedMsgs = fetchedMessages.sort((a, b) => {
        const dateA = new Date(a.timestamp);
        const dateB = new Date(b.timestamp);
        return dateA - dateB;
      });

      setMessages(sortedMsgs);
      setFiles(decryptedFiles);
    };

    fetchMessagesAndFiles();
  }, [receiverEmail, receiverChatId, notesSelect, user.id, user.email, newMessageState, decryptFile]);

  useEffect(() => {
    const fetchOneTimeMessages = async () => {
      try {
        const oneTimeMsgs = await db.oneTimeMessages.toArray();
        setOneTimeMessage(oneTimeMsgs);
      } catch (error) {
        console.error('Error fetching one-time messages:', error);
      }
    };
    fetchOneTimeMessages();
  }, [newOneTimeMessageState]);

  useEffect(() => {
    const updateReceiverInfo = async () => {
      if (receiverEmail) {
        try {
          const response = await fetch(`http://localhost:5038/api/users/${receiverEmail}/email`, {
            headers: { Authorization: `Bearer ${token}` },
          });
          const data = await response.json();
          setReceiverProfile(data);
          setReceiverChat(null);
          setReceiverNotes(null);
        } catch (error) {
          console.error('Error fetching receiver profile:', error);
        }
      } else if (receiverChatId) {
        try {
          const response = await fetch(`http://localhost:5038/api/chat/${receiverChatId}`, {
            headers: { Authorization: `Bearer ${token}` },
          });
          const data = await response.json();
          setReceiverChat(data);
          setReceiverProfile(null);
          setReceiverNotes(null);
        } catch (error) {
          console.error('Error fetching receiver chat:', error);
        }
      } else if (notesSelect) {
        setReceiverNotes(user);
        setReceiverChat(null);
        setReceiverProfile(null);
      } else {
        setReceiverProfile(null);
        setReceiverChat(null);
        setReceiverNotes(null);
      }
    };
    updateReceiverInfo();
  }, [receiverEmail, receiverChatId, notesSelect, user, token]);

  useEffect(() => {
    const checkBlockStatus = async () => {
      if (receiverEmail && receiverProfile && receiverProfile.id !== null) {
        try {
          const response = await fetch(`http://localhost:5038/api/contacts/${user.id}/contact/${receiverProfile.id}`, {
            headers: { Authorization: `Bearer ${token}` },
          });
          const data = await response.json();
          setIsBlockedByUser(data.isBlockedByUser);
          setIsBlockedByContact(data.isBlockedByContact);

          if (data.isBlockedByContact) {
            setBlockMessage('You are blocked by this contact.');
          } else if (data.isBlockedByUser) {
            setBlockMessage('You have blocked this contact.');
          } else {
            setBlockMessage('');
          }
        } catch (error) {
          console.error('Error checking block status:', error);
        }
      } else {
        setIsBlockedByUser(false);
        setIsBlockedByContact(false);
        setBlockMessage('');
      }
    };

    checkBlockStatus();
  }, [receiverEmail, user.id, token, receiverProfile]);

  useEffect(() => {
    scrollToBottom();
  }, [messages, files, scrollToBottom]);

  const sendMessage = async () => {
    if (!connection) {
      console.error('No SignalR connection available.');
      return;
    }
    if (receiverEmail) {
      try {
        const senderResponse = await fetch(`http://localhost:5038/api/users/${user.email}/email`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        const dataS = await senderResponse.json();

        const receiverResponse = await fetch(`http://localhost:5038/api/users/${receiverEmail}/email`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        const dataR = await receiverResponse.json();

        const privateResponse = await fetch(`http://localhost:5038/api/chat/private/${dataS.id}/${dataR.id}`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        const data = await privateResponse.json();

        const newMsg = {
          senderId: user.email,
          receiverId: receiverEmail,
          decryptedMessage: message,
          timestamp: new Date().toISOString(),
          isRead: false,
          isSent: false
        };

        if (isOneTimeMessage) {
          await connection.invoke("SendMessage", user.email, data.id, receiverEmail, message, newMsg.timestamp, true);
        } else {
          await connection.invoke("SendMessage", user.email, data.id, receiverEmail, message, newMsg.timestamp, false);
          await db.messages.add({
            userId: user.id,
            senderId: user.email,
            receiverId: receiverEmail,
            decryptedMessage: message,
            timestamp: newMsg.timestamp,
            isRead: true,
            isSent: true
          });
          setMessages(prev => [...prev, newMsg]);
        }
        setMessage('');
        scrollToBottom();
      } catch (err) {
        console.error('Error sending message: ', err);
      }
    } else if (receiverChatId) {
      try {
        const newMsg = {
          senderId: user.email,
          chatId: receiverChatId,
          receiverId: null,
          decryptedMessage: message,
          timestamp: new Date().toISOString(),
          isRead: false,
          isSent: false
        };
        await connection.invoke("SendMessage", user.email, receiverChatId, newMsg.receiverId, message, newMsg.timestamp, false);
        await db.chatmessages.add({
          userId: user.id,
          senderId: user.email,
          chatId: receiverChatId,
          decryptedMessage: message,
          timestamp: newMsg.timestamp,
          isRead: true,
          isSent: true
        });
        setMessages(prev => [...prev, newMsg]);
        setMessage('');
        scrollToBottom();
      } catch (err) {
        console.error('Error sending message: ', err);
      }
    } else if (notesSelect) {
      try {
        const senderResponse = await fetch(`http://localhost:5038/api/users/${user.email}/email`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        const dataS = await senderResponse.json();

        const privateResponse = await fetch(`http://localhost:5038/api/chat/private/${dataS.id}/${dataS.id}`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        const data = await privateResponse.json();

        const newMsg = {
          senderId: user.email,
          receiverId: user.email,
          decryptedMessage: message,
          timestamp: new Date().toISOString(),
          isRead: false,
          isSent: false
        };

        if (isOneTimeMessage) {
          await connection.invoke("SendMessage", user.email, data.id, user.email, message, newMsg.timestamp, true);
        } else {
          await connection.invoke("SendMessage", user.email, data.id, user.email, message, newMsg.timestamp, false);
          await db.messages.add({
            userId: user.id,
            senderId: user.email,
            receiverId: user.email,
            decryptedMessage: message,
            timestamp: newMsg.timestamp,
            isRead: true,
            isSent: true
          });
          setMessages(prev => [...prev, newMsg]);
        }
        setMessage('');
        scrollToBottom();
      } catch (err) {
        console.error('Error sending message: ', err);
      }
    }
  };

  const sendFile = async (file) => {
    if (!connection) {
      console.error('No SignalR connection available.');
      return;
    }

    const reader = new FileReader();
    reader.onload = async (event) => {
      const fileData = event.target.result.split(',')[1];
      const fileName = file.name;
      const fileType = file.type;
      const timestamp = new Date().toISOString();

      const newFile = {
        senderId: user.email,
        receiverId: receiverEmail || user.email,
        chatId: receiverChatId || null,
        fileName,
        fileType,
        fileData,
        timestamp,
        isRead: false,
        isSent: false,
      };

      let currentChatId;
      if (receiverEmail) {
        try {
          const senderResponse = await fetch(`http://localhost:5038/api/users/${user.email}/email`, {
            headers: { Authorization: `Bearer ${token}` },
          });
          const dataS = await senderResponse.json();

          const receiverResponse = await fetch(`http://localhost:5038/api/users/${receiverEmail}/email`, {
            headers: { Authorization: `Bearer ${token}` },
          });
          const dataR = await receiverResponse.json();

          const privateResponse = await fetch(`http://localhost:5038/api/chat/private/${dataS.id}/${dataR.id}`, {
            headers: { Authorization: `Bearer ${token}` },
          });
          const data = await privateResponse.json();
          currentChatId = data.id;

          await connection.invoke("SendFile", user.email, currentChatId, receiverEmail, fileName, fileData, fileType, timestamp);

          await db.files.add({
            userId: user.id,
            senderId: user.email,
            receiverId: receiverEmail,
            fileName,
            fileType,
            fileData,
            timestamp,
            isRead: true,
            isSent: true,
            isGroup: false,
          });

          setFiles(prev => [...prev, newFile]);
          scrollToBottom();
        } catch (err) {
          console.error('Error sending file: ', err);
        }
      } else if (receiverChatId) {
        try {
          await connection.invoke("SendFile", user.email, receiverChatId, null, fileName, fileData, fileType, timestamp);

          await db.files.add({
            userId: user.id,
            senderId: user.email,
            chatId: receiverChatId,
            fileName,
            fileType,
            fileData,
            timestamp,
            isRead: true,
            isSent: true,
            isGroup: true,
          });

          setFiles(prev => [...prev, newFile]);
          scrollToBottom();
        } catch (err) {
          console.error('Error sending file: ', err);
        }
      } else if (notesSelect) {
        try {
          const senderResponse = await fetch(`http://localhost:5038/api/users/${user.email}/email`, {
            headers: { Authorization: `Bearer ${token}` },
          });
          const dataS = await senderResponse.json();

          const privateResponse = await fetch(`http://localhost:5038/api/chat/private/${dataS.id}/${dataS.id}`, {
            headers: { Authorization: `Bearer ${token}` },
          });
          const data = await privateResponse.json();
          currentChatId = data.id;

          await connection.invoke("SendFile", user.email, currentChatId, user.email, fileName, fileData, fileType, timestamp);

          await db.files.add({
            userId: user.id,
            senderId: user.email,
            receiverId: user.email,
            fileName,
            fileType,
            fileData,
            timestamp,
            isRead: true,
            isSent: true,
            isGroup: false,
          });

          setFiles(prev => [...prev, newFile]);
          scrollToBottom();
        } catch (err) {
          console.error('Error sending file: ', err);
        }
      }
    };
    reader.readAsDataURL(file);
  };

  const updateMessages = useCallback(async () => {
    try {
      const oneTimeMessages = await db.oneTimeMessages.toArray();
      setOneTimeMessage(oneTimeMessages);
      setNewOneTimeMessageState(Date.now());
    } catch (error) {
      console.error('Error updating one-time messages:', error);
    }
  }, []);


  const handleBlockContact = async (contactId) => {
    try {
      await axios.put(
        `http://localhost:5038/api/contacts/${user.id}/contact/${contactId}/block`,
        {
          UserId: user.id,
          ContactUserId: contactId,
          IsBlocked: true,
        },
        {
          headers: { Authorization: `Bearer ${token}` },
        }
      );
      setIsBlockedByUser(true);
      fetchContacts();
    } catch (error) {
      console.error('Error blocking contact:', error);
    }
  };

  const handleUnBlockContact = async (contactId) => {
    try {
      await axios.put(
        `http://localhost:5038/api/contacts/${user.id}/contact/${contactId}/block`,
        {
          UserId: user.id,
          ContactUserId: contactId,
          IsBlocked: false,
        },
        {
          headers: { Authorization: `Bearer ${token}` },
        }
      );
      setIsBlockedByUser(false);
      fetchContacts();
    } catch (error) {
      console.error('Error unblocking contact:', error);
    }
  };

  const handleDeleteChat = async (contactEmail) => {
    try {
      await db.messages
        .where('senderId')
        .equals(contactEmail)
        .or('receiverId')
        .equals(contactEmail)
        .delete();
      console.log('Chat deleted successfully from IndexedDB.');
      setReceiverEmail(null);
      fetchContacts();
      fetchChats();
    } catch (error) {
      console.error('Error deleting chat from IndexedDB:', error);
    }
  };


  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        height: '100vh',
        width: '100%',
        backgroundColor: 'var(--white-color)',
      }}
    >
      <ChatContainer
        user={user}
        token={token}
        onLogout={onLogout}
        connection={connection}
        messages={messages}
        files={files}
        message={message}
        setMessage={setMessage}
        sendMessage={sendMessage}
        isOneTimeMessage={isOneTimeMessage}
        setIsOneTimeMessage={setIsOneTimeMessage}
        onSendFile={sendFile}
        receiverEmail={receiverEmail}
        setReceiverEmail={setReceiverEmail}
        receiverChatId={receiverChatId}
        setReceiverChatId={setReceiverChatId}
        notesSelect={notesSelect}
        setNotesSelect={setNotesSelect}
        receiverProfile={receiverProfile}
        receiverChat={receiverChat}
        receiverNotes={receiverNotes}
        messagesEndRef={messagesEndRef}
        selectedProfile={selectedProfile}
        setSelectedProfile={setSelectedProfile}
        contacts={contacts}
        setContacts={setContacts}
        chats={chats}
        setChats={setChats}
        unreadMessages={{}}
        newMessage={newMessageState}
        newOneTimeMessage={newOneTimeMessageState}
        friendRequests={friendRequests}
        setFriendRequests={setFriendRequests}
        onBlockContact={handleBlockContact}
        onUnBlockContact={handleUnBlockContact}
        onDeleteChat={handleDeleteChat}
        isBlockedByUser={isBlockedByUser}
        isBlockedByContact={isBlockedByContact}
        blockMessage={blockMessage}
        updateMessages={updateMessages}
        contactStatus={contactStatus}
        handleUserOnline={handleUserOnline}
        handleUserOffline={handleUserOffline}
        handleReceiveOnlineContacts={handleReceiveOnlineContacts}
      />
      <Snackbar
        open={snackbarOpen}
        autoHideDuration={5000}
        onClose={handleSnackbarClose}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
      >
        <Alert
          onClose={handleSnackbarClose}
          severity={snackbarSeverity}
          sx={{ width: '100%', display: 'flex', alignItems: 'center' }}
        >
          {snackbarMessage}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default Chat;