# Thermal Receipt Printer Setup Guide

## Printer Specifications

### Hardware Information
- **Model**: PL-260N Thermal Receipt Printer
- **Manufacturer**: Generic ESC/POS Compatible
- **Paper Width**: 80mm (standard thermal receipt paper)
- **Print Speed**: 260mm/s
- **Serial Number**: BM2112030397

### Power Requirements
- **Printer Power**: 24V DC, 2.5A
- **Cash Drawer**: 24V DC, 1A (supports automatic cash drawer opening)

### Connectivity Options
Your printer supports three connection methods:
1. **USB** - Recommended for desktop POS systems
2. **RS232 (Serial)** - Legacy connection
3. **LAN (Network)** - For networked POS systems

### Command Support
- **ESC/POS** - Industry standard thermal printer command set
- Supports standard receipt formatting, cutting, and cash drawer control

---

## Windows Setup Instructions

### Step 1: Install the Printer Driver

1. **Connect the printer** via USB cable to your computer
2. **Power on the printer** (ensure 24V power adapter is connected)
3. **Windows should auto-detect** the printer and install generic drivers
4. If auto-detection fails:
   - Download the driver from the manufacturer's website
   - Or use the generic "POS Printer" driver in Windows

### Step 2: Set as Default Printer

1. Open **Settings** → **Devices** → **Printers & scanners**
2. Find your thermal printer in the list (may appear as "POS-80" or "PL-260N")
3. Click on it and select **"Set as default"**
4. Click **"Manage"** → **"Print a test page"** to verify it works

### Step 3: Configure Printer Preferences

1. Right-click the printer → **"Printing preferences"**
2. Set the following:
   - **Paper Size**: Custom (80mm width)
   - **Orientation**: Portrait
   - **Quality**: Draft (faster printing)
   - **Paper Type**: Thermal

### Step 4: Test with Anchor POS

1. Launch the Anchor POS application
2. Complete a test transaction
3. Click **"Print Receipt"** to test printing
4. Verify the receipt prints correctly with proper formatting

---

## Application Configuration

### Current Settings

The Anchor POS application is already configured for your 80mm thermal printer:

```csharp
// Receipt width in characters (optimized for 80mm thermal paper)
private const int RECEIPT_WIDTH = 32;

// Font settings for thermal printing
var font = new Font("Courier New", 9);
```

### Receipt Format

The application generates receipts with the following structure:

```
================================
     ANCHOR POS
         Point of Sale
--------------------------------

Receipt #: 123
Date: 2026-02-09 19:14:19
Cashier: admin
Payment: Cash
--------------------------------

ITEMS:

Product Name Here
  2 x K50.00              K100.00

Another Product
  1 x K25.00               K25.00

--------------------------------
TOTAL:                   K125.00
================================

    Thank you for shopping!
       Please come again
```

### Cash Drawer Integration

The printer supports automatic cash drawer opening via ESC/POS commands:

- **Command**: `ESC p m t1 t2` (0x1B 0x70 0x00 0x19 0xFA)
- **Trigger**: Automatically opens when a cash transaction is completed
- **Connection**: Cash drawer connects to the printer's RJ11/RJ12 port

---

## Troubleshooting

### Issue: "No printer is installed or available"

**Solution:**
1. Verify the printer is powered on
2. Check USB cable connection
3. Ensure printer appears in Windows "Printers & scanners"
4. Set the thermal printer as the default printer

### Issue: "Printer is not valid or not accessible"

**Solution:**
1. Check if the printer is online (not paused or offline)
2. Clear the print queue: Settings → Printers → Select printer → "Open queue" → Cancel all documents
3. Restart the printer and computer
4. Reinstall the printer driver

### Issue: Receipt prints but formatting is wrong

**Solution:**
1. Verify paper width is set to 80mm in printer preferences
2. Ensure "Courier New" font is installed on your system
3. Check that the receipt width constant matches your paper (32 characters for 80mm)

### Issue: Cash drawer doesn't open

**Solution:**
1. Verify cash drawer is properly connected to the printer's drawer port
2. Check drawer power connection (24V, 1A)
3. Test drawer opening manually through printer settings
4. Ensure ESC/POS commands are enabled in printer firmware

### Issue: Printer is slow or prints blank receipts

**Solution:**
1. Check thermal paper is installed correctly (thermal side facing print head)
2. Clean the print head with isopropyl alcohol
3. Verify paper roll is not jammed
4. Replace thermal paper if it's old or faded

---

## Paper Specifications

### Recommended Thermal Paper

- **Width**: 80mm (± 0.5mm)
- **Diameter**: 80mm roll (standard)
- **Core**: 12mm inner diameter
- **Length**: 50-80 meters per roll
- **Thickness**: 55-65 microns
- **Type**: BPA-free thermal paper (recommended)

### Paper Installation

1. Open the printer cover
2. Insert the paper roll with thermal side facing the print head
3. Pull out about 10cm of paper
4. Close the cover (it will auto-feed)
5. Tear off excess paper

---

## Maintenance

### Daily
- Check paper level and replace when low
- Clear any paper jams immediately

### Weekly
- Clean the print head with isopropyl alcohol and a soft cloth
- Check for paper dust buildup

### Monthly
- Inspect USB/power cables for damage
- Test cash drawer operation
- Print a test receipt to verify quality

### As Needed
- Replace thermal paper rolls
- Update printer drivers if issues occur
- Clean the paper cutter blade

---

## Technical Support

### Application Issues
- Check the application logs in the Debug output
- Review error messages in the receipt preview dialog
- Ensure all services are properly registered in dependency injection

### Printer Hardware Issues
- Consult the printer manual for hardware troubleshooting
- Contact the printer manufacturer for warranty support
- Check online forums for ESC/POS printer issues

### Driver Issues
- Visit the manufacturer's website for latest drivers
- Use Windows Update to check for driver updates
- Try the generic "POS Printer" driver as a fallback

---

## Additional Features

### Supported by Your Printer
✅ Auto-cut (automatic paper cutting after each receipt)
✅ Cash drawer control (ESC/POS commands)
✅ High-speed printing (260mm/s)
✅ Multiple connectivity options (USB, Serial, LAN)

### Planned Application Features
🔄 Logo printing (requires image support)
🔄 Barcode printing (for product codes)
🔄 QR code printing (for digital receipts)
🔄 Custom receipt templates

---

## Configuration Files

The printer service is located at:
```
src/AnchorPOS.Services/ReceiptPrinterService.cs
```

Key configuration constants:
- `RECEIPT_WIDTH = 32` - Characters per line for 80mm paper
- Font: "Courier New", 9pt - Monospace font for alignment
- Cash drawer command: `{0x1B, 0x70, 0x00, 0x19, 0xFA}`

---

## Quick Reference

| Setting | Value |
|---------|-------|
| Paper Width | 80mm |
| Characters per Line | 32 |
| Font | Courier New, 9pt |
| Print Speed | 260mm/s |
| Interface | USB (recommended) |
| Command Set | ESC/POS |
| Cash Drawer Voltage | 24V DC, 1A |

---

*Last Updated: 2026-02-09*
*Anchor POS Version: 1.0*
