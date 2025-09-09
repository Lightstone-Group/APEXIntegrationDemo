# APEX Integration Demo - README

This is a .NET 8 ASP.NET Core application that demonstrates integration with the APEX platform, including Product Flow and User Onboarding functionality. The application uses Azure Key Vault for secure credential management with multiple fallback mechanisms.

## Overview

The application showcases two main APEX integrations:
- **Product Flow**: Standardized digital product delivery system
- **User Onboarding**: Streamlined user registration and onboarding process

## Architecture

### TokenService
The `TokenService` handles authentication token retrieval with a sophisticated fallback mechanism:

1. **Azure Key Vault** (highest priority) - Retrieves credentials from Key Vault
2. **User Secrets** (development fallback) - Local development credentials
3. **Configuration** (final fallback) - appsettings.json values

### Configuration Priority

The application uses the following configuration sources in order of priority:

1. **Azure Key Vault secrets**
2. **User Secrets** (development)
3. **Environment Variables** (production)
4. **appsettings.json** (fallback)

## Required Configuration

### Essential Settings

The following settings are **required** for the application to function:

- **TenantId**: Azure tenant identifier
- **ApiKey**: APEX platform API key  
- **Referer**: Authorized referer URL
- **AuthToken**: Authentication token (if not obtaining via TokenService)

### Authentication Token Options

The application supports two authentication approaches:

#### Option 1: Dynamic Token Generation (Recommended)
Configure the TokenService to generate tokens dynamically:

**Key Vault Secrets:**
- `Lightstone--TokenUrl` ? OAuth2 token endpoint URL
- `Lightstone--UserEmail` ? Authentication email address
- `Lightstone--UserPassword` ? Authentication password
- `Lightstone--ClientId` ? OAuth2 client ID

**User Secrets (Development):**
```bash
dotnet user-secrets set "KeyVault:VaultUrl" "https://your-keyvault.vault.azure.net/"
dotnet user-secrets set "KeyVault:ClientId" "your-app-registration-client-id"
dotnet user-secrets set "KeyVault:ClientSecret" "your-app-registration-client-secret"
dotnet user-secrets set "KeyVault:TenantId" "your-azure-tenant-id"
```

#### Option 2: Static Token Fallback
If dynamic token generation fails or is unavailable, the application falls back to:

**User Secrets:**
```bash
dotnet user-secrets set "Lightstone:AuthToken" "your-static-bearer-token"
dotnet user-secrets set "Lightstone:ApiKey" "your-api-key"
```

**Or appsettings.json:**
```json
{
  "AppSettings": {
    "AuthToken": "your-static-bearer-token",
    "ApiKey": "your-api-key"
  }
}
```

## Setup Instructions

### 1. Initial Setup

```bash
# Clone the repository
git clone <repository-url>
cd ProductFlowIntegration

# Restore packages
dotnet restore
```

### 2. Development Configuration

Set up user secrets for local development:

```bash
# Initialize user secrets (if not already done)
dotnet user-secrets init

# Key Vault configuration (for dynamic token generation)
dotnet user-secrets set "KeyVault:VaultUrl" "https://your-keyvault.vault.azure.net/"
dotnet user-secrets set "KeyVault:ClientId" "your-client-id"
dotnet user-secrets set "KeyVault:ClientSecret" "your-client-secret"
dotnet user-secrets set "KeyVault:TenantId" "your-tenant-id"

# Fallback authentication (if Key Vault is unavailable)
dotnet user-secrets set "Lightstone:AuthToken" "your-static-token"
dotnet user-secrets set "Lightstone:ApiKey" "your-api-key"

# Verify secrets are set
dotnet user-secrets list
```

### 3. Production Configuration

For production environments, use environment variables:

```bash
# Key Vault configuration
KEYVAULT__VAULTURL=https://your-keyvault.vault.azure.net/
KEYVAULT__CLIENTID=your-client-id
KEYVAULT__CLIENTSECRET=your-client-secret
KEYVAULT__TENANTID=your-tenant-id

# Fallback authentication
LIGHTSTONE__AUTHTOKEN=your-static-token
LIGHTSTONE__APIKEY=your-api-key
```

### 4. Azure Key Vault Secrets Setup

Create the following secrets in your Azure Key Vault (using the exact names the TokenService expects):

```bash
# OAuth2 token endpoint URL
az keyvault secret set --vault-name "your-keyvault-name" --name "Lightstone--TokenUrl" --value "your-lightstone-token-url"

# Authentication credentials
az keyvault secret set --vault-name "your-keyvault-name" --name "Lightstone--UserEmail" --value "your-auth-email@domain.com"
az keyvault secret set --vault-name "your-keyvault-name" --name "Lightstone--UserPassword" --value "your-auth-password"

# OAuth2 client ID
az keyvault secret set --vault-name "your-keyvault-name" --name "Lightstone--ClientId" --value "your-oauth-client-id"
```

