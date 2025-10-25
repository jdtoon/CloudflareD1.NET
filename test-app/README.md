# CloudflareD1.NET - Cloudflare D1 Connection Test

This test application verifies that the published CloudflareD1.NET NuGet package works correctly with your Cloudflare D1 database.

## 🎉 Package Successfully Downloaded from NuGet.org!

The test app is using **CloudflareD1.NET version 1.0.0** from the public NuGet registry.

## Prerequisites

1. **Cloudflare Account** with D1 database access
2. **D1 Database** created in your Cloudflare account
3. **API Token** with D1 permissions

## Setup Instructions

### Step 1: Get Your Cloudflare Credentials

#### A. Get Your Account ID
1. Log in to [Cloudflare Dashboard](https://dash.cloudflare.com/)
2. Go to any page in your account
3. Look in the URL or right sidebar - your Account ID is visible there
4. Copy your Account ID (format: `xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`)

#### B. Create or Get Your D1 Database ID
1. Go to **Workers & Pages** → **D1 SQL Database**
2. Create a new database or select an existing one
3. Copy the Database ID from the database details page

#### C. Create an API Token
1. Go to **My Profile** → **API Tokens**
2. Click **Create Token**
3. Choose **Edit Cloudflare Workers** template (or create custom)
4. Ensure these permissions are included:
   - Account → D1 → Edit
5. Click **Continue to summary** → **Create Token**
6. **IMPORTANT**: Copy the token immediately (you won't see it again!)

### Step 2: Configure the Test App

Edit `appsettings.json` and replace the placeholder values:

```json
{
  "CloudflareD1": {
    "UseLocalMode": false,
    "AccountId": "your-actual-account-id-here",
    "DatabaseId": "your-actual-database-id-here",
    "ApiToken": "your-actual-api-token-here"
  }
}
```

**Example:**
```json
{
  "CloudflareD1": {
    "UseLocalMode": false,
    "AccountId": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6",
    "DatabaseId": "12345678-1234-1234-1234-123456789abc",
    "ApiToken": "abcdefghijklmnopqrstuvwxyz123456789"
  }
}
```

### Step 3: Run the Test

```bash
dotnet run
```

## What the Test Does

The application will perform these operations:

1. ✅ **Create Table** - Creates a `test_users` table
2. ✅ **Insert Data** - Adds a test user with parameterized query
3. ✅ **Query Data** - Retrieves and displays recent users
4. ✅ **Update Data** - Updates the test user's name
5. ✅ **Batch Operations** - Inserts multiple users in a transaction
6. ✅ **Count Records** - Gets total user count
7. ✅ **Cleanup** - Deletes test data (optional)

## Expected Output

```
=== CloudflareD1.NET - Cloudflare D1 Connection Test ===

✓ Configuration loaded successfully
  Use Local Mode: false
  Account ID: a1b2c3d4...
  Database ID: 12345678...
  API Token: abcdefgh...

Step 1: Creating test table...
✓ Table created successfully

Step 2: Inserting test data...
✓ Inserted user with ID: 1

Step 3: Querying data...
✓ Found 1 users:
  - ID: 1, Name: Test User, Email: test123@example.com

Step 4: Updating data...
✓ Updated 1 row(s)

Step 5: Testing batch operations...
✓ Executed 2 statements in batch

Step 6: Getting total count...
✓ Total users in database: 3

Step 7: Cleaning up test data...
✓ Deleted 3 test row(s)

========================================
🎉 ALL TESTS PASSED SUCCESSFULLY!
========================================

Your CloudflareD1.NET package is working correctly with Cloudflare D1!
```

## Troubleshooting

### Error: "401 Unauthorized"
- **Cause**: Invalid API Token or insufficient permissions
- **Solution**: 
  1. Verify your API Token is correct
  2. Ensure the token has "Account → D1 → Edit" permissions
  3. Create a new token if needed

### Error: "404 Not Found"
- **Cause**: Invalid Account ID or Database ID
- **Solution**:
  1. Double-check your Account ID from Cloudflare dashboard
  2. Verify your Database ID from the D1 database details page
  3. Ensure the database actually exists

### Error: "Configuration not found"
- **Cause**: `appsettings.json` file not found or not copied to output
- **Solution**: The project file is configured to copy it automatically, but verify the file exists in the same directory as the executable

### Error: "Network error" or timeout
- **Cause**: Firewall or network restrictions
- **Solution**: Ensure you can access `https://api.cloudflare.com` from your network

## Testing with Local Mode

If you want to test the package functionality without Cloudflare, you can switch to local SQLite mode:

```json
{
  "CloudflareD1": {
    "UseLocalMode": true,
    "LocalDatabasePath": "test.db"
  }
}
```

Then run:
```bash
dotnet run
```

This will create a local SQLite database file instead of connecting to Cloudflare.

## Alternative: Environment Variables

Instead of using `appsettings.json`, you can set environment variables:

**PowerShell:**
```powershell
$env:CloudflareD1__UseLocalMode="false"
$env:CloudflareD1__AccountId="your-account-id"
$env:CloudflareD1__DatabaseId="your-database-id"
$env:CloudflareD1__ApiToken="your-api-token"
dotnet run
```

**Bash/Linux:**
```bash
export CloudflareD1__UseLocalMode="false"
export CloudflareD1__AccountId="your-account-id"
export CloudflareD1__DatabaseId="your-database-id"
export CloudflareD1__ApiToken="your-api-token"
dotnet run
```

## Security Note

⚠️ **IMPORTANT**: The `appsettings.json` file contains sensitive credentials. 

- **DO NOT commit** this file to git with real credentials
- Consider using environment variables for production
- Use User Secrets for development: `dotnet user-secrets set "CloudflareD1:ApiToken" "your-token"`

## Next Steps

Once the test passes successfully:

1. ✅ Confirm CloudflareD1.NET works with your Cloudflare D1 database
2. Use the package in your own projects: `dotnet add package CloudflareD1.NET`
3. Check out the [samples](https://github.com/jdtoon/CloudflareD1.NET/tree/main/samples) for more examples
4. Read the [documentation](https://jdtoon.github.io/CloudflareD1.NET/) for advanced usage

## Support

- 📖 [Documentation](https://jdtoon.github.io/CloudflareD1.NET/)
- 💬 [GitHub Discussions](https://github.com/jdtoon/CloudflareD1.NET/discussions)
- 🐛 [Report Issues](https://github.com/jdtoon/CloudflareD1.NET/issues)
- 📦 [NuGet Package](https://www.nuget.org/packages/CloudflareD1.NET/)

---

**Happy Testing! 🚀**
