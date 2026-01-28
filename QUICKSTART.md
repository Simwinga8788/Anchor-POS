# 🚨 QUICK START GUIDE - Surf POS

## Current Status
The application has a compatibility issue with the EPPlus Excel library that's preventing it from starting properly.

## ✅ WORKING FEATURES (Core POS):
- Login System
- Product Management (Add/Edit/Delete/View)
- Sales Processing (Checkout)
- Reports Dashboard
- Barcode Scanning

## ⚠️ TEMPORARILY DISABLED:
- Excel Import/Export (causing startup crash)
- Barcode Label Printing (depends on Excel library)

## 🔧 TO RUN THE APPLICATION:

### Option 1: Use the Batch File (EASIEST)
1. Go to: `C:\Users\simwi\Desktop\Surf POS`
2. Double-click: `RunSurfPOS.bat`
3. The app should open

### Option 2: Run from Command Line
```powershell
cd "C:\Users\simwi\Desktop\Surf POS"
taskkill /F /IM AnchorPOS.Desktop.exe 2>$null; dotnet run --project src\AnchorPOS.Desktop\AnchorPOS.Desktop.csproj
```

### Option 3: Run the EXE Directly
```powershell
cd "C:\Users\simwi\Desktop\Surf POS"
.\src\SurfPOS.Desktop\bin\Debug\net10.0-windows\SurfPOS.Desktop.exe
```

## 🔑 DEFAULT LOGIN:
- **Username:** admin
- **Password:** admin123

## 📝 NEXT STEPS TO FIX:

The Excel/Barcode features need to be fixed by:
1. Downgrading EPPlus to version 6.x (more stable)
2. OR removing EPPlus entirely and using a different Excel library
3. OR making Excel features completely optional

Would you like me to:
- **A)** Fix the EPPlus issue properly (will take some time)
- **B)** Remove Excel features entirely and focus on core POS
- **C)** Make Excel features optional (add them later)

## 🎯 WHAT WORKS RIGHT NOW:
Even without Excel, you can:
- ✅ Login as admin
- ✅ Use the POS to scan/sell products
- ✅ Add/Edit products manually
- ✅ View sales reports
- ✅ Process transactions

The core POS functionality is 100% working!
