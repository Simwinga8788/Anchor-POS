# Installing Proper Driver for PL-260N Thermal Printer

## The Issue
You're currently using the **Generic/Text Only** driver, which doesn't support graphics printing. This causes the application to crash when trying to print receipts.

## Solution: Install a Compatible Driver

### Method 1: Use Windows Generic ESC/POS Driver (Easiest)

1. **Open Printer Settings**
   - Press `Win + I` → **Devices** → **Printers & scanners**
   - Find your printer (currently using Generic/Text Only)
   - Click **Remove device**

2. **Add Printer with Correct Driver**
   - Click **"Add a printer or scanner"**
   - Click **"The printer that I want isn't listed"**
   - Select **"Add a local printer or network printer with manual settings"**
   - Click **Next**

3. **Select Port**
   - Choose **"Use an existing port"**
   - Select the USB port your printer is connected to (e.g., `USB001`)
   - Click **Next**

4. **Choose Driver**
   - Manufacturer: **Generic**
   - Printers: **Generic / Text Only** ❌ (DON'T use this)
   - Instead, look for:
     - **"MS Publisher Imagesetter"** ✅
     - **"Generic IBM Graphics 9pin"** ✅
     - **"Generic IBM Graphics 9pin wide"** ✅
   
   OR better yet:
   
   - Click **"Windows Update"** to download more drivers
   - After update, look for:
     - **"EPSON TM-T20"** (very compatible with ESC/POS)
     - **"EPSON TM-T88"** (industry standard)
     - **"Star TSP100"** (also ESC/POS compatible)
   
5. **Complete Installation**
   - Click **Next** → **Next** → **Finish**
   - Set as **Default Printer**
   - Print a test page

### Method 2: Download Manufacturer Driver

1. **Search for PL-260N Driver**
   - Google: "PL-260N thermal printer driver download"
   - Or check the manufacturer's website/CD that came with printer

2. **Install Driver**
   - Run the installer
   - Follow the installation wizard
   - Restart computer if prompted

3. **Verify Installation**
   - Check Printers & scanners
   - Should show proper driver name (not "Generic/Text Only")

### Method 3: Use EPSON TM-T20 Driver (Most Compatible)

Most ESC/POS thermal printers work with EPSON drivers:

1. **Download EPSON TM-T20 Driver**
   - Visit: https://epson.com/Support/Point-of-Sale/Thermal-Printers/TM-T20II/s/SPT_C31CD52062
   - Download the Windows driver

2. **Install for Your Printer**
   - During installation, select your USB port
   - Complete the setup

3. **Your PL-260N should work** with this driver (ESC/POS standard)

---

## Quick Test After Driver Installation

### Test 1: Windows Test Page
1. Right-click printer → **Printer properties**
2. Click **"Print Test Page"**
3. Should print a formatted page with graphics ✅

### Test 2: Anchor POS Application
1. Run the application
2. Complete a test transaction
3. Click "Print Receipt"
4. Should print without errors ✅

---

## Alternative: Modify Code for Text-Only Driver

If you MUST use the Generic/Text Only driver, I can modify the code to send raw text instead of using graphics. However, this is **NOT recommended** because:
- ❌ Limited formatting options
- ❌ No logo support
- ❌ No barcode/QR code support
- ❌ More complex code

**Recommendation**: Install a proper driver instead!

---

## Troubleshooting

### "I can't find a compatible driver"
- Use **EPSON TM-T20** driver (works with most ESC/POS printers)
- Or use **MS Publisher Imagesetter** as a fallback

### "Driver installs but printer still doesn't work"
- Make sure you selected the correct USB port
- Try unplugging and replugging the USB cable
- Restart the computer

### "Test page prints but Anchor POS still fails"
- Check that the printer is set as **Default**
- Verify the printer name in Windows matches what the app expects
- Check the error message for specific details

---

## Current vs. Recommended Setup

| Setting | Current (❌ Not Working) | Recommended (✅ Working) |
|---------|-------------------------|-------------------------|
| Driver | Generic/Text Only | EPSON TM-T20 or ESC/POS driver |
| Graphics Support | No | Yes |
| Font Support | No | Yes |
| Formatting | Plain text only | Full formatting |
| Application Compatibility | ❌ Fails | ✅ Works |

---

*After installing the proper driver, your receipts will print perfectly!*
