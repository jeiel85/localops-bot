# Phase 1 Implementation Kit

Scope:

- Protocol foundation
- Remote command compatibility
- Outbound routing foundation

## Apply

```powershell
.\apply-phase1.ps1 -RepoRoot "D:\Project\homebase" -Apply
```

Without `-Apply`, the script performs a dry run.

The script adds only new files. Existing destination files cause an error instead of being overwritten.

After copying, apply `patch-guides` in numeric order.

## Verify

```powershell
.\verify-phase1.ps1 -RepoRoot "D:\Project\homebase"
```

This artifact runtime did not expose the .NET SDK. Compilation must be performed in the actual Homebase environment.
