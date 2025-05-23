import React, { useState } from 'react';
import CloseIcon from '@mui/icons-material/Close';
import { Box, Typography, List, ListItem, ListItemText, IconButton, Modal } from '@mui/material';
import CheckIcon from '@mui/icons-material/Check';

const FriendRequests = ({ connection, onClose, user, fetchContacts }) => {

  const [friendRequests, setFriendRequests] = useState([]);

  const acceptFriendRequest = async (requesterId) => {
    if (!connection) {
      console.error('No SignalR connection.');
      return;
    }
  
    if (connection.state !== 'Connected') {
      console.error('Connection is not in the Connected state.');
      try {
        await connection.start();
        console.log('Reconnected to SignalR.');
      } catch (err) {
        console.error('Failed to reconnect:', err);
        return;
      }
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
  
    if (connection.state !== 'Connected') {
      console.error('Connection is not in the Connected state.');
      try {
        await connection.start();
        console.log('Reconnected to SignalR.');
      } catch (err) {
        console.error('Failed to reconnect:', err);
        return;
      }
    }
    try {
      await connection.invoke("RejectFriendRequest", user.email, requesterId);
      
      setFriendRequests(prev => prev.filter(id => id !== requesterId));
    } catch (error) {
      console.error('Error rejecting friend request:', error);
    }
  };
  
    return (
        <Modal open={acceptFriendRequest} onClose={onClose}>
        <Box
          sx={{
            position: 'absolute',
            top: '50%',
            left: '50%',
            transform: 'translate(-50%, -50%)',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            padding: 2,
            backgroundColor: 'white',
            borderRadius: 2,
            boxShadow: 3,
            width: '100%',
            maxWidth: 400,
          }}
        >
          <Typography variant="h6">Friend Requests</Typography>
          <IconButton onClick={onClose} sx={{ position: 'absolute', top: 10, right: 10 }}>
            <CloseIcon />
          </IconButton>
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
        </Box>
      </Modal>
    );
  };
  
  export default FriendRequests;