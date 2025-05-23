import React, { useState } from 'react';
import axios from 'axios';
import {
  Container,
  TextField,
  Button,
  Typography,
  Paper,
  Box,
  Alert,
  CircularProgress
} from '@mui/material';

const Register = ({ onRegisterSuccess, setIsRegistering }) => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [username, setUsername] = useState('');
  const [phone, setPhone] = useState('');
  const [verificationCode, setVerificationCode] = useState('');
  const [codeSent, setCodeSent] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleRegister = async () => {
    setError('');
    setLoading(true);
    try {
      const response = await axios.post('http://localhost:5038/api/auth/register', {
        email,
        password,
        username,
        phone,
      });

      setCodeSent(true);
      alert('Verification code has been sent to your email.');
    } catch (error) {
      console.error('Registration error', error);
      setError(error.response?.data?.message || 'Registration error, please check your data.');
    } finally {
      setLoading(false);
    }
  };

  const handleVerifyCode = async () => {
    setError('');
    setLoading(true);
    try {
      const response = await axios.post('http://localhost:5038/api/auth/verify-registration', {
        email,
        code: verificationCode,
      });

      onRegisterSuccess();
      alert('Registration complete! Please sign in.');
    } catch (error) {
      console.error('Verification error', error);
      setError(error.response?.data?.message || 'Error verifying code.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Container component="main" maxWidth="xs" sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: '100vh' }}>
      <Paper elevation={3} sx={{ padding: 3 }}>
        <Box textAlign="center" marginBottom={2}>
          <Typography variant="h5" component="h1" gutterBottom>
            Registration
          </Typography>
        </Box>

        {error && (
          <Alert severity="error" sx={{ marginBottom: 2 }}>
            {error}
          </Alert>
        )}

        {!codeSent ? (
          <>
            <TextField
              variant="outlined"
              margin="normal"
              required
              fullWidth
              label="Username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
            />
            <TextField
              variant="outlined"
              margin="normal"
              required
              fullWidth
              label="Email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
            <TextField
              variant="outlined"
              margin="normal"
              required
              fullWidth
              label="Phone"
              type="tel"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
            />
            <TextField
              variant="outlined"
              margin="normal"
              required
              fullWidth
              label="Password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
            <Button
              variant="contained"
              color="primary"
              fullWidth
              onClick={handleRegister}
              sx={{ marginTop: 2 }}
              disabled={loading}
            >
              {loading ? <CircularProgress size={24} /> : 'Send code'}
            </Button>
          </>
        ) : (
          <>
            <TextField
              variant="outlined"
              margin="normal"
              required
              fullWidth
              label="Enter verification code"
              value={verificationCode}
              onChange={(e) => setVerificationCode(e.target.value)}
            />
            <Button
              variant="contained"
              color="primary"
              fullWidth
              onClick={handleVerifyCode}
              sx={{ marginTop: 2 }}
              disabled={loading}
            >
              {loading ? <CircularProgress size={24} /> : 'Confirm registration'}
            </Button>
          </>
        )}

        <Button 
          variant="outlined" 
          color="primary" 
          onClick={() => setIsRegistering(false)} 
          sx={{ marginTop: 2, width: '100%' }}
        >
          Already have an account? Sign in
        </Button>
      </Paper>
    </Container>
  );
};

export default Register;