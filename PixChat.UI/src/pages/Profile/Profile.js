import React, { useState, useEffect } from 'react';
import { Box, Avatar, Typography, TextField, Button, IconButton } from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import axios from 'axios';
import { useNavigate } from 'react-router-dom';

const Profile = ({ user, token }) => {
  const [isEditing, setIsEditing] = useState(false);
  const [profileData, setProfileData] = useState({
    username: '',
    email: '',
    phone: '',
    profilePicture: '',
    passwordHash: '',
  });

  const [updatedProfileData, setUpdatedProfileData] = useState(profileData);
  const [selectedImage, setSelectedImage] = useState(null);

  const navigate = useNavigate();

  const fetchUserData = async () => {
    try {
      const response = await axios.get(`http://localhost:5038/api/users/${user.id}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      const userData = response.data;
      setProfileData({
        username: userData.username,
        email: userData.email,
        phone: userData.phone,
        profilePicture: userData.profilePictureUrl,
        passwordHash: userData.passwordHash,
      });
      setUpdatedProfileData({
        username: userData.username,
        email: userData.email,
        phone: userData.phone,
        profilePicture: userData.profilePictureUrl,
      });
    } catch (error) {
      console.error('Error loading profile data:', error.response?.data || error.message);
    }
  };

  useEffect(() => {
    fetchUserData();
  }, []);

  const handleEditClick = () => {
    setIsEditing(true);
  };

  const handleSaveClick = async () => {
    try {
      const updatedData = {
        id: user.id,
        username: updatedProfileData.username,
        email: updatedProfileData.email,
        phone: updatedProfileData.phone,
        profilePictureUrl: updatedProfileData.profilePicture,
        passwordHash: profileData.passwordHash,
      };

      await axios.put(`http://localhost:5038/api/users/${user.id}`, updatedData, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (selectedImage) {
        const formData = new FormData();
        formData.append('image', selectedImage);
        
        await axios.post(`http://localhost:5038/api/users/${user.id}/uploadPhoto`, formData, {
          headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'multipart/form-data' },
        });
      }

      setProfileData(updatedData);
      setIsEditing(false);
    } catch (error) {
      console.error('Error updating profile:', error.response?.data || error.message);
    }
  };

  const handleCancelClick = () => {
    setUpdatedProfileData(profileData);
    setIsEditing(false);
  };

  const handleImageChange = (e) => {
    const file = e.target.files[0];
    setSelectedImage(file);
    const reader = new FileReader();
    reader.onloadend = () => {
      setUpdatedProfileData({ ...updatedProfileData, profilePicture: reader.result });
    };
    reader.readAsDataURL(file);
  };

  const handleGoBack = () => {
    navigate('/chat');
  };

  return (
    <Box sx={{ position: 'relative', display: 'flex', flexDirection: 'column', alignItems: 'center', padding: 2 }}>
      
      <IconButton
        onClick={handleGoBack}
        sx={{ position: 'absolute', top: 10, right: 10 }}
      >
        <CloseIcon />
      </IconButton>

      <Avatar src={updatedProfileData.profilePicture} alt={updatedProfileData.username} sx={{ width: 80, height: 80 }} />
      
      {isEditing ? (
        <>
          <TextField
            label="Username"
            value={updatedProfileData.username}
            onChange={(e) => setUpdatedProfileData({ ...updatedProfileData, username: e.target.value })}
            margin="normal"
          />
          <TextField
            label="Email"
            value={updatedProfileData.email}
            onChange={(e) => setUpdatedProfileData({ ...updatedProfileData, email: e.target.value })}
            margin="normal"
          />
          <TextField
            label="Phone"
            value={updatedProfileData.phone}
            onChange={(e) => setUpdatedProfileData({ ...updatedProfileData, phone: e.target.value })}
            margin="normal"
          />

          <input type="file" onChange={handleImageChange} accept="image/*" style={{ marginTop: 10 }} />
          
          <Box sx={{ display: 'flex', gap: 2, marginTop: 2 }}>
            <Button variant="contained" color="primary" onClick={handleSaveClick}>
              Save
            </Button>
            <Button variant="outlined" onClick={handleCancelClick}>
              Cancel
            </Button>
          </Box>
        </>
      ) : (
        <>
          <Typography variant="h6">{profileData.username}</Typography>
          <Typography variant="body1">{profileData.email}</Typography>
          <Typography variant="body1">{profileData.phone}</Typography>
          <Button variant="contained" color="primary" sx={{ marginTop: 2 }} onClick={handleEditClick}>
            Edit
          </Button>
        </>
      )}
    </Box>
  );
};

export default Profile;
