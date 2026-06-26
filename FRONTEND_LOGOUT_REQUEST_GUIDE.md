# Frontend Logout Request Guide

## How Frontend Should Send Logout Requests

This guide explains exactly how your React frontend should send requests to the logout endpoint with proper headers, credentials, and error handling.

---

## Basic Logout Request

### **Simple Example**

```javascript
const handleLogout = async () => {
    try {
        const response = await fetch('https://localhost:7085/api/Auth/logout', {
            method: 'POST',
            credentials: 'include',  // ✅ CRITICAL: Sends authToken cookie
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`  // ✅ Current access token
            },
            body: JSON.stringify({
                token: refreshToken  // ✅ Refresh token to invalidate
            })
        });

        const data = await response.json();
        
        if (response.ok) {
            console.log('Logout successful:', data.message);
            // Clear local storage
            localStorage.removeItem('token');
            localStorage.removeItem('refreshToken');
            // Redirect to login
            window.location.href = '/login';
        } else {
            console.error('Logout failed:', data.message);
        }
    } catch (error) {
        console.error('Logout error:', error);
    }
};
```

---

## Complete Logout Implementation with State Management

### **Using React Context**

```javascript
// AuthContext.js
import React, { createContext, useState, useCallback } from 'react';

export const AuthContext = createContext();

