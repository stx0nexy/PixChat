import React, { useEffect, useState } from 'react';
import { List } from '@mui/material';
import ContactItem from './ContactItem';
import ChatItem from './ChatItem';
import SelfChatItem from './SelfChatItem';
import db from '../../stores/db';

const ContactsList = ({ user, onContactSelect, onChatSelect, onNotesSelect, contactStatus, token, unreadMessages,
  newMessage, newOneTimeMessage, contacts, setContacts, chats, setChats, onProfileOpen, onBlockContact, 
  onUnBlockContact, onDeleteChat, isBlocked }) => {

  const [sortedItems, setSortedItems] = useState([]);

  useEffect(() => {
    const fetchLastMessages = async () => {
      try {
        const messages = await db.messages.toArray();
        const chatMessages = await db.chatmessages.toArray();
        const oneTimeMessages = await db.oneTimeMessages.toArray();

        const allMessages = [...messages, ...chatMessages, ...oneTimeMessages];

        const lastMessageMap = new Map();

        allMessages.forEach(msg => {
          const key = msg.chatId || msg.receiverId || msg.senderId;
          if (!msg.timestamp) {
            console.warn('Message without timestamp:', msg);
            return;
          }
          const msgTime = new Date(msg.timestamp);
          if (isNaN(msgTime.getTime())) {
            console.warn('Invalid timestamp:', msg.timestamp, msg);
            return;
          }
          const currentLast = lastMessageMap.get(key);
          if (!currentLast || new Date(currentLast.timestamp).getTime() < msgTime.getTime()) {
            lastMessageMap.set(key, msg);
          }
        });

        console.log("Messages from DB:", allMessages);
        console.log("Last message map:", Array.from(lastMessageMap.entries()));
        console.log("Contacts:", contacts);
        console.log("Chats:", chats);

        const combinedList = [
          { type: 'self', timestamp: '9999-12-31T23:59:59.999Z', name: 'Self' },
          ...await Promise.all(contacts.map(async contact => {
            const receiverResponse = await fetch(`http://localhost:5038/api/users/${contact.contactUserId}`, {
              headers: { Authorization: `Bearer ${token}` },
            });
            if (!receiverResponse.ok) {
              console.error(`API error for contactUserId ${contact.contactUserId}: ${receiverResponse.status}`);
              return {
                ...contact,
                type: 'contact',
                timestamp: '1970-01-01T00:00:00.000Z'
              };
            }
            const dataR = await receiverResponse.json();
            console.log('Receiver data:', dataR);

            const sentMsg = lastMessageMap.get(dataR.email);
            const receivedMsg = allMessages
              .filter(msg => msg.senderId === dataR.email && msg.receiverId === user.email)
              .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())[0];

            const lastMsg = (!sentMsg && receivedMsg) ? receivedMsg :
                            (sentMsg && !receivedMsg) ? sentMsg :
                            (!sentMsg && !receivedMsg) ? null :
                            new Date(sentMsg.timestamp).getTime() > new Date(receivedMsg.timestamp).getTime() ? sentMsg : receivedMsg;

            const timestamp = lastMsg ? lastMsg.timestamp : '1970-01-01T00:00:00.000Z';
            console.log(`Contact ${contact.contactUserId} (email: ${dataR.email}): sent = ${sentMsg?.timestamp || 'none'}, received = ${receivedMsg?.timestamp || 'none'}, selected = ${timestamp}`);
            return {
              ...contact,
              type: 'contact',
              timestamp
            };
          })),
          ...chats.map(chat => {
            const lastMsg = lastMessageMap.get(chat.id);
            const timestamp = lastMsg ? lastMsg.timestamp : '1970-01-01T00:00:00.000Z';
            console.log(`Chat ${chat.id}: timestamp = ${timestamp}`);
            return {
              ...chat,
              type: 'chat',
              timestamp
            };
          })
        ];

        console.log("Before sorting:", combinedList.map(item => ({ type: item.type, id: item.id || item.contactUserId, name: item.name, timestamp: item.timestamp })));

        combinedList.sort((a, b) => {
          const timeA = new Date(a.timestamp).getTime();
          const timeB = new Date(b.timestamp).getTime();
          return timeB - timeA;
        });

        console.log("After sorting:", combinedList.map(item => ({ type: item.type, id: item.id || item.contactUserId, name: item.name, timestamp: item.timestamp })));

        setSortedItems(combinedList);
      } catch (error) {
        console.error('Error loading messages:', error);
      }
    };

    fetchLastMessages();
  }, [contacts, chats, newMessage, token, user.email, newOneTimeMessage]);

  return (
    <List>
      {sortedItems.map(item => {
        if (item.type === 'self') {
          return (
            <SelfChatItem
              key="self"
              user={user}
              onSelfChatSelect={onNotesSelect}
              token={token}
              newMessage={newMessage}
              newOneTimeMessage={newOneTimeMessage}
            />
          );
        }
        if (item.type === 'contact') {
          return (
            <ContactItem
              key={`contact-${item.contactUserId}`}
              user={user}
              contact={item}
              onContactSelect={onContactSelect}
              token={token}
              unreadCount={unreadMessages[item.contactUserId] || 0}
              newMessage={newMessage}
              newOneTimeMessage={newOneTimeMessage}
              onContactRemove={() => setContacts(prev => prev.filter(c => c.contactUserId !== item.contactUserId))}
              onProfileOpen={onProfileOpen}
              onBlockContact={onBlockContact}
              onUnBlockContact={onUnBlockContact}
              onDeleteChat={onDeleteChat}
              isBlocked={isBlocked}
              contactStatus={contactStatus}
            />
          );
        }
        return (
          <ChatItem
            key={`chat-${item.id}`}
            chat={item}
            onChatSelect={onChatSelect}
            token={token}
            newMessage={newMessage}
            onChatRemove={() => setChats(prev => prev.filter(c => c.id !== item.id))}
          />
        );
      })}
    </List>
  );
};

export default ContactsList;