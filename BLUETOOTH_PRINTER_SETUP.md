# Setting Up a Bluetooth Mobile Printer on Windows

Most "phone-style" Bluetooth thermal printers do not work immediately after pairing. Windows needs a "Driver" to know how to talk to them.

## Step 1: Pair the Printer
1. Turn on the printer.
2. Go to **Settings > Devices > Bluetooth & other devices** on Windows.
3. Click **Add Bluetooth or other device**.
4. Select **Bluetooth**.
5. Click on your printer (often named like "MTP-2", "RPP02N", or "BlueTooth Printer").
6. Enter PIN if asked (usually `0000` or `1234`).
7. **Wait** until it says "Connected" or "Paired".

## Step 2: Find the COM Port
1. In Bluetooth Settings, scroll down and click **More Bluetooth options** (on the right or bottom).
2. Go to the **COM Ports** tab.
3. Look for your printer's name.
4. Note the **Outgoing** COM port number (e.g., `COM3` or `COM4`). This is the one we need.

## Step 3: Install "Generic" Driver
1. Press `Windows Key + R`, type `control printers`, and hit Enter.
2. Click **Add a printer**.
3. Click **"The printer that I want isn't listed"**.
4. Select **"Add a local printer or network printer with manual settings"**. Click Next.
5. Select **"Use an existing port"**.
6. In the dropdown, select the **COM port** you found in Step 2 (e.g., `COM3`). Click Next.
   * *If the specific COM port isn't there, try restarting your computer.*
7. For Manufacturer, choose **Generic**.
8. For Printers, choose **Generic / Text Only**. Click Next.
9. Name the printer (e.g., "Mobile Bluetooth Printer"). Click Next.
10. Do **not** share the printer.
11. Perform a test print to confirm it works.

## Step 4: Configure Anchor POS
1. Open **Anchor POS**.
2. Go to **Settings** (Login as Developer).
3. Click **REFRESH PRINTERS**.
4. Your "Mobile Bluetooth Printer" should now appear in the list. Select it.
5. Change **Paper Size** to **"58mm (Mobile/Bluetooth)"**.
6. Click **TEST PRINTER** to confirm connection.
7. Click **SAVE SETTINGS**.

Your Bluetooth printer is now ready to use!
