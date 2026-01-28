# Environment Setup Guide

## Required Environment Variables

DesktopShell uses environment variables for sensitive configuration that should not be committed to version control.

### DESKTOPSHELL_PASSPHRASE

**Purpose**: Authentication passphrase for TCP server remote commands.

**Required**: Yes (for TCP server functionality)

**Default**: Falls back to `"default"` if not set (⚠️ not secure for production use)

#### Setup Instructions

##### Windows (PowerShell)

**For current session only:**
```powershell
$env:DESKTOPSHELL_PASSPHRASE = "your-secure-passphrase-here"
```

**Permanently (User level):**
```powershell
[System.Environment]::SetEnvironmentVariable('DESKTOPSHELL_PASSPHRASE', 'your-secure-passphrase-here', 'User')
```

**Permanently (System level - requires admin):**
```powershell
[System.Environment]::SetEnvironmentVariable('DESKTOPSHELL_PASSPHRASE', 'your-secure-passphrase-here', 'Machine')
```

##### Windows (Command Prompt)

**For current session only:**
```cmd
set DESKTOPSHELL_PASSPHRASE=your-secure-passphrase-here
```

**Permanently (User level):**
```cmd
setx DESKTOPSHELL_PASSPHRASE "your-secure-passphrase-here"
```

##### Windows (GUI Method)

1. Open **Start Menu** → type "environment variables"
2. Select **Edit the system environment variables**
3. Click **Environment Variables...**
4. Under **User variables**, click **New...**
5. Variable name: `DESKTOPSHELL_PASSPHRASE`
6. Variable value: your-secure-passphrase-here
7. Click **OK** on all dialogs
8. **Restart any open terminals/applications** for changes to take effect

#### Visual Studio / Development

After setting environment variables, you may need to:
- Restart Visual Studio
- Restart VS Code
- Close and reopen any terminal windows

#### Verification

To verify your environment variable is set correctly:

**PowerShell:**
```powershell
$env:DESKTOPSHELL_PASSPHRASE
```

**Command Prompt:**
```cmd
echo %DESKTOPSHELL_PASSPHRASE%
```

Should output your passphrase value.

#### Security Best Practices

1. ✅ **Use a strong passphrase**: At least 16 characters, mix of letters, numbers, symbols
2. ✅ **Never commit**: Don't put passphrases in code or configuration files
3. ✅ **Rotate regularly**: Change your passphrase periodically
4. ✅ **Different per environment**: Use different passphrases for dev/test/prod
5. ❌ **Don't share**: Each developer should have their own passphrase
6. ❌ **Don't use "default"**: The default fallback is insecure

#### For CI/CD

If you're using continuous integration:
- GitHub Actions: Set as repository secret
- Azure DevOps: Use Pipeline Variables (mark as secret)
- Jenkins: Use Credentials binding

Example GitHub Actions:
```yaml
env:
  DESKTOPSHELL_PASSPHRASE: ${{ secrets.DESKTOPSHELL_PASSPHRASE }}
```

---

## Optional TLS for TCP remote commands

Enable TLS for the TCP server by setting:

- `DESKTOPSHELL_TCP_TLS` = `1`
- `DESKTOPSHELL_TCP_TLS_PFX` = full path to a PFX file containing the server certificate + private key
- `DESKTOPSHELL_TCP_TLS_PFX_PASSWORD` = password for the PFX file

Optional client pinning (for self-signed certs):

- `DESKTOPSHELL_TCP_TLS_THUMBPRINT` = certificate thumbprint (no spaces)

**How to get these:**
- If you already have a certificate, export it to a PFX and note the password.
- For self-signed certs, use the certificate thumbprint shown in the certificate details.

---

## HTTPS queue fallback (Cloudflare Access)

Enable queueing (used when direct TCP is unavailable):

- `DESKTOPSHELL_QUEUE_ENABLED` = `1`
- `DESKTOPSHELL_QUEUE_BASEURL` = queue API base URL (defaults to `https://queue.dlamanna.com`)
- `DESKTOPSHELL_CF_ACCESS_CLIENT_ID`
- `DESKTOPSHELL_CF_ACCESS_CLIENT_SECRET`

Optional encryption for queue payloads:

- `DESKTOPSHELL_QUEUE_KEY_B64` = Base64-encoded AES key (16/24/32 bytes)

**How to get the Access tokens:**
- Cloudflare Zero Trust dashboard → Access → Service Tokens → Create token. Use the Client ID and Client Secret.

**How to generate a queue key (optional):**
- Create a 32-byte random value and Base64-encode it. (Any 16/24/32-byte key is accepted.)

## Troubleshooting

**Problem**: Application logs show "### Socket not connected" or authentication failures

**Solution**: Verify environment variable is set and application has been restarted after setting it.

**Problem**: Tests failing with PassPhrase errors

**Solution**: Either set the environment variable, or tests will use the "default" fallback.

---

**Note**: After setting up environment variables, restart your IDE and terminal for changes to take effect.
