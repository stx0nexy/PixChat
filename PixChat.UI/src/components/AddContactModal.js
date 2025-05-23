import React from 'react';
import { Box, TextField, Button, Modal } from '@mui/material';

const AddContactModal = ({ open, onClose, newContactId, setNewContactId, onAddContact }) => (
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
      gap: 2,
    }}>
      <TextField
        label="Enter Contact Email"
        variant="outlined"
        value={newContactId}
        onChange={(e) => setNewContactId(e.target.value)}
      />
      <Button variant="contained" color="primary" onClick={onAddContact}>Add Contact</Button>
      <Button variant="outlined" onClick={onClose}>Cancel</Button>
    </Box>
  </Modal>
);

export default AddContactModal;
