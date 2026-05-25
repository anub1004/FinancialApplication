#!/usr/bin/env pwsh
# Complete Bearer Token Test Script
# This script tests the assign-role endpoint with proper Bearer token handling

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Financial App - Bearer Token Test" -ForegroundColor Cyan
Write-Host "================================`n" -ForegroundColor Cyan

# Configuration
$baseUrl = "https://localhost:7085"
$adminEmail = "admin@financial.com"
$adminPassword = "Admin@123"

# Step 1: Login to get token
Write-Host "STEP 1: Logging in as admin..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = $adminEmail
        password = $adminPassword
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/Auth/login" `
        -Method POST `
        -Headers @{ "Content-Type" = "application/json" } `
        -Body $loginBody `
        -SkipCertificateCheck `
        -ErrorAction Stop

    $accessToken = $loginResponse.accessToken
    $expiresIn = $loginResponse.expiresIn

    Write-Host "✓ Login successful!" -ForegroundColor Green
    Write-Host "  Token expires in: $expiresIn seconds" -ForegroundColor Green
    Write-Host "  Token: $($accessToken.Substring(0, 50))..." -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Verify token structure
Write-Host "STEP 2: Verifying token structure..." -ForegroundColor Yellow
$tokenParts = $accessToken -split '\.'
if ($tokenParts.Count -eq 3) {
    Write-Host "✓ Token has valid JWT structure (3 parts)" -ForegroundColor Green
}
else {
    Write-Host "✗ Token structure invalid" -ForegroundColor Red
}
Write-Host ""

# Step 3: Test assign-role with Bearer prefix
Write-Host "STEP 3: Testing /api/Auth/assign-role with Bearer token..." -ForegroundColor Yellow

$userId = "EB7D2545-985F-4997-BAAE-DF04F452D599"
$roleName = "Manager"

try {
    $assignBody = @{
        userId = $userId
        roleName = $roleName
    } | ConvertTo-Json

    $assignResponse = Invoke-RestMethod `
        -Uri "$baseUrl/api/Auth/assign-role" `
        -Method POST `
        -Headers @{
            "Authorization" = "Bearer $accessToken"
            "Content-Type" = "application/json"
        } `
        -Body $assignBody `
        -SkipCertificateCheck `
        -ErrorAction Stop

    Write-Host "✓ Role assignment successful!" -ForegroundColor Green
    Write-Host "  Response: $assignResponse" -ForegroundColor Green
    Write-Host ""
}
catch {
    $errorResponse = $_.Exception.Response
    $statusCode = $errorResponse.StatusCode.value__
    
    Write-Host "✗ Role assignment failed!" -ForegroundColor Red
    Write-Host "  Status Code: $statusCode" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($statusCode -eq 401) {
        Write-Host "  → Token is invalid or expired. Try logging in again." -ForegroundColor Yellow
    }
    elseif ($statusCode -eq 403) {
        Write-Host "  → Access denied. Make sure you have Admin role." -ForegroundColor Yellow
    }
    elseif ($statusCode -eq 404) {
        Write-Host "  → User ID not found: $userId" -ForegroundColor Yellow
    }
    
    Write-Host ""
}

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Test Complete" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
