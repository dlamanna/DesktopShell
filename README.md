## DesktopShell
###### <em>Created in Visual Studio 2017</em>
============

This program serves as a replacement of desktop shortcuts and batch files, and an extension to the Windows start menu.

____

### Setup

**Before running DesktopShell**, you need to configure environment variables for secure operation. See [ENVIRONMENT_SETUP.md](ENVIRONMENT_SETUP.md) for detailed instructions.

#### Environment variables (overview)

- `DESKTOPSHELL_PASSPHRASE` (required): TCP remote command passphrase.
- `DESKTOPSHELL_TCP_TLS` (optional): set to `1` to enable TLS for TCP remote commands.
- `DESKTOPSHELL_TCP_TLS_PFX` / `DESKTOPSHELL_TCP_TLS_PFX_PASSWORD` (optional): server certificate for TLS.
- `DESKTOPSHELL_TCP_TLS_THUMBPRINT` (optional): client-side pin for self-signed certs.
- `DESKTOPSHELL_QUEUE_ENABLED` (optional): set to `1` to enable HTTPS queue fallback.
- `DESKTOPSHELL_QUEUE_BASEURL` (optional): queue API base URL (defaults to `https://queue.dlamanna.com`).
- `DESKTOPSHELL_QUEUE_KEY_B64` (optional): Base64-encoded AES key for queue message encryption.
- `DESKTOPSHELL_CF_ACCESS_CLIENT_ID` / `DESKTOPSHELL_CF_ACCESS_CLIENT_SECRET` (required if queue enabled): Cloudflare Access service token credentials.

### Linux Harness Commands

When running in a sandboxed Linux harness (where the repo path is not writable), use these wrappers instead of calling `dotnet` directly:

```bash
# Test wrapper (writes obj/bin/results under /tmp/desktopshell-harness)
./scripts/test-harness-linux.sh

# Publish wrapper for win-x64 single-file
./scripts/publish-win-x64-singlefile-harness-linux.sh
```

Notes:
- Wrappers force `BaseIntermediateOutputPath` and `BaseOutputPath` into `/tmp`.
- Wrappers set `EnableWindowsTargeting=true` and clear/override fallback package behavior.
- If package restore is blocked by network restrictions and required packages are missing from cache, restore/publish can still fail.
- `DesktopShell.Tests` targets `net8.0-windows`; test execution itself may still require Windows behavior depending on test content.

This is mostly meant as a code example since many of the features of this program launch other programs I coded which are not bundled in this repository. If you want to try it anyways, add your [regular expression](https://regexr.com/), program executable path, and commandline arguments in separate lines in the shortcuts.txt file. Add one line spacing between statements, and if no commandline arguments are required, put a dash on the third line. To use your new regex either re-open the DesktopShell.exe program or type the command "rescan".

***

### Example:

<p>
(^txt$){1}<br>
C:\notepad.exe<br>
"C:\Program Files (x86)\testfile.txt"<br>
C:\notepad.exe<br>
"C:\Program Files (x86)\testfile2.txt"<br>
<br>
(^img$){1}<br>
C:\paint.exe<br>
-</p>
