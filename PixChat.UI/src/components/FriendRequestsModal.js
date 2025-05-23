import React from 'react';
import { Box, Typography, List, ListItem, ListItemText, IconButton } from '@mui/material';
import CheckIcon from '@mui/icons-material/Check';
import CloseIcon from '@mui/icons-material/Close';

const FriendRequestsModal = ({ open, onClose, friendRequests, acceptFriendRequest, rejectFriendRequest }) => (
  <Modal open={open} onClose={onClose}>
    <Box sx={{
      position: 'absolute',
      top: '50%',
      left: '50%',
      transform: 'translate(-50%, -50%)',
      backgroundColor: 'white',
      padding: 2,
      boxShadow: 24,
      minWidth: 300,
      display: 'flex',
      flexDirection: 'column',
    }}>
      <Typography variant="h6">Friend Requests</Typography>
      <List>
        {friendRequests.map((requesterId) => (
          <ListItem key={requesterId}>
            <ListItemText primary={requesterId} />
            <Box sx={{ display: 'flex', gap: 1 }}>
              <IconButton color="primary" onClick={() => acceptFriendRequest(requesterId)}>
                <CheckIcon />
              </IconButton>
              <IconButton color="secondary" onClick={() => rejectFriendRequest(requesterId)}>
                <CloseIcon />
              </IconButton>
            </Box>
          </ListItem>
        ))}
      </List>
      <Button variant="outlined" onClick={onClose}>Close</Button>
    </Box>
  </Modal>
);

export default FriendRequestsModal;
