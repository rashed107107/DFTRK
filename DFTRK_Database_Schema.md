# DFTRK - Database Schema Documentation

## Overview
The DFTRK (Distribution & Food Track) system is a comprehensive business management platform that facilitates relationships between wholesalers, retailers, and external customers. The database schema supports multi-tenant operations with role-based access control.

## Architecture Summary

### Core Business Flow
1. **User Management**: Role-based system (Admin, Wholesaler, Retailer)
2. **Product Catalog**: Category → Product → WholesalerProduct → RetailerProduct
3. **Order Processing**: Cart → Order → Transaction → Payment
4. **Partnership Management**: Support for retailer-wholesaler partnerships
5. **External Operations**: Wholesaler orders for non-system customers

## Entity Details

### 👤 User Management

#### ApplicationUser
- **Purpose**: Core user entity extending ASP.NET Identity
- **Roles**: Admin, Wholesaler, Retailer
- **Key Fields**:
  - `Id` (PK): Identity user ID
  - `BusinessName`: Company/business name
  - `Address`, `City`, `State`, `PostalCode`, `Country`: Location info
  - `TaxId`: Business tax identification
  - `UserType`: Enum (Admin/Wholesaler/Retailer)

### 📦 Product Management

#### Category
- **Purpose**: Product categorization
- **Key Fields**:
  - `Id` (PK): Auto-increment
  - `Name`: Category name
  - `CreatedById` (FK): User who created the category

#### Product
- **Purpose**: Master product catalog
- **Key Fields**:
  - `Id` (PK): Auto-increment
  - `Name`, `Description`: Product details
  - `SKU`: Stock keeping unit (optional)
  - `CategoryId` (FK): Reference to Category
  - `ImageUrl`: Product image path

#### WholesalerProduct
- **Purpose**: Wholesaler's inventory and pricing
- **Key Fields**:
  - `Id` (PK): Auto-increment
  - `ProductId` (FK): Reference to Product
  - `WholesalerId` (FK): Reference to ApplicationUser
  - `Price`: Wholesale price
  - `StockQuantity`: Available inventory
  - `IsActive`: Availability flag

#### RetailerProduct
- **Purpose**: Retailer's purchased inventory
- **Key Fields**:
  - `Id` (PK): Auto-increment
  - `WholesalerProductId` (FK): Source product (nullable)
  - `RetailerId` (FK): Reference to ApplicationUser
  - `PurchasePrice`: Cost from wholesaler
  - `SellingPrice`: Retail price
  - `StockQuantity`: Current inventory
  - `Notes`: Additional information

### 🛒 Order Management

#### Cart
- **Purpose**: Shopping cart for retailers
- **Key Fields**:
  - `Id` (PK): Auto-increment
  - `RetailerId` (FK): Reference to ApplicationUser
  - `CreatedAt`: Creation timestamp

#### CartItem
- **Purpose**: Individual items in cart
- **Key Fields**:
  - `Id` (PK): Auto-increment
  - `CartId` (FK): Reference to Cart
  - `WholesalerProductId` (FK): Reference to WholesalerProduct
  - `Quantity`: Requested quantity
  - `Price`: Unit price at time of addition

#### Order
- **Purpose**: Purchase orders between retailers and wholesalers
- **Key Fields**:
  - `Id` (PK): Auto-increment
  - `RetailerId` (FK): Reference to ApplicationUser (nullable for external orders)
  - `WholesalerId` (FK): Reference to ApplicationUser
  - `OrderDate`: Order creation date
  - `Status`: Enum (Pending/Processing/Shipped/Delivered/Completed/Cancelled)
  - `TotalAmount`: Total order value
  - `Notes`: Order-specific information

#### OrderItem
- **Purpose**: Line items within orders
- **Key Fields**:
  - `Id` (PK): Auto-increment
  - `OrderId` (FK): Reference to Order
  - `WholesalerProductId` (FK): Reference to WholesalerProduct (nullable)
  - `PartnerProductId`: For partnership products
  - `ProductName`: For partnership products
  - `Quantity`: Ordered quantity
  - `UnitPrice`: Price per unit
  - `Subtotal`: Line total

### 💳 Financial Management

#### Transaction
- **Purpose**: Financial transactions for orders
- **Key Fields**:
  - `Id` (PK): Auto-increment
  - `OrderId` (FK): Reference to Order (One-to-One)
  - `RetailerId` (FK): Reference to ApplicationUser (nullable)
  - `WholesalerId` (FK): Reference to ApplicationUser
  - `Amount`: Total transaction amount
  - `AmountPaid`: Amount paid so far
  - `TransactionDate`: Transaction date
  - `Status`: Enum (Pending/PartiallyPaid/Completed/Failed/Refunded)
  - `PaymentMethod`: Payment method used
  - `TransactionReference`: External reference

