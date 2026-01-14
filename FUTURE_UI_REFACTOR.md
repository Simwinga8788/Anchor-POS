# Single-Window UI Refactor Plan

## Current State
- Multi-window design (Main POS, Products, Reports as separate windows)
- All windows open maximized
- Fully functional features

## Goal
- Single unified window with navigation
- Content switching instead of new windows
- Modern single-page application feel

## Implementation Steps

### 1. Create UserControls
Convert existing window content to reusable controls:
- `Views/Controls/POSControl.xaml` - Current main POS content
- `Views/Controls/ProductsControl.xaml` - Product management content
- `Views/Controls/ReportsControl.xaml` - Reports content

### 2. Modify MainWindow
- Add TabControl or ContentControl for view switching
- Add navigation buttons/menu
- Implement view switching logic in code-behind

### 3. Update Navigation Logic
- Change button click handlers to switch views instead of opening windows
- Maintain state between view switches
- Handle data refresh when switching views

### 4. Testing Checklist
- [ ] Sales processing works in new layout
- [ ] Product management CRUD operations
- [ ] Reports generate correctly
- [ ] Excel export functionality
- [ ] All existing features preserved

## Estimated Effort
- 4-6 hours of development
- 2-3 hours of testing
- Risk: Medium (UI restructure could introduce bugs)

## Benefits
- Better user experience (no window juggling)
- More modern interface
- Easier to add new features
- Consistent navigation

## Additional Future Enhancements
- **Individual Cart Item Removal**: Add "X" button next to each cart item to remove individual items instead of clearing entire cart
- **Quantity Adjustment in Cart**: Allow users to adjust quantity directly in cart without re-adding items

## Notes
- Keep backup of current working version
- Test thoroughly before deployment
- Consider implementing in phases
