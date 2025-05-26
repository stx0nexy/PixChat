import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Paper, Box, Typography, List, ListItem, ListItemText, IconButton, Tooltip, Modal, Button, TextField, Snackbar, Alert } from '@mui/material';
import { useSignalR } from '../../stores/useSignalR';
import { ChatHeader } from '../../components/ChatHeader';
import { MessageList } from '../../components/MessageList';
import { MessageInput } from '../../components/MessageInput';
import AddContact from '../Contacts/AddContact';
import ContactsList from '../Contacts/ContactsList';
import { extractMessageFromImage } from '../../api/messageService';
import db from '../../stores/db';
import ViewProfile from '../Profile/ViewProfile';
import CheckIcon from '@mui/icons-material/Check';
import CreateChatForm from '../../components/CreateChatForm';
import axios from 'axios';
import ChatIcon from '@mui/icons-material/Chat';
import PersonIcon from '@mui/icons-material/Person';
import PersonAddIcon from '@mui/icons-material/PersonAdd';
import GroupAddIcon from '@mui/icons-material/GroupAdd';
import SettingsIcon from '@mui/icons-material/Settings';
import ExitToAppIcon from '@mui/icons-material/ExitToApp';
import SyncIcon from '@mui/icons-material/Sync';
import CloseIcon from '@mui/icons-material/Close';

