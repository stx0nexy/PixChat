import React, { useState, useEffect } from 'react';
import { Box, Typography, IconButton, Dialog, DialogTitle, DialogContent, DialogActions, Button } from '@mui/material';
import db from '../stores/db';
import CloseIcon from '@mui/icons-material/Close';

const NewOneTimeMessages = ({ onClose, messages, connection, fulloneTimeMessage, updateMessages }) => {
  const [openConfirmDialog, setOpenConfirmDialog] = useState(false);
  const [openMessageDialog, setOpenMessageDialog] = useState(false);
  const [selectedMessage, setSelectedMessage] = useState(null);
  const [message, setMessage] = useState('');
  const [localMessages, setLocalMessages] = useState(messages);

  useEffect(() => {
    setLocalMessages(messages);
  }, [messages]);

  const uniqueMessages = Array.from(new Map(localMessages.map(msg => [msg.messageId, msg])).values());

  const handleRevealMessage = (msg) => {
    setSelectedMessage(msg);
    setMessage(msg.message);
    setMessage(fulloneTimeMessage.message);
    setOpenConfirmDialog(true);
  };

  const confirmRevealMessage = () => {
    if (selectedMessage) {
      readMessage(selectedMessage.messageId);
      setMessage(selectedMessage.message);
    }
    setOpenConfirmDialog(false);
    setOpenMessageDialog(true);
  };

  const handleRemoveMessage = async (messageId) => {
    try {
      await db.oneTimeMessages.where('messageId').equals(messageId).delete();
      console.log(`Message ${messageId} deleted`);
  
      if (typeof updateMessages === 'function') {
        updateMessages();
      }
    } catch (error) {
      console.error('Error deleting message:', error);
    }
  };
  

  const readMessage = async (messageId) => {
    if (connection && connection.state !== 'Connected') {
      console.error('Connection is not in the Connected state.');
      try {
        await connection.start();
        console.log('Reconnected to SignalR.');
      } catch (err) {
        console.error('Failed to reconnect:', err);
        return;
      }
    }

    await connection.invoke("ReadOneTimeMessage", messageId);
    console.log('ONE TIME Message read successfully.');
  };

  return (
    <Box
      sx={{
        position: 'fixed',
        top: 0,
        left: 0,
        width: '100vw',
        height: '100vh',
        backgroundColor: 'rgba(0, 0, 0, 0.5)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 1050,
      }}
    >
      <Box
        sx={{
          width: '66vw',
          height: '66vh',
          backgroundColor: 'white',
          borderRadius: 2,
          boxShadow: 3,
          position: 'relative',
          padding: 3,
          display: 'flex',
          flexDirection: 'column',
        }}
      >
            <IconButton
              onClick={() => {
                onClose();
                updateMessages();
              }}
              sx={{
                position: 'absolute',
                top: 10,
                right: 10,
              }}
            >
              <CloseIcon />
            </IconButton>
            <Box
              sx={{
                flexGrow: 1,
                overflowY: 'auto',
                mt: 2,
                maxHeight: '80%',
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
              }}
            >
              {uniqueMessages.length === 0 ? (
      <Typography variant="body1" sx={{ textAlign: 'center', mt: 4, color: 'gray' }}>
        No secret messages yet
      </Typography>
        ) : (
          uniqueMessages.map((msg) => (
            <Box
              key={msg.messageId}
              sx={{
                mb: 2,
                backgroundColor: '#b4d0e7',
                color: 'black',
                borderRadius: '10px',
                padding: '10px',
                width: '90%',
                wordWrap: 'break-word',
                textAlign: 'center',
                transition: 'background-color 0.3s',
                cursor: 'pointer',
                '&:hover': {
                  backgroundColor: '#61082b',
                  color: 'white',
                },
              }}
              onClick={() => handleRevealMessage(msg)}
            >
              <Typography variant="body1">New secret message</Typography>
              <Typography variant="caption" sx={{ display: 'block', mt: 1 }}>
                {new Date(msg.timestamp).toLocaleString()}
              </Typography>
            </Box>
          ))
        )}  
        </Box>
      </Box>

      <Dialog open={openConfirmDialog} onClose={() => setOpenConfirmDialog(false)}>
        <DialogTitle>Warning</DialogTitle>
        <DialogContent>
          This message will be deleted after viewing. Do you want to continue?
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenConfirmDialog(false)}>Cancel</Button>
          <Button onClick={confirmRevealMessage} color="primary">OK</Button>
        </DialogActions>
      </Dialog>

      <Dialog 
        open={openMessageDialog} 
        onClose={() => {
          setOpenMessageDialog(false);
          if (selectedMessage) {
            handleRemoveMessage(selectedMessage.messageId);
          }
        }}
      >
        <DialogTitle>Secret Message</DialogTitle>
        <DialogContent>
          <Typography>{message} {fulloneTimeMessage.message}</Typography>
        </DialogContent>
        <DialogActions>
          <Button 
            onClick={() => {
              setOpenMessageDialog(false);
              if (selectedMessage) {
                handleRemoveMessage(selectedMessage.messageId);
              }
            }} 
            color="primary"
          >
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default NewOneTimeMessages;
