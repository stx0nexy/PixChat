import React, { useState, useEffect } from 'react';
import { Button, TextField, List, ListItem, Checkbox, FormControlLabel, Box, IconButton } from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';

const CreateChatForm = ({ user, contacts, onClose, token }) => {
  const [chatName, setChatName] = useState('');
  const [chatDescription, setChatDescription] = useState('');
  const [selectedParticipants, setSelectedParticipants] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [contactNames, setContactNames] = useState({});
  const [contactsLoaded, setContactsLoaded] = useState(false);

  useEffect(() => {
    const fetchContactNames = async () => {
      try {
        const names = await Promise.all(
          contacts.map(async (contact) => {
            const response = await fetch(`http://localhost:5038/api/Users/${contact.contactUserId}`, {
              headers: {
                Authorization: `Bearer ${token}`,
              },
            });

            if (response.ok) {
              const userData = await response.json();
              return { userId: contact.contactUserId, name: userData.username };
            } else {
              console.error(`Failed to fetch user ${contact.contactUserId}`);
              return { userId: contact.contactUserId, name: 'Unknown' };
            }
          })
        );

        const namesMap = names.reduce((acc, { userId, name }) => {
          acc[userId] = name;
          return acc;
        }, {});

        setContactNames(namesMap);
        setContactsLoaded(true);
      } catch (error) {
        console.error('Error fetching contacts:', error);
      }
    };

    fetchContactNames();
  }, [contacts, token]);

  const handleCreate = async () => {
    const chatData = {
      Name: chatName,
      Description: chatDescription,
      IsGroup: true,
      CreatorId: user.id,
      ParticipantIds: selectedParticipants,
    };

    try {
      setIsLoading(true);
      const response = await fetch(`http://localhost:5038/api/Chat`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(chatData),
      });

      if (!response.ok) {
        throw new Error('Failed to create chat');
      }

      setChatName('');
      setChatDescription('');
      setSelectedParticipants([]);

      onClose();
    } catch (error) {
      console.error('Failed to create chat:', error);
      alert('Error occurred while creating chat. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Box
      sx={{
        position: 'relative',
        p: 3,
        pt: 6,
        bgcolor: 'background.paper',
        borderRadius: 2,
        boxShadow: 3,
        display: 'flex',
        flexDirection: 'column',
      }}
    >
      <IconButton
        sx={{
          position: 'absolute',
          top: 8,
          right: 8,
          zIndex: 10,
        }}
        onClick={onClose}
      >
        <CloseIcon />
      </IconButton>

      <TextField
        label="Chat Name"
        value={chatName}
        onChange={e => setChatName(e.target.value)}
        fullWidth
        margin="normal"
      />
      <TextField
        label="Description"
        value={chatDescription}
        onChange={e => setChatDescription(e.target.value)}
        fullWidth
        margin="normal"
      />
      <List>
        {contacts.map(contact => (
          <ListItem key={contact.contactUserId}>
            <FormControlLabel
              control={
                <Checkbox
                  checked={selectedParticipants.includes(contact.contactUserId)}
                  onChange={() => {
                    if (selectedParticipants.includes(contact.contactUserId)) {
                      setSelectedParticipants(selectedParticipants.filter(id => id !== contact.contactUserId));
                    } else {
                      setSelectedParticipants([...selectedParticipants, contact.contactUserId]);
                    }
                  }}
                />
              }
              label={contactsLoaded ? contactNames[contact.contactUserId] || 'Unknown' : 'Loading...'}
            />
          </ListItem>
        ))}
      </List>
      <Button
        variant="contained"
        color="primary"
        onClick={handleCreate}
        disabled={isLoading || !chatName.trim()}
        sx={{ mt: 2 }}
      >
        {isLoading ? 'Creating...' : 'Create Chat'}
      </Button>
      <Button variant="outlined" onClick={onClose} sx={{ mt: 1, ml: 1 }}>
        Cancel
      </Button>
    </Box>
  );
};

export default CreateChatForm;
