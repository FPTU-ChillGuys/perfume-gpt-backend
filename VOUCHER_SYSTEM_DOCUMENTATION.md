# Voucher Management System

This document describes the voucher management system implementation including CRUD operations and order integration.

## Overview

The voucher system allows:
- **Admins** to create, update, delete, and manage vouchers
- **Users** to redeem vouchers using loyalty points
- **Users** to apply vouchers to orders for discounts
- Automatic voucher validation and usage tracking

## Architecture

### Entities

#### Voucher
- `Id` (Guid): Primary key
- `Code` (string): Unique voucher code
- `DiscountValue` (decimal): Discount amount or percentage
- `DiscountType` (enum): Percentage or FixedAmount
- `RequiredPoints` (long): Loyalty points needed to redeem
- `MinOrderValue` (decimal): Minimum order amount to use voucher
- `ExpiryDate` (DateTime): Voucher expiration date

#### UserVoucher
- `Id` (Guid): Primary key
- `UserId` (Guid): Foreign key to User
- `VoucherId` (Guid): Foreign key to Voucher
- `IsUsed` (bool): Whether voucher has been used

## API Endpoints

### Admin Endpoints (Requires Admin Role)

#### Create Voucher
```http
POST /api/vouchers
Content-Type: application/json

{
  "code": "SUMMER2024",
  "discountValue": 20,
  "discountType": 0,  // 0 = Percentage, 1 = FixedAmount
  "requiredPoints": 1000,
  "minOrderValue": 100.00,
  "expiryDate": "2024-12-31T23:59:59Z"
}
```

#### Update Voucher
```http
PUT /api/vouchers/{voucherId}
Content-Type: application/json

{
  "code": "SUMMER2024-UPDATED",
  "discountValue": 25,
  "expiryDate": "2024-12-31T23:59:59Z"
}
```

#### Delete Voucher
```http
DELETE /api/vouchers/{voucherId}
```

#### Get Voucher Details
```http
GET /api/vouchers/{voucherId}
```

#### Get Paginated Vouchers
```http
GET /api/vouchers?pageNumber=1&pageSize=10&isExpired=false&code=SUMMER
```

### User Endpoints (Requires Authentication)

#### Redeem Voucher
```http
POST /api/vouchers/redeem
Content-Type: application/json

{
  "voucherId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Logic:**
1. Validates voucher exists and is not expired
2. Checks if user already redeemed this voucher
3. Verifies user has enough loyalty points
4. Deducts required points from user's loyalty account
5. Creates UserVoucher record

#### Get My Vouchers
```http
GET /api/vouchers/my-vouchers?pageNumber=1&pageSize=10
```

Returns user's redeemed vouchers with usage status.

#### Apply Voucher to Order
```http
POST /api/vouchers/apply
Content-Type: application/json

{
  "voucherCode": "SUMMER2024",
  "orderAmount": 500.00
}
```

**Returns:**
```json
{
  "voucherId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "code": "SUMMER2024",
  "discountAmount": 100.00,
  "finalAmount": 400.00,
  "discountType": "Percentage"
}
```

**Logic:**
1. Validates voucher code exists and is not expired
2. Checks if order amount meets minimum requirement
3. Verifies user owns the voucher and hasn't used it
4. Calculates discount based on type:
   - **Percentage**: `orderAmount * (discountValue / 100)`
   - **FixedAmount**: `discountValue`
5. Ensures discount doesn't exceed order amount
6. Returns calculated discount and final amount

#### Validate Voucher
```http
GET /api/vouchers/validate/{voucherCode}
```

Checks if a voucher is valid for the current user.

## Service Methods

### IVoucherService

```csharp
// Admin operations
Task<BaseResponse<string>> CreateVoucherAsync(CreateVoucherRequest request);
Task<BaseResponse<string>> UpdateVoucherAsync(Guid voucherId, UpdateVoucherRequest request);
Task<BaseResponse<string>> DeleteVoucherAsync(Guid voucherId);
Task<BaseResponse<VoucherResponse>> GetVoucherAsync(Guid voucherId);
Task<BaseResponse<PagedResult<VoucherResponse>>> GetVouchersAsync(GetPagedVouchersRequest request);

// User operations
Task<BaseResponse<string>> RedeemVoucherAsync(Guid userId, RedeemVoucherRequest request);
Task<BaseResponse<PagedResult<UserVoucherResponse>>> GetUserVouchersAsync(Guid userId, int pageNumber, int pageSize);