const Chat = ({ user, token, onLogout }) => {
  const navigate = useNavigate();
  const messagesEndRef = useRef(null);
  const [message, setMessage] = useState('');
  const [decodedRarMessage, setDecodedRarMessage] = useState('');
  const [receiverEmail, setReceiverEmail] = useState(null);
  const [receiverChatId, setReceiverChatId] = useState(null);
  const [notesSelect, setNotesSelect] = useState(false);
  const [receiverChat, setReceiverChat] = useState('');
  const [receiverProfile, setReceiverProfile] = useState('');
  const [receiverNotes, setReceiverNotes] = useState('');
  const [messages, setMessages] = useState([]);
  const [files, setFiles] = useState([]);
  const [oneTimeMessage, setOneTimeMessage] = useState([]);
  const [anchorEl, setAnchorEl] = useState(null);
  const [unreadMessages, setUnreadMessages] = useState({});
  const [newMessageState, setNewMessageState] = useState(null);
  const [newOneTimeMessageState, setNewOneTimeMessageState] = useState(null);
  const [contacts, setContacts] = useState([]);
  const [chats, setChats] = useState([]);
  const [selectedProfile, setSelectedProfile] = useState(null);
  const [friendRequests, setFriendRequests] = useState([]);
  const [showAddContact, setShowAddContact] = useState(false);
  const [showCreateChat, setShowCreateChat] = useState(false);
  const [isBlockedByUser, setIsBlockedByUser] = useState(false);
  const [isBlockedByContact, setIsBlockedByContact] = useState(false);
  const [blockMessage, setBlockMessage] = useState('');
  const [isOneTimeMessage, setIsOneTimeMessage] = useState(false);
  const [contactStatus, setContactStatus] = useState({});
  const [showSyncModal, setShowSyncModal] = useState(false);
  const [syncFile, setSyncFile] = useState(null);
  const [snackbarOpen, setSnackbarOpen] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState('');
  const [snackbarSeverity, setSnackbarSeverity] = useState('info');

  const showSnackbar = (message, severity = 'info') => {
    setSnackbarMessage(message);
    setSnackbarSeverity(severity);
    setSnackbarOpen(true);
  };

   const handleSnackbarClose = (event, reason) => {
    if (reason === 'clickaway') {
      return;
    }
    setSnackbarOpen(false);
    // setSnackbarAction(null);
  };

  const decryptFile = async (encryptedFileData, encryptedAESKey, aesIV) => {
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
          encryptedAESKey: encryptedAESKey,
          privateKey: privateKey,
        }),
      });
  
      if (!aesKeyResponse.ok) {
        const errorText = await aesKeyResponse.text();
        console.error('Server response:', errorText);
        throw new Error(`Ошибка дешифрования AES-ключа: ${aesKeyResponse.status} - ${errorText}`);
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
      if (!decryptedFileResponse.ok) throw new Error(`Ошибка дешифрования файла: ${decryptedFileResponse.status}`);
      const decryptedFile = await decryptedFileResponse.text();
  
      return decryptedFile;
    } catch (error) {
      console.error('Ошибка при дешифровании файла:', error);
      return encryptedFileData;
    }
  };

  useEffect(() => {
    const fetchFiles = async () => {
      let fetchedFiles = [];
      if (receiverEmail) {
        fetchedFiles = await db.files
          .where('userId')
          .equals(user.id)
          .and(file => file.senderId === receiverEmail || file.receiverId === receiverEmail)
          .toArray();
      } else if (receiverChatId) {
        fetchedFiles = await db.files
          .where('chatId')
          .equals(receiverChatId)
          .toArray();
      } else if (notesSelect) {
        fetchedFiles = await db.files
          .where('userId') 
          .equals(user.id)
          .and(file => file.senderId === user.email && file.receiverId === user.email)
          .toArray();
      }
  
      const decryptedFiles = await Promise.all(
        fetchedFiles.map(async (file) => {
          const decryptedFileData = await decryptFile(file.fileData, file.encryptedAESKey, file.aesIV);
          return { ...file, fileData: decryptedFileData };
        })
      );
  
      setFiles(decryptedFiles);
    };
  
    fetchFiles();
  }, [receiverEmail, receiverChatId, notesSelect, user.id, user.email]);

  const exportData = async () => {
    try {
      const messagesData = await db.messages.toArray();
      const chatMessagesData = await db.chatmessages.toArray();
      const oneTimeMessagesData = await db.oneTimeMessages.toArray();
      const filesData = await db.files.toArray();

      const exportData = {
        messages: messagesData,
        chatmessages: chatMessagesData,
        oneTimeMessages: oneTimeMessagesData,
        files: filesData,
      };

      const jsonString = JSON.stringify(exportData, null, 2);
      const blob = new Blob([jsonString], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `chat_data_${new Date().toISOString()}.json`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);

      console.log('Data exported successfully');
    } catch (error) {
      console.error('Error exporting data:', error);
    }
  };

  const importData = async (file) => {
    try {
      const reader = new FileReader();
      reader.onload = async (event) => {
        const jsonString = event.target.result;
        const importData = JSON.parse(jsonString);

        await db.messages.clear();
        await db.chatmessages.clear();
        await db.oneTimeMessages.clear();
        await db.files.clear();

        if (importData.messages) {
          await db.messages.bulkAdd(importData.messages);
        }
        if (importData.chatmessages) {
          await db.chatmessages.bulkAdd(importData.chatmessages);
        }
        if (importData.oneTimeMessages) {
          await db.oneTimeMessages.bulkAdd(importData.oneTimeMessages);
        }
        if (importData.files) {
          await db.files.bulkAdd(importData.files);
        }

        console.log('Data imported successfully');

        let updatedMessages = [];
        let updatedChatMessages = [];
        let updatedFiles = [];

        if (user?.id && (typeof user.id === 'string' || typeof user.id === 'number')) {
          if (receiverEmail) {
            updatedMessages = await db.messages
              .where('userId')
              .equals(user.id)
              .and(msg => msg.senderId === receiverEmail || msg.receiverId === receiverEmail)
              .toArray();
            updatedFiles = await db.files
              .where('userId')
              .equals(user.id)
              .and(file => file.senderId === receiverEmail || file.receiverId === receiverEmail)
              .toArray();
          } else if (notesSelect) {
            updatedMessages = await db.messages
              .where('userId')
              .equals(user.id)
              .and(msg => msg.senderId === user.email && msg.receiverId === user.email)
              .toArray();
            updatedFiles = await db.files
              .where('userId')
              .equals(user.id)
              .and(file => file.senderId === user.email && file.receiverId === user.email)
              .toArray();
          }
          window.location.reload();
        } else {
          console.warn('user.id is not valid, skipping messages update:', user?.id);
        }

        if (receiverChatId && (typeof receiverChatId === 'string' || typeof receiverChatId === 'number')) {
          updatedChatMessages = await db.chatmessages
            .where('chatId')
            .equals(receiverChatId)
            .toArray();
          updatedFiles = await db.files
            .where('chatId')
            .equals(receiverChatId)
            .toArray();
        } else {
          console.warn('receiverChatId is not valid, skipping chat messages update:', receiverChatId);
        }

        const updatedOneTimeMessages = await db.oneTimeMessages.toArray();

        setMessages([...updatedMessages, ...updatedChatMessages].sort((a, b) => new Date(a.timestamp) - new Date(b.timestamp)));
        setFiles(updatedFiles);
        setOneTimeMessage(updatedOneTimeMessages);
        fetchContacts();
        fetchChats();
      };
      reader.readAsText(file);
    } catch (error) {
      console.error('Error importing data:', error);
    }
  };

  const handleFileChange = (event) => {
    const file = event.target.files[0];
    if (file) {
      setSyncFile(file);
    }
  };

  const handleImport = () => {
    if (syncFile) {
      importData(syncFile);
      setShowSyncModal(false);
      setSyncFile(null);
    }
  };

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
  }, [receiverEmail, user.id, token, receiverProfile?.id, receiverProfile]);

  const handleProfileOpen = (profile) => {
    setSelectedProfile(profile);
  };

  const handleProfileClose = () => {
    setSelectedProfile(null);
  };

  const scrollToBottom = useCallback(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, []);

  useEffect(() => {
    scrollToBottom();
  }, [messages, files, scrollToBottom]);

  useEffect(() => {
    const fetchMessages = async () => {
      setMessages([]);
      let msgs = [];

      if (receiverEmail) {
        msgs = await db.messages
          .where('userId').equals(user.id)
          .and(msg => msg.senderId === receiverEmail || msg.receiverId === receiverEmail)
          .toArray();
      } else if (receiverChatId) {
        msgs = await db.chatmessages
          .where('chatId').equals(receiverChatId)
          .toArray();
      } else if (notesSelect) {
        msgs = await db.messages
          .where('userId').equals(user.id)
          .and(msg => msg.senderId === user.email && msg.receiverId === user.email)
          .toArray();
      }

      const sortedMsgs = msgs.sort((a, b) => {
        const dateA = parseCustomDate(a.timestamp);
        const dateB = parseCustomDate(b.timestamp);
        return dateA - dateB;
      });

      setMessages(sortedMsgs);
    };

    const parseCustomDate = (dateStr) => {
      if (!dateStr || typeof dateStr !== 'string') {
        console.warn('Invalid timestamp:', dateStr);
        return new Date(0);
      }

      if (dateStr.includes('T')) {
        return new Date(dateStr);
      }

      try {
        const parts = dateStr.split(', ');
        if (parts.length !== 2) {
          throw new Error('Unexpected date format');
        }

        const [datePart, timePart] = parts;
        const [day, month, year] = datePart.split('.').map(Number);
        const [hours, minutes, seconds] = (timePart || '00:00:00').split(':').map(Number);

        return new Date(year, month - 1, day, hours, minutes, seconds);
      } catch (error) {
        console.warn('Failed to parse timestamp:', dateStr, error);
        return new Date(0);
      }
    };
    fetchMessages();
  }, [receiverEmail, receiverChatId, notesSelect, user.id, user.email]);

  useEffect(() => {
    if (receiverEmail && unreadMessages[receiverEmail]) {
      setUnreadMessages(prev => ({
        ...prev,
        [receiverEmail]: 0,
      }));
    }
  }, [receiverEmail, unreadMessages]);

  const updateMessages = useCallback(async () => {
    try {
      const oneTimeMessages = await db.oneTimeMessages.toArray();
      setOneTimeMessage(oneTimeMessages);
      setNewOneTimeMessageState(Date.now());
    } catch (error) {
      console.error('Error updating one-time messages:', error);
    }
  }, []);

  const handleContactSelect = (email) => {
    setNotesSelect(false);
    setReceiverChatId(null);
    setReceiverEmail(email);
  };

  const handleChatSelect = (chatId) => {
    setNotesSelect(false);
    setReceiverEmail(null);
    setReceiverChatId(chatId);
  };

  const handleNotesSelect = () => {
    setReceiverEmail(null);
    setReceiverChatId(null);
    setNotesSelect(true);
  };

  const fetchContacts = useCallback(async () => {
    if (!user?.id || !token) return [];

    try {
      const response = await fetch(`http://localhost:5038/api/contacts/${user.id}/contacts`, {
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
      console.log('Contacts data:', data);
      setContacts(data);
      return data;
    } catch (error) {
      console.error('Error fetching contacts list:', error);
      return [];
    }
  }, [user?.id, token]);

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
  }, [user?.id, token]);

  useEffect(() => {
    const loadInitialData = async () => {
      await fetchContacts();
      await fetchChats();
    };
    loadInitialData();
  }, [fetchContacts, fetchChats]);

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
    [user, token, scrollToBottom, contacts, fetchContacts, receiverProfile]
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
  }, [user, token, fetchContacts]);

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
  }, [user, token, contacts, fetchContacts]);

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

      setOneTimeMessage(newMsg);

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

      setOneTimeMessage(newMsg);

      console.log('Full one-time pending message info saved:', { extractedMessage, messageId, chatId, senderId, timestamp });
    }
  }, [user, token, contacts, fetchContacts]);

  const handleNewGroupMessage = useCallback(async (chatId, senderId, base64Image, encryptedKey, messageLength, timestamp) => {
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
      console.log('New message received:', newMsg);
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
  }, [user, token, scrollToBottom]);

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
        setDecodedRarMessage(decodedMessage);
      } else {
        const decodedMessage = await extractMessageFromImage(
          user,
          token,
          senderId,
          base64Image,
          null,
          messageId,
        );
        setDecodedRarMessage(decodedMessage);
      }
      const newMsg = {
        senderId,
        message: decodedRarMessage,
        timestamp: timestamp || new Date().toISOString(),
        messageId
      };
      console.log('New pending message: ', decodedRarMessage);
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
      if (!isContact && !receiverChatId) {
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
  
      if (receiverChatId) {
        await db.files.add({
          userId: user.id,
          senderId,
          chatId: receiverChatId,
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

      const chatName = senderUsername;
      const messageTime = new Date(newFile.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
      const messageDate = new Date(newFile.timestamp).toLocaleDateString();

      const snackbarContent = (
        <Box sx={{ display: 'flex', flexDirection: 'column', width: '100%' }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%' }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>{senderUsername}</Typography>
            <Typography variant="caption" sx={{ ml: 1 }}>{messageDate} {messageTime}</Typography>
          </Box>
          <Typography variant="body2">New pending file: "{fileName}"</Typography>
        </Box>
      );
      showSnackbar(snackbarContent, 'info');
    }
  }, [user, token, contacts, fetchContacts, receiverProfile, receiverChatId, scrollToBottom]);

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
      const chatName = senderUsername;
      const messageTime = new Date(newFile.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
      const messageDate = new Date(newFile.timestamp).toLocaleDateString();

      const snackbarContent = (
        <Box sx={{ display: 'flex', flexDirection: 'column', width: '100%' }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%' }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>{senderUsername} {isGroup ? `in ${chatName}` : ''}</Typography>
            <Typography variant="caption" sx={{ ml: 1 }}>{messageDate} {messageTime}</Typography>
          </Box>
          <Typography variant="body2">New pending file: "{fileName}"</Typography>
        </Box>
      );
      showSnackbar(snackbarContent, 'info');
    }
  }, [user, token, receiverProfile, scrollToBottom]);

  const acceptFriendRequest = async (requesterId) => {
    if (!connection) {
      console.error('No SignalR connection.');
      return;
    }
    try {
      await connection.invoke("ConfirmFriendRequest", user.email, requesterId);
      setFriendRequests(prev => prev.filter(id => id !== requesterId));
      fetchContacts();
    } catch (error) {
      console.error('Error accepting friend request:', error);
    }
  };

  const rejectFriendRequest = async (requesterId) => {
    if (!connection) {
      console.error('No SignalR connection.');
      return;
    }
    try {
      await connection.invoke("RejectFriendRequest", user.email, requesterId);
      setFriendRequests(prev => prev.filter(id => id !== requesterId));
    } catch (error) {
      console.error('Error rejecting friend request:', error);
    }
  };

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

  const handleUserOnline = (userId) => {
    setContactStatus((prev) => ({
      ...prev,
      [userId]: true,
    }));
  };

  const handleUserOffline = (userId) => {
    setContactStatus((prev) => ({
      ...prev,
      [userId]: false,
    }));
  };

  const handleReceiveOnlineContacts = (onlineContactEmails) => {
    const newStatus = {};
    onlineContactEmails.forEach((email) => {
      newStatus[email] = true;
    });
    setContactStatus((prev) => ({
      ...prev,
      ...newStatus,
    }));
  };

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

  const sendMessage = async () => {
    if (!connection) {
      console.error('No SignalR connection available.');
      return;
    }
    if (receiverEmail) {
      try {
        console.log('Sending message:', user.email, receiverEmail, message);

        const senderResponse = await fetch(`http://localhost:5038/api/users/${user.email}/email`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        const dataS = await senderResponse.json();
        console.log('User data:', dataS);

        const receiverResponse = await fetch(`http://localhost:5038/api/users/${receiverEmail}/email`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        const dataR = await receiverResponse.json();
        console.log('Receiver data:', dataR);

        const privateResponse = await fetch(`http://localhost:5038/api/chat/private/${dataS.id}/${dataR.id}`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        const data = await privateResponse.json();
        console.log('Private data:', data);

        const newMsg = {
          senderId: user.email,
          receiverId: receiverEmail,
          decryptedMessage: message,
          timestamp: new Date().toISOString(),
          isRead: false,
          isSent: false
        };

        console.log('Invoking SendMessage with params:', {
          senderId: user.email,
          chatId: data.id,
          receiverId: receiverEmail,
          message: message,
          timestamp: newMsg.timestamp
        });
        if (isOneTimeMessage) {
          await connection.invoke("SendMessage", user.email, data.id, receiverEmail, message, newMsg.timestamp, true);
          console.log('ONE TIME Message sent successfully.');
        } else {
          await connection.invoke("SendMessage", user.email, data.id, receiverEmail, message, newMsg.timestamp, false);
          console.log('Message sent successfully.');

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
          setNewMessageState(newMsg);
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

        console.log('Invoking SendMessage with params:', {
          senderId: user.email,
          chatId: receiverChatId,
          message: message,
          timestamp: newMsg.timestamp
        });

        await connection.invoke("SendMessage", user.email, receiverChatId, newMsg.receiverId, message, newMsg.timestamp, false);
        console.log('Message sent successfully.');

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
        setNewMessageState(newMsg);
        setMessage('');
        scrollToBottom();
      } catch (err) {
        console.error('Error sending message: ', err);
      }
    } else if (notesSelect) {
      try {
        console.log('Sending message:', user.email, message);

        const senderResponse = await fetch(`http://localhost:5038/api/users/${user.email}/email`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        const dataS = await senderResponse.json();
        console.log('User data:', dataS);

        const privateResponse = await fetch(`http://localhost:5038/api/chat/private/${dataS.id}/${dataS.id}`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        const data = await privateResponse.json();
        console.log('Private data:', data);

        const newMsg = {
          senderId: user.email,
          receiverId: user.email,
          decryptedMessage: message,
          timestamp: new Date().toISOString(),
          isRead: false,
          isSent: false
        };

        console.log('Invoking SendMessage with params:', {
          senderId: user.email,
          chatId: data.id,
          receiverId: user.email,
          message: message,
          timestamp: newMsg.timestamp
        });

        if (isOneTimeMessage) {
          await connection.invoke("SendMessage", user.email, data.id, user.email, message, newMsg.timestamp, true);
          console.log('ONE TIME Message sent successfully.');
        } else {
          await connection.invoke("SendMessage", user.email, data.id, user.email, message, newMsg.timestamp, false);
          console.log('Message sent successfully.');

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
          setNewMessageState(newMsg);
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
  
      let chatId;
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
          chatId = data.id;
  
          await connection.invoke("SendFile", user.email, chatId, receiverEmail, fileName, fileData, fileType, timestamp);
          console.log('File sent successfully.');
  
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
          console.log('File sent successfully.');
  
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
          chatId = data.id;
  
          await connection.invoke("SendFile", user.email, chatId, user.email, fileName, fileData, fileType, timestamp);
          console.log('File sent successfully.');
  
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

  const addReceiverProfile = useCallback(async (email) => {
    if (email) {
      try {
        const response = await fetch(`http://localhost:5038/api/users/${email}/email`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        const data = await response.json();
        console.log('User data:', data);
        setReceiverProfile(data);
      } catch (error) {
        console.error('Error fetching user data:', error);
      }
    }
  }, [token]);

  const addReceiverChat = useCallback(async (chatId) => {
    if (chatId) {
      try {
        const response = await fetch(`http://localhost:5038/api/chat/${chatId}`, {
          headers: {
            'Authorization': `Bearer ${token}`,
          },
        });

        if (!response.ok) {
          throw new Error('Error loading chat');
        }
        const data = await response.json();
        setReceiverChat(data);
      } catch (error) {
        console.error('Error fetching user data:', error);
      }
    }
  }, [token]);

  const addReceiverNotes = useCallback(async () => {
    try {
      setReceiverNotes(user);
    } catch (error) {
      console.error('Error fetching user data:', error);
    }
  }, [user]);

  useEffect(() => {
    if (receiverEmail) {
      addReceiverProfile(receiverEmail);
      setReceiverChatId(null);
      setReceiverChat(null);
      setReceiverNotes(null);
    } else if (receiverChatId) {
      addReceiverChat(receiverChatId);
      setReceiverEmail(null);
      setReceiverProfile(null);
      setReceiverNotes(null);
    } else if (notesSelect) {
      addReceiverNotes();
      setReceiverChatId(null);
      setReceiverChat(null);
      setReceiverEmail(null);
      setReceiverProfile(null);
    }
  }, [receiverEmail, receiverChatId, addReceiverProfile, addReceiverChat, addReceiverNotes, notesSelect]);

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

  const handleDeleteChat = async (contactId) => {
    try {
      await db.messages
        .where('senderId')
        .equals(contactId)
        .or('receiverId')
        .equals(contactId)
        .delete();
      console.log('Chat deleted successfully.');
    } catch (error) {
      console.error('Error deleting chat:', error);
    }
  };

  return (
    <Box
      display="flex"
      flexDirection="row"
      sx={{
        height: '100%',
        width: '100%',
        padding: 1,
        gap: 0,
      }}
    >
      <Box
        flex={0.5}
        sx={{
          display: 'flex',
          flexDirection: 'column',
          height: '98vh',
          width: '100%',
        }}
      >
        <Paper
          sx={{
            height: '100%',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: 2,
            borderRadius: 0
          }}
        >
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <Tooltip title="Все чаты">
              <IconButton color="primary">
                <ChatIcon fontSize="large" />
              </IconButton>
            </Tooltip>

            <Tooltip title="Редактирование профиля">
              <IconButton
                color="primary"
                onClick={() => {
                  navigate('/profile');
                }}
              >
                <PersonIcon fontSize="large" />
              </IconButton>
            </Tooltip>

            <Tooltip title="Добавить контакт">
              <IconButton
                color="primary"
                onClick={() => {
                  setShowAddContact(true);
                }}
              >
                <PersonAddIcon fontSize="large" />
              </IconButton>
            </Tooltip>

            <Tooltip title="Создать группу">
              <IconButton
                color="primary"
                onClick={() => setShowCreateChat(true)}
              >
                <GroupAddIcon fontSize="large" />
              </IconButton>
            </Tooltip>


            <Tooltip title="Синхронизация данных">
              <IconButton
                color="primary"
                onClick={() => setShowSyncModal(true)}
              >
                <SyncIcon fontSize="large" />
              </IconButton>
            </Tooltip>
          </Box>

          <Box>
            <Tooltip title="Настройки">
              <IconButton color="secondary ">
              <SettingsIcon fontSize="large" />
            </IconButton>
          </Tooltip>
          <Tooltip title="Выйти">
            <IconButton color="secondary" onClick={onLogout}>
              <ExitToAppIcon fontSize="large" />
            </IconButton>
          </Tooltip>
        </Box>
      </Paper>
    </Box>

    <Modal
      open={showSyncModal}
      onClose={() => setShowSyncModal(false)}
      aria-labelledby="sync-modal-title"
      aria-describedby="sync-modal-description"
    >
      <Box
        sx={{
          position: 'absolute',
          top: '50%',
          left: '50%',
          transform: 'translate(-50%, -50%)',
          width: 400,
          bgcolor: 'background.paper',
          boxShadow: 24,
          p: 4,
          borderRadius: 2,
        }}
      >
        <Typography id="sync-modal-title" variant="h6" component="h2">
          Синхронизация данных
        </Typography>
        <Box sx={{ mt: 2, display: 'flex', flexDirection: 'column', gap: 2 }}>
          <Button
            variant="contained"
            color="primary"
            onClick={exportData}
          >
            Экспортировать данные
          </Button>
          <Box>
            <Typography variant="body1">Импортировать данные:</Typography>
            <TextField
              type="file"
              onChange={handleFileChange}
              inputProps={{ accept: 'application/json' }}
              fullWidth
              sx={{ mt: 1 }}
            />
            <Button
              variant="contained"
              color="secondary"
              onClick={handleImport}
              disabled={!syncFile}
              sx={{ mt: 1 }}
            >
              Импортировать
            </Button>
          </Box>
          <Button
            variant="outlined"
            onClick={() => setShowSyncModal(false)}
          >
            Закрыть
          </Button>
        </Box>
      </Box>
    </Modal>

    <Box
      flex={3.5}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        height: '98vh',
        width: '100%',
      }}
    >
      <Paper sx={{ height: '100%', display: 'flex', flexDirection: 'column', padding: 2, borderRadius: 0 }}>
        <ChatHeader
          onMenuClick={(e) => setAnchorEl(e.currentTarget)}
          onProfileClick={() => {
            setAnchorEl(null);
            navigate('/profile');
          }}
          onAddContactClick={() => {
            setAnchorEl(null);
            setShowAddContact(true);
          }}
          onCreateChat={() => setShowCreateChat(true)}
          onLogout={onLogout}
          anchorEl={anchorEl}
          open={Boolean(anchorEl)}
          onClose={() => setAnchorEl(null)}
        />
        {showAddContact && (
          <AddContact connection={connection} onClose={() => setShowAddContact(false)} user={user} token={token} />
        )}

        {showCreateChat && (
          <CreateChatForm user={user} contacts={contacts} onClose={() => setShowCreateChat(false)} token={token} />
        )}

        <Box sx={{ marginBottom: 2 }}>
          <List>
            {friendRequests.map((requesterId) => (
              <ListItem key={requesterId}>
                <ListItemText primary={requesterId} />
                <Box sx={{ display: 'flex', gap: 1 }}>
                  <IconButton
                    edge="end"
                    color="primary"
                    onClick={() => acceptFriendRequest(requesterId)}
                  >
                    <CheckIcon />
                  </IconButton>
                  <IconButton
                    edge="end"
                    color="secondary"
                    onClick={() => rejectFriendRequest(requesterId)}
                  >
                    <CloseIcon />
                  </IconButton>
                </Box>
              </ListItem>
            ))}
          </List>
        </Box>

        <Box
          sx={{
            flexGrow: 1,
            overflowY: 'auto',
            overflowX: 'hidden',
            maxHeight: '60vh',
            width: '100%'
          }}
        >
          <ContactsList
            user={user}
            onContactSelect={handleContactSelect}
            onChatSelect={handleChatSelect}
            onNotesSelect={handleNotesSelect}
            contactStatus={contactStatus}
            onProfileOpen={handleProfileOpen}
            token={token}
            unreadMessages={unreadMessages}
            newMessage={newMessageState}
            newOneTimeMessage={newOneTimeMessageState}
            contacts={contacts}
            setContacts={setContacts}
            chats={chats}
            setChats={setChats}
            onBlockContact={handleBlockContact}
            onUnBlockContact={handleUnBlockContact}
            onDeleteChat={handleDeleteChat}
            isBlocked={isBlockedByUser}
          />
          <ViewProfile profile={selectedProfile} onClose={handleProfileClose} />
        </Box>
      </Paper>
    </Box>

    <Box
      flex={8}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        height: '98vh',
        width: '100%'
      }}
    >
      <Paper
        sx={{
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
          padding: 2,
          borderRadius: 0
        }}
      >
        {!receiverEmail && !receiverChatId && !notesSelect ? (
          <Box
            sx={{
              flexGrow: 1,
              display: 'flex',
              justifyContent: 'center',
              alignItems: 'center'
            }}
          >
            <Typography variant="h6">Select a chat</Typography>
          </Box>
        ) : (
          <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
            <MessageList
              connection={connection}
              fulloneTimeMessage={oneTimeMessage}
              newOneTimeMessage={newOneTimeMessageState}
              messages={messages}
              files={files}
              user={user}
              chat={receiverChat}
              receiverProfile={receiverProfile}
              notes={receiverNotes}
              messagesEndRef={messagesEndRef}
              onProfileOpen={handleProfileOpen}
              onContactSelect={handleContactSelect}
              updateMessages={updateMessages}
            />
            {isBlockedByUser || isBlockedByContact ? (
              <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', padding: 2 }}>
                <Typography variant="body1" color="error">{blockMessage}</Typography>
              </Box>
            ) : (
              <MessageInput
                message={message}
                setMessage={setMessage}
                onSend={sendMessage}
                messageStatus={setIsOneTimeMessage}
                isGroupChat={!!receiverChatId}
                onSendFile={sendFile}
              />
            )}
            <ViewProfile profile={selectedProfile} onClose={handleProfileClose} />
          </Box>
        )}
      </Paper>
    </Box>
      <Snackbar
        open={snackbarOpen}
        autoHideDuration={5000} // Скрывать через 5 секунд
        onClose={handleSnackbarClose}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }} // Позиция Snackbar
      >
        <Alert
          onClose={handleSnackbarClose}
          severity={snackbarSeverity}
          sx={{ width: '100%', display: 'flex', alignItems: 'center' }}
        >
          {/* Содержимое Snackbar, которое мы формируем в showSnackbar */}
          {snackbarMessage}
          {/* Если нужно, можете добавить кнопку действия, как в предыдущих примерах, но вы просили ее убрать для этой задачи */}
        </Alert>
      </Snackbar>
  </Box>
  );
};

export default Chat;