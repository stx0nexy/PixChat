import React, { useEffect, useState } from 'react';
import { Avatar, Typography, Badge, ListItem, ListItemAvatar, ListItemText, IconButton, Menu, MenuItem } from '@mui/material';
import { styled } from '@mui/material/styles';
import MoreVertIcon from '@mui/icons-material/MoreVert';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import db from '../../stores/db';

const defaultProfilePic = 'path-to-default-image.jpg';

const StyledBadge = styled(Badge)(({ theme }) => ({
  '& .MuiBadge-badge': {
    backgroundColor: '#44b700',
    color: '#44b700',
    boxShadow: `0 0 0 2px ${theme.palette.background.paper}`,
    '&::after': {
      position: 'absolute',
      top: 0,
      left: 0,
      width: '100%',
      height: '100%',
      borderRadius: '50%',
      animation: 'ripple 1.2s infinite ease-in-out',
      border: '1px solid currentColor',
      content: '""',
    },
  },
  '@keyframes ripple': {
    '0%': {
      transform: 'scale(.8)',
      opacity: 1,
    },
    '100%': {
      transform: 'scale(2.4)',
      opacity: 0,
    },
  },
}));

const ContactItem = ({
  user,
  contact,
  onContactSelect,
  token,
  newMessage,
  newOneTimeMessage,
  onContactRemove,
  onProfileOpen,
  onBlockContact,
  onUnBlockContact,
  onDeleteChat,
  isBlocked,
  contactStatus,
}) => {
  const [contactUser, setContactUser] = useState(null);
  const [lastMessage, setLastMessage] = useState('');
  const [unreadMessageCount, setUnreadMessageCount] = useState(0);
  const [unreadOneTimeMessageCount, setUnreadOneTimeMessageCount] = useState(0);
  const [anchorEl, setAnchorEl] = useState(null);

  useEffect(() => {
    const fetchUser = async () => {
      try {
        const response = await fetch(`http://localhost:5038/api/users/${contact.contactUserId}`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        if (!response.ok) {
          throw new Error(`API error for contactUserId ${contact.contactUserId}: ${response.status}`);
        }
        const data = await response.json();
        setContactUser(data);
      } catch (error) {
        console.error('Error getting user data:', error);
      }
    };
    fetchUser();
  }, [contact.contactUserId, token]);

  useEffect(() => {
    const fetchMessages = async () => {
      if (!contactUser || !contactUser.email || !user || !user.email) return;

      try {
        const messages = await db.messages
          .where('receiverId').equals(contactUser.email)
          .or('senderId').equals(contactUser.email)
          .toArray();

        const sortedMessages = messages.sort((a, b) => {
          return new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime();
        });

        if (sortedMessages.length > 0) {
          setLastMessage(sortedMessages[0].message);
          const unreadMessages = sortedMessages.filter(
            message => !message.isRead && message.senderId === contactUser.email
          );
          setUnreadMessageCount(unreadMessages.length);
        } else {
          setLastMessage('No messages');
          setUnreadMessageCount(0);
        }

        const oneTimeMessages = await db.oneTimeMessages
          .where('senderId').equals(contactUser.email)
          .toArray();

        if (oneTimeMessages.length > 0) {
          const unreadOneTimeMessages = oneTimeMessages.filter(
            oneTimeMessage => !oneTimeMessage.isRead && oneTimeMessage.senderId === contactUser.email
          );
          setUnreadOneTimeMessageCount(unreadOneTimeMessages.length);
        } else {
          setUnreadOneTimeMessageCount(0);
        }
      } catch (error) {
        console.error('Error getting messages:', error);
      }
    };

    fetchMessages();
  }, [contactUser, user, newMessage, newOneTimeMessage]);

  useEffect(() => {
    const fetchUnreadOneTimeMessages = async () => {
      if (!contactUser) return;

      try {
        const oneTimeMessages = await db.oneTimeMessages
          .where('senderId').equals(contactUser.email)
          .toArray();

        const unreadOneTimeMessages = oneTimeMessages.filter(
          msg => !msg.isRead && msg.senderId === contactUser.email
        );
        setUnreadOneTimeMessageCount(unreadOneTimeMessages.length);
      } catch (error) {
        console.error('Error fetching one-time messages:', error);
      }
    };

    fetchUnreadOneTimeMessages();
  }, [contactUser, newMessage, newOneTimeMessage]);

  const handleContactSelect = async (email) => {
    try {
      const unreadMessages = await db.messages
        .where('senderId').equals(email)
        .and(message => !message.isRead)
        .toArray();

      await Promise.all(unreadMessages.map(message => db.messages.update(message.id, { isRead: true })));
      setUnreadMessageCount(0);
      onContactSelect(email);
    } catch (error) {
      console.error('Error updating messages to "read":', error);
    }
  };

  const handleBlockContact = async (event) => {
    event.stopPropagation();
    try {
      if (isBlocked) {
        onUnBlockContact(contact.contactUserId);
      } else {
        onBlockContact(contact.contactUserId);
      }
    } catch (error) {
      console.error('Error blocking/unblocking contact:', error);
    }
  };

  const handleRemoveContact = async (event) => {
    event.stopPropagation();
    try {
      onDeleteChat(contactUser.email);
    } catch (error) {
      console.error('Error removing contact:', error);
    }
  };

  const handleMenuOpen = (event) => {
    event.stopPropagation();
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  if (!contactUser) return <div>Loading...</div>;

  const isOnline = contactStatus[contactUser.email] || false;

  return (
    <ListItem
      alignItems="flex-start"
      sx={{
        padding: '10px 0',
        borderBottom: '1px solid #e0e0e0',
        cursor: 'pointer',
        backgroundColor: unreadMessageCount > 0 ? '#f0f8ff' : 'inherit',
      }}
      onClick={() => handleContactSelect(contactUser.email)}
    >
      <ListItemAvatar onClick={(e) => { e.stopPropagation(); onProfileOpen(contactUser); }}>
        <StyledBadge
          overlap="circular"
          anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
          variant={isOnline ? 'dot' : undefined}
        >
          <Avatar alt={contactUser.username} src={contactUser.profilePictureUrl || defaultProfilePic} />
        </StyledBadge>
      </ListItemAvatar>

      <ListItemText
        primary={
          <Typography variant="subtitle1" component="span">
            {contactUser.username}
          </Typography>
        }
        secondary={
          <Typography component="div" style={{ marginTop: '4px', display: 'flex', alignItems: 'center' }}>
            <Typography component="span" variant="body2" color={unreadMessageCount > 0 ? 'primary' : 'text.secondary'}>
              {lastMessage.length > 40 ? `${lastMessage.substring(0, 40)}...` : lastMessage}
            </Typography>
            {unreadMessageCount > 0 && <Badge badgeContent={unreadMessageCount} color="secondary" sx={{ ml: 2 }} />}
            {unreadOneTimeMessageCount > 0 && (
              <VisibilityOffIcon sx={{ ml: 2, color: '#61082b' }} />
            )}
          </Typography>
        }
      />

      <IconButton edge="end" aria-label="more" onClick={handleMenuOpen}>
        <MoreVertIcon />
      </IconButton>

      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
      >
        <MenuItem onClick={handleRemoveContact}>Delete contact</MenuItem>
        <MenuItem onClick={handleRemoveContact}>Delete conversation</MenuItem>
        <MenuItem onClick={handleBlockContact}>{isBlocked ? 'Unblock Contact' : 'Block Contact'}</MenuItem>
      </Menu>
    </ListItem>
  );
};

export default ContactItem;