#### Payment
- **Purpose**: Individual payment records
- **Key Fields**:
  - `Id` (PK): Auto-increment
  - `TransactionId` (FK): Reference to Transaction
  - `Amount`: Payment amount
  - `PaymentDate`: Payment date
  - `Method`: Enum (CreditCard/BankTransfer/Cash/Other)
  - `ReferenceNumber`: Payment reference
  - `Notes`: Payment notes

### 🤝 Partnership Management

#### RetailerPartnership
- **Purpose**: Partnerships between retailers and external wholesalers
- **Key Fields**:
  - `Id` (PK): Auto-increment
  - `RetailerId` (FK): Reference to ApplicationUser
  - `WholesalerName`: External wholesaler name
  - `PartnershipName`: Partnership identifier
  - `Notes`: Partnership details
  - `CreatedDate`: Partnership creation date
  - `IsActive`: Partnership status

#### RetailerPartnerCategory
- **Purpose**: Categories within partnerships
- **Key Fields**:
  - `Id` (PK): Auto-increment
  - `PartnershipId` (FK): Reference to RetailerPartnership
  - `Name`: Category name
  - `Description`: Category description
  - `CreatedDate`: Creation date
  - `IsActive`: Category status

#### RetailerPartnerProduct
- **Purpose**: Products within partnership categories
- **Key Fields**:
  - `Id` (PK): Auto-increment
  - `PartnershipId` (FK): Reference to RetailerPartnership
  - `CategoryId` (FK): Reference to RetailerPartnerCategory
  - `Name`: Product name
  - `Description`: Product description
  - `Price`: Product price
  - `StockQuantity`: Available quantity
  - `Notes`: Product notes
  - `CreatedDate`: Creation date
  - `IsActive`: Product status

### 🏪 External Operations

#### WholesalerExternalRetailer
- **Purpose**: External retailers not in the system
- **Key Fields**:
  - `Id` (PK): Auto-increment
  - `WholesalerId` (FK): Reference to ApplicationUser
  - `RetailerName`: External retailer name
  - `ContactInfo`: Contact information
  - `Notes`: Additional notes
  - `IsActive`: Retailer status
  - `CreatedDate`: Creation date

## Key Relationships

### Primary Relationships
1. **ApplicationUser** → **WholesalerProduct** (One-to-Many): Wholesalers own products
2. **ApplicationUser** → **RetailerProduct** (One-to-Many): Retailers purchase products
3. **Product** → **WholesalerProduct** (One-to-Many): Products sold by multiple wholesalers
4. **Order** → **Transaction** (One-to-One): Each order has one transaction
5. **Transaction** → **Payment** (One-to-Many): Transactions can have multiple payments

### Special Relationships
- **Order.RetailerId**: Nullable to support external orders
- **OrderItem.WholesalerProductId**: Nullable to support partnership products
- **Transaction.RetailerId**: Nullable for external retailer transactions

## Business Rules

### Order Processing
1. Internal orders require valid RetailerId
2. External orders have RetailerId = null
3. Partnership orders use PartnerProductId instead of WholesalerProductId
4. Each order must have exactly one transaction
5. Transactions can have multiple partial payments

### Payment System
1. Transaction.AmountPaid = Sum of all Payment.Amount for that transaction
2. Transaction status updates based on payment completion
3. Full payment changes status to Completed
4. Partial payment changes status to PartiallyPaid

### User Roles
- **Admin**: Full system access
- **Wholesaler**: Manage products, view orders, handle payments
- **Retailer**: Browse products, place orders, manage inventory

## Database Configuration Notes

### Entity Framework Specific
- **ApplicationUser**: Extends IdentityUser for ASP.NET Identity
- **Order.Transaction**: Navigation property ignored in DbContext to prevent conflicts
- **Transaction loading**: Manual loading required due to relationship configuration
- **Duplicate handling**: GroupBy used to handle multiple transactions per order

### Performance Considerations
- Indexes on foreign keys for optimal query performance
- Navigation properties configured for efficient loading
- NotMapped attributes used to prevent unnecessary EF tracking

---

**Generated**: $(date)
**System**: DFTRK (Distribution & Food Track)
**Version**: Production Schema 