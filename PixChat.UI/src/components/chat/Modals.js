import React from 'react';
import { Modal, Box, Typography, Button, TextField, IconButton } from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';

export const SyncModal = ({ showSyncModal, setShowSyncModal, exportData, importData, syncFile, setSyncFile, handleFileChange }) => {
  return (
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
          Data synchronization
        </Typography>
        <Box sx={{ mt: 2, display: 'flex', flexDirection: 'column', gap: 2 }}>
          <Button
            variant="contained"
            color="primary"
            onClick={exportData}
          >
            Export data
          </Button>
          <Box>
            <Typography variant="body1">Import data:</Typography>
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
              onClick={() => importData(syncFile)}
              disabled={!syncFile}
              sx={{ mt: 1 }}
            >
              Import
            </Button>
          </Box>
          <Button
            variant="outlined"
            onClick={() => setShowSyncModal(false)}
          >
            Close
          </Button>
        </Box>
      </Box>
    </Modal>
  );
};