import React, { useState } from 'react';
import { Box, TextField, Button, IconButton, Popover } from '@mui/material';
import TimesOneMobiledataIcon from '@mui/icons-material/TimesOneMobiledata';
import EmojiEmotionsIcon from '@mui/icons-material/EmojiEmotions';
import AttachFileIcon from '@mui/icons-material/AttachFile'; // Иконка для прикрепления файла
import EmojiPicker from 'emoji-picker-react';

export const MessageInput = ({ message, setMessage, onSend, messageStatus, isGroupChat, onSendFile }) => {
  const [isOneTime, setIsOneTime] = useState(false);
  const [anchorEl, setAnchorEl] = useState(null);
  const [selectedFile, setSelectedFile] = useState(null); // Состояние для выбранного файла

  const handleKeyDown = (e) => {
    if (e.key === 'Enter' && !e.shiftKey && message.trim()) {
      e.preventDefault();
      handleSend();
    }
  };

  const handleSend = () => {
    if (message.trim()) {
      messageStatus(isOneTime);
      onSend();
      setIsOneTime(false);
      setMessage('');
    }
  };

  const handleFileChange = (event) => {
    const file = event.target.files[0];
    if (file) {
      setSelectedFile(file);
      handleSendFile(file); // Отправляем файл сразу после выбора
    }
  };

  const handleSendFile = (file) => {
    if (file) {
      onSendFile(file); // Вызываем функцию отправки файла
      setSelectedFile(null); // Сбрасываем выбранный файл
    }
  };

  const handleEmojiClick = (emojiObject) => {
    setMessage((prev) => prev + emojiObject.emoji);
    setAnchorEl(null);
  };

  const handleOpenEmojiPicker = (event) => {
    setAnchorEl(event.currentTarget);
  };

  const handleCloseEmojiPicker = () => {
    setAnchorEl(null);
  };

  return (
    <Box
      sx={{
        width: '100%',
        display: 'flex',
        backgroundColor: 'white',
        padding: 1,
        boxShadow: 3,
        zIndex: 1000,
        alignItems: 'center',
        height: '60px',
        flexShrink: 0,
      }}
    >
      <TextField
        variant="outlined"
        placeholder="Enter your message"
        value={message}
        onChange={(e) => setMessage(e.target.value)}
        onKeyDown={handleKeyDown}
        fullWidth
        multiline
        maxRows={4}
        sx={{
          maxHeight: '60px',
          flexGrow: 1,
        }}
      />

      <IconButton
        onClick={handleOpenEmojiPicker}
        sx={{ ml: 1 }}
      >
        <EmojiEmotionsIcon />
      </IconButton>

      <IconButton component="label" sx={{ ml: 1 }}>
        <AttachFileIcon />
        <input
          type="file"
          hidden
          onChange={handleFileChange}
        />
      </IconButton>

      {!isGroupChat && (
        <IconButton 
          onClick={() => setIsOneTime(!isOneTime)} 
          sx={{ color: isOneTime ? 'red' : 'gray', ml: 1 }}
        >
          <TimesOneMobiledataIcon />
        </IconButton>
      )}

      <Button
        variant="contained"
        color="primary"
        onClick={handleSend}
        disabled={!message.trim()}
        sx={{
          ml: 1,
          height: '100%',
          backgroundColor: !message.trim() ? 'gray' : undefined,
          '&:hover': { backgroundColor: !message.trim() ? 'gray' : undefined },
        }}
      >
        Send
      </Button>

      <Popover
        open={Boolean(anchorEl)}
        anchorEl={anchorEl}
        onClose={handleCloseEmojiPicker}
        anchorOrigin={{
          vertical: 'top',
          horizontal: 'left',
        }}
        transformOrigin={{
          vertical: 'bottom',
          horizontal: 'left',
        }}
      >
        <EmojiPicker onEmojiClick={handleEmojiClick} />
      </Popover>
    </Box>
  );
};