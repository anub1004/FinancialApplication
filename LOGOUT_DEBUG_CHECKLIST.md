# Logout 400 Bad Request - Debug Checklist

## **Issue**: You're getting 400 Bad Request on logout, even with `[Authorize]` added.

---

## **Root Cause Diagnosis**

The 400 error means the backend is **rejecting the request before or during validation**. Here's what to check:

---

## **1. Verify React Client is Sending Authorization Header**

Your React frontend **must** send the access token in the Authorization header:

```javascript
// Example: React logout call
const logoutResponse = await fetch('https://localhost:7085/api/auth/logout', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${accessToken}`,  // ← CRITICAL: Must include this
    'credentials': 'include'
  },
  body: JSON.stringify({
    token: refreshToken  // Send refresh token in body
  })
});
```

**Check**: 
- Open browser DevTools → Network tab
- Perform logout
- Click the logout request → Headers tab
- **Look for `Authorization: Bearer ...`**
- If missing → Your React code isn't sending it

---

## **2. Verify Token Format**

When you login, you receive:
```json
{
  "isAuthenticated": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "message": "Login successful"
}
```

**Check**:
- Is the token a long JWT string? ✅
- Or is it null/empty? ❌

---

## **3. Test with Postman/Curl First**

Bypass React to isolate the issue:

```bash
# 1. Login first to get tokens
curl -X POST https://localhost:7085/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@test.com","password":"Password123"}'

# Response will include:
# { "token": "eyJ...", "refreshToken": "eyJ..." }

# 2. Copy the access token (not refresh token)

# 3. Logout using the access token
curl -X POST https://localhost:7085/api/auth/logout \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer eyJ..." \
  -d '{"token":"refreshTokenValueHere"}'
```

**Expected**: 200 OK response with `{ "message": "Logged out successfully" }`

---

## **4. Check Backend Logs**

Enable detailed logging in `Program.cs`:

```csharp
// In Program.cs, JWT bearer events are already logging:
options.Events = new JwtBearerEvents
{
    OnAuthenticationFailed = context =>
    {
        Console.WriteLine($"❌ Authentication failed: {context.Exception.Message}");
        return Task.CompletedTask;
    },
    OnTokenValidated = context =>
    {
        Console.WriteLine($"✅ Token validated successfully");
        return Task.CompletedTask;
    }
};
```

**Check Visual Studio Output window**:
- Build output pane → Show output from: Debug
- When you logout, look for these messages
- If you see "❌ Authentication failed", the JWT is invalid

---

## **5. Common Issues & Solutions**

| Issue | Cause | Solution |
|-------|-------|----------|
| `Authorization: Bearer` header missing | React not sending token | Add header to fetch/axios request |
| `Authorization` header present but 400 | Token expired | Get fresh token by logging in again |
| `Authorization` header present but 400 | Wrong token format | Ensure you're sending **access token**, not refresh token |
| `Authorization` header present but 400 | Token tampered/invalid | Verify JWT key in `appsettings.json` matches |
| `401 Unauthorized` instead of 400 | Endpoint IS receiving token but it's invalid | Check JWT expiration, signature, claims |

---

## **6. Example React Code (Correct Implementation)**

```javascript
// src/services/authService.js
export const logout = async () => {
  try {
    const accessToken = localStorage.getItem('accessToken');
    const refreshToken = localStorage.getItem('refreshToken');
    
    if (!accessToken) {
      throw new Error('No access token found');
    }

    const response = await fetch(
      `${process.env.REACT_APP_API_URL}/api/auth/logout`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${accessToken}`,  // ← KEY LINE
        },
        credentials: 'include',  // Send cookies if using them
        body: JSON.stringify({
          token: refreshToken
        })
      }
    );

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Logout failed');
    }

    const data = await response.json();
    
    // Clear local storage after successful logout
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    
    return data;
  } catch (error) {
    console.error('Logout error:', error);
    throw error;
  }
};
```

---

## **Quick Fix Checklist** ✓

- [ ] React is sending `Authorization: Bearer <accessToken>` header
- [ ] Backend receives the header (check Network tab in DevTools)
- [ ] Token is valid (not expired, correct format)
- [ ] `LogoutRequestDto.Token` contains the refresh token
- [ ] Test with Postman to isolate React vs Backend issue
- [ ] Check backend logs for authentication errors

---

## **If Still Failing After Checklist**

Post the following in your debugging:
1. **Network tab output** from browser (request & response headers)
2. **Backend console output** when logout fails
3. **Your React logout code**
4. **Exact error message** from response body
