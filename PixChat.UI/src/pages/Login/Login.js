import React, { useState } from 'react';
import { TextField, Button, Box, Alert } from '@mui/material';
import axios from 'axios';

const Login = ({ onAuthSuccess, setIsRegistering }) => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
   const [errors, setErrors] = useState({});
  const [generalError, setGeneralError] = useState('');


  const handleSubmit = async (event) => {
    event.preventDefault();
    setErrors({});
    setGeneralError('');
    
    try {
      const response = await axios.post('http://localhost:5038/api/auth/login', {
        email,
        password,
      });

      const { user, token } = response.data;
      onAuthSuccess(user, token);
    } catch (error) {
      console.error('Authentication error:', error);
      if (error.response && error.response.data) {
        if (error.response.data.errors) {
          setErrors(error.response.data.errors);
        } else if (error.response.data.message) {
          setGeneralError(error.response.data.message);
        } else {
          setGeneralError('An unexpected error occurred.');
        }
      } else {
        setGeneralError('Network error or server unreachable.');
      }
    }
  };

  return (
    <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
      {generalError && (
        <Alert severity="error" sx={{ marginBottom: 2, width: '100%' }}>
          {generalError}
        </Alert>
      )}
      <TextField
        label="Email"
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        required
        fullWidth
        sx={{ mb: 2 }}
        error={!!errors.Email}
        helperText={errors.Email ? errors.Email[0] : ''}
      />
      <TextField
        label="Password"
        type="password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        required
        fullWidth
        sx={{ mb: 2 }}
        error={!!errors.Password}
        helperText={errors.Password ? errors.Password[0] : ''}
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