// Apply voucher logic
Task<BaseResponse<ApplyVoucherResponse>> ApplyVoucherToOrderAsync(Guid userId, ApplyVoucherRequest request);
Task<BaseResponse<bool>> ValidateToApplyVoucherAsync(string voucherCode, Guid userId);
Task<BaseResponse<bool>> MarkVoucherAsUsedAsync(Guid userId, Guid voucherId);
```

## Integration with Order System

When creating an order with a voucher:

1. **Order Preview/Calculation:**
   ```csharp
   var applyResult = await _voucherService.ApplyVoucherToOrderAsync(userId, new ApplyVoucherRequest 
   {
       VoucherCode = "SUMMER2024",
       OrderAmount = totalAmount
   });
   ```

2. **Order Creation:**
   - Include `VoucherId` in `CreateOrderRequest`
   - The Order entity already has a `VoucherId` field and navigation property

3. **Order Completion:**
   ```csharp
   if (order.VoucherId.HasValue)
   {
       await _voucherService.MarkVoucherAsUsedAsync(order.CustomerId.Value, order.VoucherId.Value);
   }
   ```

## Validation Rules

### CreateVoucherRequest
- Code: Required, max 50 chars, uppercase letters/numbers/hyphens/underscores only
- DiscountValue: Must be > 0
- DiscountType: Must be valid enum value
- RequiredPoints: Must be ? 0
- MinOrderValue: Must be ? 0
- ExpiryDate: Must be in the future
- Percentage discount: Cannot exceed 100%

### UpdateVoucherRequest
- All fields optional but follow same rules as create when provided

### ApplyVoucherRequest
- VoucherCode: Required
- OrderAmount: Must be > 0

## Business Rules

1. **Voucher Codes:**
   - Must be unique
   - Automatically converted to uppercase
   - Can only contain letters, numbers, hyphens, and underscores

2. **Redemption:**
   - User can only redeem each voucher once
   - Must have sufficient loyalty points
   - Points are deducted immediately upon redemption

3. **Usage:**
   - Voucher must be owned by user (redeemed)
   - Cannot be used if already marked as used
   - Cannot be used if expired
   - Order amount must meet minimum requirement
   - Can only be used once per order

4. **Discount Calculation:**
   - Percentage discounts: Applied to order total
   - Fixed amount discounts: Deducted from order total
   - Discount never exceeds order amount

## Response Types

### VoucherResponse
```csharp
{
  "id": "guid",
  "code": "SUMMER2024",
  "discountValue": 20,
  "discountType": "Percentage",
  "requiredPoints": 1000,
  "minOrderValue": 100.00,
  "expiryDate": "2024-12-31T23:59:59Z",
  "isExpired": false,
  "createdAt": "2024-01-01T00:00:00Z"
}
```

### UserVoucherResponse
```csharp
{
  "id": "guid",
  "voucherId": "guid",
  "code": "SUMMER2024",
  "discountValue": 20,
  "discountType": "Percentage",
  "minOrderValue": 100.00,
  "expiryDate": "2024-12-31T23:59:59Z",
  "isUsed": false,
  "isExpired": false,
  "redeemedAt": "2024-01-15T10:30:00Z"
}
```

### ApplyVoucherResponse
```csharp
{
  "voucherId": "guid",
  "code": "SUMMER2024",
  "discountAmount": 100.00,
  "finalAmount": 400.00,
  "discountType": "Percentage"
}
```

## Error Handling

Common error scenarios:
- `404 NotFound`: Voucher code doesn't exist
- `400 BadRequest`: Voucher expired, insufficient points, order below minimum
- `409 Conflict`: Voucher code already exists, voucher already redeemed
- `401 Unauthorized`: User not authenticated
- `403 Forbidden`: User doesn't have required role (Admin endpoints)

## Database Repositories

The system uses existing repositories:
- `IVoucherRepository`: For Voucher entity operations
- `IUserVoucherRepository`: For UserVoucher entity operations
- `ILoyaltyPointRepository`: For managing loyalty points during redemption
- `IUnitOfWork`: For transaction management

## Testing Recommendations

1. **Unit Tests:**
   - Voucher creation with duplicate codes
   - Discount calculations (percentage and fixed)
   - Expiry date validation
   - Loyalty points deduction
   - Voucher usage marking

2. **Integration Tests:**
   - Complete redemption flow
   - Order checkout with voucher
   - Concurrent voucher usage attempts
   - Transaction rollback scenarios

## Future Enhancements

Potential improvements:
- Voucher usage limits (max uses per voucher)
- User-specific vouchers (targeted promotions)
- Bulk voucher generation
- Voucher categories/tags
- Usage analytics and reporting
- Automatic expiry notifications
- Voucher stacking rules
