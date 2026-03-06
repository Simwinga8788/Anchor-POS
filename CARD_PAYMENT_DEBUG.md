# Card Payment Checkout Issue - Debugging Guide

## Problem Description
When selecting "Card" as the payment method and clicking Checkout, the transaction fails with "Checkout failed" error. Other payment methods (Cash, Mobile Money) work correctly.

## Changes Made for Debugging

### 1. Enhanced Error Logging in MainWindow.xaml.cs
Added comprehensive error logging to the `CheckoutButton_Click` method:
- Logs the selected payment method index
- Logs the resolved PaymentMethod enum value
- Logs cart item count
- Displays full error details including inner exceptions

### 2. Improved Error Messages
The error dialog now shows:
- Main error message
- Inner exception details
- Instruction to check debug output

## Testing Steps

1. **Run the application**:
   ```powershell
   dotnet run --project src\AnchorPOS.Desktop\AnchorPOS.Desktop.csproj
   ```

2. **Login** with your credentials

3. **Add a product to cart**

4. **Select "Card" from Payment Method dropdown**

5. **Click CHECKOUT button**

6. **Note the error message** - especially the "Details" section

7. **Check Debug Output** in Visual Studio or the console for detailed logs

## Possible Causes

### Theory 1: Null Reference in Transaction
The transaction object might be null or missing required properties when using Card payment.

**Check**: Look for "Object reference not set to an instance of an object" in the error

### Theory 2: Database Constraint
There might be a database constraint or validation rule that rejects Card payments.

**Check**: Look for SQL or database-related errors

### Theory 3: Receipt Preview Dialog Issue
The ReceiptPreviewDialog might fail to initialize with Card payment transactions.

**Check**: Look for errors related to ReceiptPreviewDialog or printer service

### Theory 4: Enum Mapping Issue
The PaymentMethod enum value might not be correctly mapped or stored.

**Check**: Debug output should show "Payment Method: Card"

## Debug Output to Look For

When you click Checkout with Card selected, you should see:
```
Selected Index: 1
Payment Method: Card
Cart Items: [number]
```

If you see an error, it will show:
```
CHECKOUT ERROR: [error message]
Stack Trace: [stack trace]
Inner Exception: [inner exception if any]
```

## Quick Fixes to Try

### Fix 1: Check Database
Ensure the database allows Card as a payment method:
```sql
SELECT * FROM Transactions WHERE PaymentMethod = 1;
```

### Fix 2: Verify Enum Values
Ensure the enum values match:
- Cash = 0
- Card = 1
- MobileMoney = 2

### Fix 3: Test with Simple Transaction
Try with a single item, low quantity to rule out stock or calculation issues.

## Code Locations

- **Checkout Logic**: `src/AnchorPOS.Desktop/MainWindow.xaml.cs` (line 279-360)
- **Sales Service**: `src/AnchorPOS.Services/SalesService.cs` (line 19-116)
- **Payment Method Enum**: `src/AnchorPOS.Core/Entities/Transaction.cs` (line 7-12)
- **Receipt Preview**: `src/AnchorPOS.Desktop/Views/ReceiptPreviewDialog.xaml.cs`

## Next Steps

1. **Run the test** and get the exact error message
2. **Check the debug output** for detailed logging
3. **Share the error details** so we can identify the root cause
4. **Apply the appropriate fix** based on the error

## Contact Information

If the error persists after trying these steps, please provide:
- The exact error message from the dialog
- The debug output from the console
- Any stack trace information
- The payment method index and enum value from the debug output

---

*Last Updated: 2026-02-09*
*Status: Awaiting test results*
