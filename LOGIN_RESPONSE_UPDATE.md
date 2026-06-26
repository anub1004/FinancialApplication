# Updated Login Response Format

## Backend Now Returns

```json
{
  "isAuthenticated": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 900,
  "role": "User",
  "message": "Login successful"
}
```

## React Frontend Update

Store both tokens after login:

```javascript
// src/services/authService.js

export const login = async (email, password) => {
  try {
    const response = await fetch(
      `${process.env.REACT_APP_API_URL}/api/auth/login`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include',  // Include cookies
        body: JSON.stringify({ email, password })
      }
    );

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Login failed');
    }

    const data = await response.json();
    
    // Store tokens in localStorage
    localStorage.setItem('accessToken', data.accessToken);
    localStorage.setItem('refreshToken', data.refreshToken);
    localStorage.setItem('role', data.role);
    
    // Optional: Store expiration time
    localStorage.setItem('expiresIn', data.expiresIn);
    
    return data;
  } catch (error) {
    console.error('Login error:', error);
    throw error;
  }
};

export const logout = async () => {
  try {
    const accessToken = localStorage.getItem('accessToken');
    const refreshToken = localStorage.getItem('refreshToken');
    
    if (!accessToken || !refreshToken) {
      throw new Error('Tokens not found');
    }

    const response = await fetch(
      `${process.env.REACT_APP_API_URL}/api/auth/logout`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${accessToken}`,  // Send access token in header
        },
        credentials: 'include',
        body: JSON.stringify({
          token: refreshToken  // Send refresh token in body
        })
      }
    );

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Logout failed');
    }

    // Clear localStorage after successful logout
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('role');
    localStorage.removeItem('expiresIn');
    
    return await response.json();
  } catch (error) {
    console.error('Logout error:', error);
    throw error;
  }
};
```

## Token Usage

**For API calls that require authentication:**

```javascript
// When calling protected endpoints
const response = await fetch(
  `${process.env.REACT_APP_API_URL}/api/some-protected-endpoint`,
  {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
    },
    credentials: 'include'
  }
);
```

## Token Refresh Flow (Optional)

When access token expires, use refresh token to get a new one:

```javascript
export const refreshAccessToken = async () => {
  try {
    const refreshToken = localStorage.getItem('refreshToken');
    
    if (!refreshToken) {
      throw new Error('No refresh token found');
    }

    const response = await fetch(
      `${process.env.REACT_APP_API_URL}/api/auth/refresh`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include',
        body: JSON.stringify({ refreshToken })
      }
    );

    if (!response.ok) {
      throw new Error('Token refresh failed');
    }

    const data = await response.json();
    
    // Update access token
    localStorage.setItem('accessToken', data.accessToken);
    
    return data;
  } catch (error) {
    console.error('Token refresh error:', error);
    // Redirect to login if refresh fails
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    window.location.href = '/login';
    throw error;
  }
};
```

## Summary of Changes

| Before | After |
|--------|-------|
| Only `accessToken` in response | Both `accessToken` and `refreshToken` |
| Field named `token` | Field named `accessToken` |
| No refresh token to frontend | Refresh token now available for logout/refresh |
| Logout had no refresh token | Logout sends refresh token in body |
