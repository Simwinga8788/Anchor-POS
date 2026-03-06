# Anchor POS - Developer & Setup Manual

## 1. System Overview
**Anchor POS** is a WPF Desktop Application built on .NET 10.
- **Frontend**: WPF (XAML) with Material Design.
- **Backend Service**: Dependency Injection, Entity Framework Core (SQL Server Express).
- **Reporting**: ClosedXML (Excel), System.Net.Mail (Email).
- **Printing**: System.Drawing.Printing (GDI+).

---

## 2. Configuration System
The application uses a **Hybrid Configuration System** to ensure robustness.

### Priority Order:
1.  **Local File (`store_config.json`)**: Primary source for Store Name, Address, Phone, and Paper Size.
    -   Location: `%LocalStorage%\AnchorPOS\store_config.json`
    -   *Why?* Allows changing settings without DB access and persists across updates.
2.  **Database (`AppSettings` Table)**: Fallback source.
    -   If file is missing or unreadable, the app queries the `AppSettings` table.
3.  **Hardcoded Defaults**: Last resort (e.g., "Anchor POS").

### Key Config Files:
-   `store_config.json`: Store Name, Address, Phone, PaperSize ("80mm" or "58mm").
-   `printer_config.txt`: Name of the selected printer.

---

## 3. User Roles & Security
The system has three distinct roles:
1.  **Developer** (Hidden Role):
    -   **Access**: Full Access + **Settings Menu**.
    -   **Default Creds**: `developer` / `dev123`
    -   *Note*: The "Settings" button on the dashboard is **only** visible to this role.
2.  **Admin**:
    -   **Access**: Point of Sale, Product Management, Reports, User Management.
    -   **Restriction**: Cannot access Settings.
3.  **Salesperson**:
    -   **Access**: Point of Sale only.

**Seeding**: The `DbSeeder.cs` ensures the `developer` account exists on startup.

---

## 4. Printer Setup Guide

### A. Standard USB Thermal Printer (80mm)
1.  Install the manufacturer driver.
2.  Verify it appears in Windows "Printers & Scanners".
3.  In Anchor POS > **Settings**:
    -   Click **REFRESH PRINTERS**.
    -   Select the printer.
    -   Set Paper Size to **"80mm (Standard)"**.
    -   Click **SAVE SETTINGS**.

### B. Bluetooth / Mobile Printer (58mm)
*Most "phone-style" Bluetooth printers require a specific setup on Windows to act as a standard printer.*

**Step 1: Pair in Windows**
-   Settings > Devices > Bluetooth > Add Device.
-   Pair with printer (PIN: `0000` or `1234`).

**Step 2: Identify COM Port**
-   In Bluetooth Settings > **More Bluetooth options** > **COM Ports**.
-   Note the **Outgoing** COM port (e.g., `COM3`).

**Step 3: Install Generic Driver**
-   Open `Control Panel > Devices and Printers`.
-   Add a Printer > "The printer that I want isn't listed".
-   "Add a local printer... with manual settings".
-   **Port**: Select the COM port from Step 2 (e.g., `COM3`).
-   **Driver**: Manufacturer: **Generic** > **Generic / Text Only**.
-   Name: "Mobile Bluetooth Printer".

**Step 4: Configure in Anchor POS**
-   **Settings** > Select "Mobile Bluetooth Printer".
-   **Paper Size**: Set to **"58mm (Mobile/Bluetooth)"**.
    -   *Crucial*: This adjusts the receipt width layout to 32 columns (vs 42 for 80mm) to prevent text wrapping.
-   Click **SAVE SETTINGS**.

---

## 5. Troubleshooting

**Issue: Receipt Text is Not Centered / Left Aligned**
-   **Cause**: Paper Size setting mismatch.
-   **Fix**:
    -   If using **80mm** paper, ensure Settings > Paper Size is **80mm**.
    -   If using **58mm** paper, ensure Settings > Paper Size is **58mm**.
    -   *Note*: 80mm uses 42-column layout; 58mm uses 32-column layout.

**Issue: Store Name shows "KENJI'S..." instead of custom name**
-   **Cause**: Configuration file might be missing or not updated.
-   **Fix**: 
    1.  Login as `developer`.
    2.  Go to **Settings**.
    3.  Verify the Store Name is correct.
    4.  Click **SAVE SETTINGS**. This forces a write to `store_config.json`.
    5.  Restart the app.

**Issue: "Printer Not Accessible"**
-   **Cause**: Printer is offline, Bluetooth disconnected, or wrong COM port.
-   **Fix**:
    -   Check if printer is ON.
    -   Check Windows "Printers & Scanners" queue.
    -   If Bluetooth, remove device and re-pair.

---

## 6. Development Notes
-   **Receipt Logic**: Located in `ReceiptPrinterService.cs`.
    -   `GenerateReceiptContent`: Builds the receipt string based on Paper Size width.
    -   `PrintReceiptAsync`: Sends the string to the Windows Driver using `System.Drawing.Graphics`.
-   **Reports**: Located in `ExcelService.cs` (ClosedXML).
-   **Emails**: Located in `EmailService.cs` (SmtpClient).
