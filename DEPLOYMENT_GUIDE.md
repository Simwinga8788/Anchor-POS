# 🚀 Surf POS - Deployment Guide

## Quick Start: Building the Installer

### Option 1: Automated Build (Recommended)

1. **Right-click** `BuildAndPackage.ps1`
2. Select **"Run with PowerShell"**
3. Wait for the build to complete
4. Find your installer in `installer_output\SurfPOS_Setup_v1.0.0.exe`

### Option 2: Manual Build

```powershell
# 1. Build the application
dotnet publish src\SurfPOS.Desktop\SurfPOS.Desktop.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true

# 2. Create installer (requires Inno Setup)
# Open SurfPOS_Installer.iss in Inno Setup and click "Compile"
```

---

## 📧 Email Configuration

### For Gmail Users:

1. **Enable 2-Step Verification**
   - Go to https://myaccount.google.com/security
   - Enable 2-Step Verification

2. **Create App Password**
   - Go to https://myaccount.google.com/apppasswords
   - Select "Mail" and "Windows Computer"
   - Copy the 16-character password

3. **Configure in Surf POS**
   - SMTP Host: `smtp.gmail.com`
   - SMTP Port: `587`
   - Username: `your.email@gmail.com`
   - Password: [16-character app password]
   - From Email: `your.email@gmail.com`

4. **Test Configuration**
   - Click "TEST EMAIL" button
   - Check your inbox

### For Other Email Providers:

| Provider | SMTP Host | Port |
|----------|-----------|------|
| Outlook/Hotmail | smtp-mail.outlook.com | 587 |
| Yahoo | smtp.mail.yahoo.com | 587 |
| Office 365 | smtp.office365.com | 587 |

---

## 📦 Installer Features

✅ **Automatic .NET Runtime Check**
- Detects if .NET 10 Desktop Runtime is installed
- Provides download link if missing

✅ **Clean Installation**
- Installs to Program Files
- Creates Start Menu shortcuts
- Optional Desktop icon

✅ **Database Setup**
- Automatically creates database directory
- Initializes with default admin user

✅ **Uninstaller**
- Clean removal of all files
- Optional: Keep database for reinstallation

---

## 🎯 Distribution Checklist

Before distributing to clients:

- [ ] Test email configuration works
- [ ] Verify all features (POS, Reports, User Management)
- [ ] Test on clean Windows machine
- [ ] Update version number in `SurfPOS_Installer.iss`
- [ ] Update `INSTALLATION_NOTES.txt` with support contact
- [ ] Create user manual/training materials
- [ ] Test installer on Windows 10 and 11

---

## 📝 Version History

### Version 1.0.0 (2026-01-05)
- Initial release
- Core POS functionality
- Email shift reports
- Audit trail
- MTD/YTD reporting
- Kwacha currency support

---

## 🛠️ Troubleshooting

### Build Errors

**Problem**: "dotnet command not found"
- **Solution**: Install .NET SDK from https://dotnet.microsoft.com/download

**Problem**: "Inno Setup not found"
- **Solution**: Download from https://jrsoftware.org/isdl.php

### Email Issues

**Problem**: "Failed to send test email"
- Check SMTP host and port
- Verify username/password
- For Gmail, ensure App Password is used (not regular password)
- Check firewall/antivirus settings

**Problem**: "Authentication failed"
- For Gmail: Use App Password, not account password
- For Office 365: Enable SMTP AUTH in admin center

---

## 📞 Support

For technical assistance:
- Email: support@surfpos.com
- Documentation: See `INSTALLATION_NOTES.txt`

---

**Kenji's Beauty Space POS System**
Built with ❤️ for retail excellence
