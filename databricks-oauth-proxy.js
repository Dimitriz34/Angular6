/**
 * OAuth M2M Token Generator
 * Configure your credentials below and run: node databricks-oauth-proxy.js
 */

// ═══════════════════════════════════════════════════════════
// CONFIGURATION - UPDATE THESE VALUES
// ═══════════════════════════════════════════════════════════

const AZURE_TENANT_ID = '';
const AZURE_CLIENT_ID = '';
const AZURE_CLIENT_SECRET = '';
const AZURE_SCOPE = 'https://databricks.azure.net/.default';

const PORT = 3001;

// ═══════════════════════════════════════════════════════════

const express = require('express');
const axios = require('axios');
const cors = require('cors');

const app = express();

// Token cache
let tokenCache = {
  accessToken: null,
  expiresAt: null
};

// Middleware
app.use(cors());
app.use(express.json());

// Check if token is valid
function isTokenValid() {
  if (!tokenCache.accessToken || !tokenCache.expiresAt) {
    return false;
  }
  const now = Date.now();
  const bufferTime = 5 * 60 * 1000; // 5 minutes buffer
  return tokenCache.expiresAt - now > bufferTime;
}

// Generate OAuth token
async function generateToken() {
  if (isTokenValid()) {
    console.log('✅ Using cached token');
    return tokenCache.accessToken;
  }

  try {
    console.log('🔄 Generating new OAuth token...');
    const tokenUrl = `https://login.microsoftonline.com/${AZURE_TENANT_ID}/oauth2/v2.0/token`;
    
    const params = new URLSearchParams({
      client_id: AZURE_CLIENT_ID,
      client_secret: AZURE_CLIENT_SECRET,
      scope: AZURE_SCOPE,
      grant_type: 'client_credentials'
    });

    const response = await axios.post(tokenUrl, params, {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
    });

    const { access_token, expires_in } = response.data;
    
    tokenCache.accessToken = access_token;
    tokenCache.expiresAt = Date.now() + (expires_in * 1000);
    
    console.log(`✅ Token generated (expires in ${expires_in}s = ${Math.floor(expires_in/60)} min)`);
    console.log('🔑 Token:', access_token);
    console.log('⏰ Expires at:', new Date(tokenCache.expiresAt).toISOString());
    console.log('');
    
    return access_token;
  } catch (error) {
    console.error('❌ Token generation failed:', error.response?.data || error.message);
    throw new Error('OAuth token generation failed: ' + (error.response?.data?.error_description || error.message));
  }
}

// Middleware
app.use(cors());
app.use(express.json());
app.get('/token', async (req, res) => {
  try {
    console.log('🔐 Token request received...');
    const token = await generateToken();
    res.json({
      success: true,
      token: token,
      expiresAt: new Date(tokenCache.expiresAt).toISOString(),
      expiresIn: Math.floor((tokenCache.expiresAt - Date.now()) / 1000)
    });
  } catch (error) {
    console.error('❌ Token generation error:', error.message);
    res.status(500).json({
      success: false,
      error: error.message
    });
  }
});

// Clear token cache
app.delete('/cache', (req, res) => {
  console.log('🗑️ Clearing token cache...');
  tokenCache = {
    accessToken: null,
    expiresAt: null
  };
  res.json({
    success: true,
    message: 'Token cache cleared'
  });
});

// Error handler
app.use((err, req, res, next) => {
  console.error('❌ Unhandled error:', err);
  res.status(500).json({
    success: false,
    error: 'Internal server error',
    message: err.message
  });
});

app.listen(PORT, () => {
  console.log('');
  console.log('═══════════════════════════════════════════════════════════');
  console.log('🔐 OAuth M2M Token Generator');
  console.log('═══════════════════════════════════════════════════════════');
  console.log(`📡 Server: http://localhost:${PORT}`);
  console.log(`🆔 Tenant: ${AZURE_TENANT_ID}`);
  console.log(`🔑 Client: ${AZURE_CLIENT_ID}`);
  console.log('');
  console.log('Endpoints:');
  console.log('  GET    /token    - Generate OAuth token');
  console.log('  DELETE /cache    - Clear token cache');
  console.log('');
  console.log(`Test: curl http://localhost:${PORT}/token`);
  console.log('═══════════════════════════════════════════════════════════');
  console.log('');
});
