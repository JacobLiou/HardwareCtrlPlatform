# Publish quick start

Default RID is **win-x86** (UDL2 COM is win32).

```powershell
.\scripts\publish.ps1
.\scripts\publish.ps1 -RuntimeId win-x86
.\scripts\publish.ps1 -Configuration Debug
```

Output: `publish\<rid>\<Configuration>\` for `Station.App`.