export const AuthProvider = ({ children }) => {
    const [authState, setAuthState] = useState({
        isAuthenticated: false,
        user: null,
        role: null,
        accessToken: null,
        refreshToken: null,
        loading: true
    });

    const logout = useCallback(async () => {
        try {
            // Step 1: Prepare logout request
            const response = await fetch('https://localhost:7085/api/Auth/logout', {
                method: 'POST',
                credentials: 'include',  // ✅ Send cookies
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${authState.accessToken}`
                },
                body: JSON.stringify({
                    token: authState.refreshToken
                })
            });

            // Step 2: Parse response
            const data = await response.json();

            // Step 3: Handle response
            if (response.ok) {
                // ✅ Logout successful
                
                // Step 4: Clear local storage
                localStorage.removeItem('token');
                localStorage.removeItem('refreshToken');
                localStorage.removeItem('user');
                
                // Step 5: Clear auth state
                setAuthState({
                    isAuthenticated: false,
                    user: null,
                    role: null,
                    accessToken: null,
                    refreshToken: null,
                    loading: false
                });
                
                // Step 6: Redirect to login
                window.location.href = '/login';
            } else {
                // ❌ Logout failed but still clear state
                console.error('Logout failed:', data.message);
                localStorage.clear();
                setAuthState({
                    isAuthenticated: false,
                    user: null,
                    role: null,
                    accessToken: null,
                    refreshToken: null,
                    loading: false
                });
                window.location.href = '/login';
            }
        } catch (error) {
            // ❌ Network error
            console.error('Logout error:', error);
            // Still clear everything on error
            localStorage.clear();
            setAuthState({
                isAuthenticated: false,
                user: null,
                role: null,
                accessToken: null,
                refreshToken: null,
                loading: false
            });
            window.location.href = '/login';
        }
    }, [authState.accessToken, authState.refreshToken]);

    return (
        <AuthContext.Provider value={{ authState, logout }}>
            {children}
        </AuthContext.Provider>
    );
};
```

### **Using the Context in Components**

```javascript
// Dashboard.jsx
import React, { useContext } from 'react';
import { AuthContext } from './AuthContext';

export const Dashboard = () => {
    const { authState, logout } = useContext(AuthContext);

    const handleLogoutClick = async () => {
        if (window.confirm('Are you sure you want to logout?')) {
            await logout();
        }
    };

    return (
        <div>
            <h1>Welcome, {authState.user}</h1>
            <button onClick={handleLogoutClick}>Logout</button>
        </div>
    );
};
```

---

## Axios Example

### **Using Axios with Interceptors**

```javascript
// axiosConfig.js
import axios from 'axios';

export const createAxiosInstance = (token) => {
    return axios.create({
        baseURL: 'https://localhost:7085/api',
        withCredentials: true,  // ✅ Send cookies
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`  // ✅ Include token
        }
    });
};

// Usage in logout
const handleLogout = async (accessToken, refreshToken) => {
    try {
        const axiosInstance = createAxiosInstance(accessToken);
        
        const response = await axiosInstance.post('/Auth/logout', {
            token: refreshToken
        });

        if (response.status === 200) {
            // Clear local storage
            localStorage.removeItem('token');
            localStorage.removeItem('refreshToken');
            
            // Redirect
            window.location.href = '/login';
        }
    } catch (error) {
        console.error('Logout failed:', error);
        localStorage.clear();
        window.location.href = '/login';
    }
};
```

---

## Request Format Reference

### **HTTP Request**

```http
POST /api/Auth/logout HTTP/1.1
Host: localhost:7085
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Cookie: authToken=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

{
  "token": "refresh_token_value_here_aeioudnoui...=="
}
```

### **Required Headers**

| Header | Value | Purpose |
|--------|-------|---------|
| `Content-Type` | `application/json` | Tells server request body is JSON |
| `Authorization` | `Bearer {accessToken}` | ✅ **Required** - Current access token for authentication |
| `Cookie` | `authToken={token}` | ✅ Automatic - Browser sends automatically with `credentials: 'include'` |

### **Request Body**

```json
{
  "token": "your_refresh_token_here"
}
```

**Required Fields:**
- `token` (string) - The refresh token to invalidate

---

## Response Format Reference

### **Success Response (200 OK)**

```json
{
  "message": "Logged out successfully",
  "isAuthenticated": false
}
```

### **Error Responses**

**No Authorization Header (401)**
```json
{
  "detail": "Authorization token was not provided"
}
```

**Invalid Token (401)**
```json
{
  "detail": "Invalid token"
}
```

**Invalid Refresh Token (401)**
```json
{
  "message": "Logout failed. Invalid or expired refresh token."
}
```

**Missing Refresh Token (400)**
```json
{
  "message": "Refresh token is required."
}
```

**General Error (400)**
```json
{
  "message": "Logout failed: Exception details"
}
```

---

## Complete React Component Example

### **Logout Button with All Best Practices**

```javascript
// LogoutButton.jsx
import React, { useState, useContext } from 'react';
import { AuthContext } from './AuthContext';
import './LogoutButton.css';

export const LogoutButton = () => {
    const { authState, logout } = useContext(AuthContext);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState(null);

    const handleLogout = async () => {
        // Step 1: Confirm logout
        const confirmed = window.confirm(
            'Are you sure you want to logout? You will need to login again.'
        );
        if (!confirmed) return;

        try {
            setIsLoading(true);
            setError(null);

            // Step 2: Make logout request
            const response = await fetch('https://localhost:7085/api/Auth/logout', {
                method: 'POST',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${authState.accessToken}`
                },
                body: JSON.stringify({
                    token: authState.refreshToken
                })
            });

            const data = await response.json();

            // Step 3: Handle response
            if (response.ok) {
                // ✅ Success
                console.log('✅ Logout successful');
                
                // Clear storage
                localStorage.removeItem('token');
                localStorage.removeItem('refreshToken');
                localStorage.removeItem('user');
                
                // Update context
                await logout();
                
            } else {
                // ❌ Server error but still logout frontend
                console.warn('⚠️ Server logout failed:', data.message);
                
                // Still clear frontend state
                localStorage.clear();
                await logout();
            }
        } catch (error) {
            // ❌ Network error
            console.error('❌ Logout error:', error);
            setError('Failed to logout. Please try again.');
            
            // Still clear frontend state after error
            setTimeout(() => {
                localStorage.clear();
                window.location.href = '/login';
            }, 2000);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="logout-button-container">
            <button
                onClick={handleLogout}
                disabled={isLoading}
                className="logout-button"
            >
                {isLoading ? 'Logging out...' : 'Logout'}
            </button>
            {error && <div className="error-message">{error}</div>}
        </div>
    );
};
```

---

## Error Handling Guide

### **Handling Different Error Scenarios**

```javascript
const handleLogout = async () => {
    try {
        const response = await fetch('https://localhost:7085/api/Auth/logout', {
            method: 'POST',
            credentials: 'include',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`
            },
            body: JSON.stringify({ token: refreshToken })
        });

        const data = await response.json();

        // Scenario 1: Success
        if (response.status === 200) {
            console.log('✅ Logout successful');
            localStorage.clear();
            navigate('/login');
        }

        // Scenario 2: No Authorization Header
        else if (response.status === 401 && data.detail?.includes('token was not provided')) {
            console.warn('⚠️ No token found. Redirecting to login.');
            navigate('/login');
        }

        // Scenario 3: Invalid/Expired Token
        else if (response.status === 401 && data.detail?.includes('Invalid token')) {
            console.warn('⚠️ Token expired. Redirecting to login.');
            localStorage.clear();
            navigate('/login');
        }

        // Scenario 4: Invalid Refresh Token
        else if (response.status === 401 && data.message?.includes('Invalid or expired refresh token')) {
            console.warn('⚠️ Refresh token invalid. Clearing state.');
            localStorage.clear();
            navigate('/login');
        }

        // Scenario 5: Missing Refresh Token Parameter
        else if (response.status === 400 && data.message?.includes('required')) {
            console.error('❌ Missing refresh token in request body');
            // Still logout
            localStorage.clear();
            navigate('/login');
        }

        // Scenario 6: Other server errors
        else {
            console.error('❌ Logout failed:', data.message);
            // Force logout anyway
            localStorage.clear();
            navigate('/login');
        }

    } catch (error) {
        // Network error
        console.error('❌ Network error during logout:', error);
        
        // Force logout on network error
        localStorage.clear();
        navigate('/login');
    }
};
```

---

## TypeScript Example

### **Typed Logout Request**

```typescript
// types.ts
export interface LogoutRequest {
    token: string;  // Refresh token
}

export interface LogoutResponse {
    message: string;
    isAuthenticated: boolean;
}

export interface ErrorResponse {
    message?: string;
    detail?: string;
}

// auth.service.ts
export class AuthService {
    private readonly API_URL = 'https://localhost:7085/api/Auth';

    async logout(
        accessToken: string,
        refreshToken: string
    ): Promise<LogoutResponse> {
        const response = await fetch(`${this.API_URL}/logout`, {
            method: 'POST',
            credentials: 'include',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${accessToken}`
            },
            body: JSON.stringify({
                token: refreshToken
            } as LogoutRequest)
        });

        const data = await response.json() as LogoutResponse | ErrorResponse;

        if (!response.ok) {
            throw new Error(
                (data as ErrorResponse).message || 
                'Logout failed'
            );
        }

        return data as LogoutResponse;
    }
}

// Usage
const authService = new AuthService();

try {
    const result = await authService.logout(accessToken, refreshToken);
    console.log(result.message);
    // Redirect to login
} catch (error) {
    console.error(error.message);
}
```

---

## Common Mistakes to Avoid

### ❌ **Wrong: Missing credentials**
```javascript
// ❌ WRONG - Cookie won't be sent
const response = await fetch('/api/Auth/logout', {
    method: 'POST',
    body: JSON.stringify({ token: refreshToken })
});
```

**Fix**: Add `credentials: 'include'`
```javascript
// ✅ CORRECT
const response = await fetch('/api/Auth/logout', {
    method: 'POST',
    credentials: 'include',  // ✅ Send cookies
    body: JSON.stringify({ token: refreshToken })
});
```

---

### ❌ **Wrong: Missing Authorization header**
```javascript
// ❌ WRONG - Will get 401 Unauthorized
const response = await fetch('/api/Auth/logout', {
    method: 'POST',
    credentials: 'include',
    body: JSON.stringify({ token: refreshToken })
});
```

**Fix**: Add Authorization header
```javascript
// ✅ CORRECT
const response = await fetch('/api/Auth/logout', {
    method: 'POST',
    credentials: 'include',
    headers: {
        'Authorization': `Bearer ${accessToken}`  // ✅ Required
    },
    body: JSON.stringify({ token: refreshToken })
});
```

---

### ❌ **Wrong: Forgetting to clear local storage**
```javascript
// ❌ WRONG - User still has tokens in storage
if (response.ok) {
    navigate('/login');
}
```

**Fix**: Clear storage before redirecting
```javascript
// ✅ CORRECT
if (response.ok) {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    navigate('/login');
}
```

---

### ❌ **Wrong: Not handling errors**
```javascript
// ❌ WRONG - If logout fails, user stays logged in
const response = await fetch('/api/Auth/logout', { ... });
navigate('/login');
```

**Fix**: Handle errors gracefully
```javascript
// ✅ CORRECT
try {
    const response = await fetch('/api/Auth/logout', { ... });
    if (response.ok) {
        localStorage.clear();
    } else {
        // Still logout even if server error
        localStorage.clear();
    }
} catch (error) {
    // Still logout even if network error
    localStorage.clear();
} finally {
    navigate('/login');
}
```

---

## Environment Configuration

### **Using Environment Variables**

```javascript
// .env
REACT_APP_API_URL=https://localhost:7085

// .env.production
REACT_APP_API_URL=https://api.production.com
```

```javascript
// Use in code
const API_URL = process.env.REACT_APP_API_URL;

const handleLogout = async () => {
    const response = await fetch(`${API_URL}/api/Auth/logout`, {
        method: 'POST',
        credentials: 'include',
        headers: {
            'Authorization': `Bearer ${accessToken}`
        },
        body: JSON.stringify({ token: refreshToken })
    });
    // ...
};
```

---

## Testing the Logout Request

### **Using Thunder Client / Postman**

**Method**: POST

**URL**: 
```
https://localhost:7085/api/Auth/logout
```

**Headers**:
```
Content-Type: application/json
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Cookies**:
```
authToken=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Body**:
```json
{
  "token": "your_refresh_token_here"
}
```

**Expected Response (200 OK)**:
```json
{
  "message": "Logged out successfully",
  "isAuthenticated": false
}
```

---

## Flow Diagram

```
User clicks "Logout" button
    ↓
[Confirm Dialog] "Are you sure?"
    ↓
Frontend Sends POST /api/Auth/logout
├─ Header: Authorization: Bearer {accessToken}
├─ Cookie: authToken={cookie}
└─ Body: { token: refreshToken }
    ↓
Backend Validates [Authorize]
    ↓
Backend Extracts userId from JWT
    ↓
Backend Deletes Refresh Token from Database
    ↓
Backend Deletes authToken Cookie
    ↓
Backend Returns 200 OK { message: "Logged out successfully" }
    ↓
Frontend Receives Success (200)
    ↓
Frontend Clears localStorage
├─ Removes 'token'
├─ Removes 'refreshToken'
└─ Removes 'user'
    ↓
Frontend Updates Auth State (isAuthenticated = false)
    ↓
Frontend Redirects to /login
    ↓
✅ Complete Session Invalidation
```

---

## Summary

**Frontend should always send:**

| Item | Example | Required |
|------|---------|----------|
| **Method** | `POST` | ✅ Yes |
| **URL** | `https://localhost:7085/api/Auth/logout` | ✅ Yes |
| **Credentials** | `include` | ✅ Yes (sends cookie) |
| **Authorization Header** | `Bearer {accessToken}` | ✅ Yes |
| **Content-Type Header** | `application/json` | ✅ Yes |
| **Body** | `{ "token": "{refreshToken}" }` | ✅ Yes |

**Frontend should always do after:**

1. ✅ Clear localStorage
2. ✅ Clear auth state
3. ✅ Redirect to login
4. ✅ Handle errors gracefully (still logout)
