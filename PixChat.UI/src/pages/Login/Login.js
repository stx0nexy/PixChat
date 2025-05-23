import React, { useState } from 'react';
import { TextField, Button, Box } from '@mui/material';
import axios from 'axios';

const Login = ({ onAuthSuccess, setIsRegistering }) => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const handleSubmit = async (event) => {
    event.preventDefault();
    
    try {
      const response = await axios.post('http://localhost:5038/api/auth/login', {
        email,
        password,
      });

      const { user, token } = response.data;
      onAuthSuccess(user, token);
    } catch (error) {
      console.error('Authentication error:', error);
    }
  };

  return (
    <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
      <TextField
        label="Email"
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        required
        fullWidth
        sx={{ mb: 2 }}
      />
      <TextField
        label="Password"
        type="password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        required
        fullWidth
        sx={{ mb: 2 }}
      />
      <Button type="submit" variant="contained" color="primary">
        Login
      </Button>

      <Button 
        variant="outlined" 
        color="primary" 
        onClick={() => setIsRegistering(true)}
        sx={{ marginTop: 2 }}
      >
        No account? Register
      </Button>
    </Box>
  );
};

export default Login;