**Note**: The double dashes (`--`) in the secret names are converted to colons (`:`) by the Azure configuration provider.

## Configuration Files

### appsettings.json

Contains non-sensitive application configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "AppSettings": {
    "ProductCode": "your-lightstone-product-code",
    "ProductName": "your-lightstone-product-name",
    "TenantId": "your-tenant-id",
    "ProductUrl": "your-lightstone-product-url",
    "CreateUserUrl": "your-lightstone-user-url",
    "OnboardingUrl": "your-lightstone-onboarding-url",
    "Referer": "https://your-tenant-website.com/",
   
  }
}
```

**Note**: Credentials or any sensitive information should come from Key Vault or user secrets.

## Usage Examples

### TokenService in Controllers

```csharp
public class ExampleController : Controller
{
    private readonly TokenService _tokenService;
    private readonly IConfiguration _configuration;

    public ExampleController(TokenService tokenService, IConfiguration configuration)
    {
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<IActionResult> MakeApiCall()
    {
        // Try to get dynamic token first
        var token = await _tokenService.GetTokenAsync();
        var authToken = token?.AccessToken ?? 
                       _configuration["Lightstone:AuthToken"] ?? 
                       _configuration["AppSettings:AuthToken"];

        if (string.IsNullOrEmpty(authToken))
        {
            return BadRequest("No authentication token available");
        }

        // Use the token for API calls
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
        
        // Make your API call
        var response = await _httpClient.PostAsync(apiUrl, content);
        return Ok();
    }
}
```

## Features

### Product Flow Integration
- Demonstrates standardized product delivery workflow
- Handles user input collection and validation
- Integrates with APEX quoting and payment systems
- Supports both popup and embedded display modes

### User Onboarding Integration  
- Automated user registration in APEX platform
- Party ID generation and management
- Tenant association and welcome email handling
- Error handling and rollback capabilities

### Security Features
- Azure Key Vault integration for credential storage
- User secrets for development security
- Multiple fallback mechanisms for reliability
- Comprehensive logging for audit trails
- No sensitive data in source control

## Authentication Flow

1. **TokenService.GetTokenAsync()** attempts to retrieve credentials from:
   - Azure Key Vault secrets: `Lightstone--TokenUrl`, `Lightstone--UserEmail`, `Lightstone--UserPassword`, `Lightstone--ClientId`
   - Configuration fallbacks: `Lightstone:TokenUrl`, `Lightstone:UserEmail`, `Lightstone:UserPassword`, `Lightstone:ClientId`

2. **Makes OAuth2 token request** using retrieved credentials with ROPC (Resource Owner Password Credentials) flow

3. **Falls back to static token** if dynamic generation fails:
   - Checks `Lightstone:AuthToken` configuration
   - Checks `AppSettings:AuthToken` configuration

4. **Controllers use the token** for API authentication

## Troubleshooting

### Common Issues

1. **"No authentication token available"**
   - Verify Key Vault credentials are correct
   - Check user secrets are set: `dotnet user-secrets list`
   - Ensure fallback tokens are configured

2. **Key Vault connection fails**
   - Verify VaultUrl is correct
   - Check ClientId, ClientSecret, and TenantId
   - Ensure proper permissions on Key Vault

3. **API calls fail with 401 Unauthorized**
   - Check if token has expired
   - Verify tenant ID and API key are correct
   - Ensure referer header is whitelisted

4. **TokenService returns null**
   - Check that Key Vault secrets use correct names: `Lightstone--TokenUrl`, `Lightstone--UserEmail`, `Lightstone--UserPassword`, `Lightstone--ClientId`
   - Verify configuration fallback values in `Lightstone:*` section
   - Check logs for detailed error messages

### Logging

The application provides detailed logging for troubleshooting:
- Key Vault connection attempts
- Token generation success/failure
- Configuration fallback usage
- API call results

Check the application logs for detailed error information.

## Development Team Setup

Each developer should run these commands to set up their local environment:

```bash
# Set up user secrets with Key Vault credentials
dotnet user-secrets set "KeyVault:VaultUrl" "https://your-keyvault.vault.azure.net/"
dotnet user-secrets set "KeyVault:ClientId" "your-client-id"
dotnet user-secrets set "KeyVault:ClientSecret" "your-client-secret"  
dotnet user-secrets set "KeyVault:TenantId" "your-tenant-id"

# Set up fallback authentication
dotnet user-secrets set "Lightstone:AuthToken" "your-token"
dotnet user-secrets set "Lightstone:ApiKey" "your-api-key"

# Run the application
dotnet run
```

This ensures consistent development experience while maintaining security best practices.