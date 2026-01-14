# Surf POS - System Implementation Plan

## 1. Technical Architecture
**Platform**: Windows Desktop Application (.NET 10.0)
**Database**: SQL Server Express (Local)
**ORM**: Entity Framework Core
**UI Framework**: WPF (Windows Presentation Foundation) with Material Design
**Architecture Pattern**: MVVM (Model-View-ViewModel)

### Solution Structure
1. **SurfPOS.Core**: Domain models, Interfaces, DTOs, Business Rules.
2. **SurfPOS.Data**: EF Core Context, Migrations, Repositories.
3. **SurfPOS.Services**: Business logic implementation (Auth, Sales, Inventory, Reporting).
4. **SurfPOS.Desktop**: WPF Application (Views, ViewModels, Resources).
5. **SurfPOS.Worker**: Background Worker Service (Windows Service) for automation.

---

## 2. Database Schema Strategy

### Key Entities
- **User**: `Id`, `Username`, `PasswordHash`, `Role` (Admin/Salesperson), `LastLogin`, `IsActive`.
- **Product**: `Id`, `Name`, `Barcode` (Unique Index), `Price`, `CostPrice`, `StockQuantity`, `LowStockThreshold`, `Category`, `IsActive`.
- **Transaction**: `Id`, `TransactionRef`, `Date`, `TotalAmount`, `PaymentMethod`, `UserId`, `ShiftId`.
- **TransactionItem**: `Id`, `TransactionId`, `ProductId`, `Quantity`, `UnitPrice`.
- **StockLog**: `Id`, `ProductId`, `ChangeAmount`, `NewQuantity`, `Reason`, `UserId`, `Date`.
- **Shift**: `Id`, `UserId`, `StartTime`, `EndTime`, `CashStart`, `CashEnd`, `TotalSales`.
- **AppSetting**: `Key`, `Value` (Config for Email, Printer, WhatsApp).

---

## 3. Implementation Phases

### Phase 1: Foundation & Data Layer
1.  **Project Setup**: Initialize `.sln` and component projects.
2.  **Domain & Data**: Define entities and setting up EF Core with SQL Server.
3.  **Basic Services**: Generic Repository pattern and Unit of Work.
4.  **Seed Data**: Default Admin account and sample Categories.

### Phase 2: Core Business Services
1.  **Authentication**: Login logic, Password hashing (BCrypt), Session tracking.
2.  **Inventory Management**: 
    - CRUD operations for Products.
    - **Logic**: Auto-generate Barcode string if empty.
3.  **Barcode Service**: 
    - Generate Barcode images (using `NetBarcode` or `ZXing`).
    - Label printing logic (FlowDocument or bitmap to printer).
4.  **Sales Engine**:
    - Cart logic (Add/Remove item, Calculate Totals).
    - Checkout transaction (Atomic operation: Save Sale -> Deduct Stock).

### Phase 3: Desktop UI (WPF)
1.  **Design System**: Setup `MaterialDesignInXamlBehavior` for modern look.
2.  **Shell**: Main Window with Navigation (Admin vs Salesperson view).
3.  **Modules**:
    - **Login View**: Simple, secure entry.
    - **POS View**: High contrast, large text, optimized for barcode scanning (keyboard input handling).
    - **Inventory View**: DataGrid with search/filter, color coding for low stock.
    - **Reporting View**: Dashboard with charts (LiveCharts) and Daily Summary.

### Phase 4: Automation (Worker Service)
1.  **Monitor Job**: Runs every 5 minutes.
    - Checks `Product.StockQuantity <= Product.LowStockThreshold`.
    - Triggers Alert Service if condition met.
2.  **Reporting Job**: Runs at configured times (e.g., Midnight).
    - Generates PDF Report (using `QuestPDF`).
3.  **Notification Service**:
    - Email (SMTP).
    - WhatsApp (Twilio API or alternative).
    - Offline Queue: Store notifications in DB if no internet, retry later.

### Phase 5: Hardware & Integration
1.  **Receipt Printing**: Integration with ESC/POS commands for thermal printers.
2.  **Barcode Scanner**: Ensure UI focus handling for global scan events.
3.  **Label Printer**: Custom page sizes for barcode stickers.

### Phase 6: Polish & Deployment
1.  **Error Handling**: Global exception logging (Serilog).
2.  **Installer**: Create Setup (MSI or Squirrel) for easy installation on client machine.
3.  **User Manual**: Basic usage guide.

---

## 4. Dependencies & Tools
- **UI**: MaterialDesignThemes (NuGet).
- **ORM**: Microsoft.EntityFrameworkCore.SqlServer.
- **Reporting**: QuestPDF.
- **Charts**: LiveCharts.Wpf.
- **Barcodes**: NetBarcode.
- **Logging**: Serilog.
