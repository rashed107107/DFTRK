# DFTRK (Distribution Tracking) System - Use Case Documentation

## 1. System Overview

DFTRK is a comprehensive distribution tracking system designed to facilitate the supply chain between wholesalers and retailers. The system manages product catalogs, inventory, orders, payments, and reporting across three user roles: Admin, Wholesaler, and Retailer.

## 2. User Roles and Responsibilities

### 2.1 Admin
- System administration and oversight
- User management (retailers and wholesalers)
- Product category management
- Access to comprehensive sales and payment reports
- Monitor overall system performance and transactions

### 2.2 Wholesaler
- Manage product catalog and inventory
- Set product pricing
- Process orders from retailers
- Track payments and revenue
- View wholesaler-specific performance reports

### 2.3 Retailer
- Browse wholesaler products
- Manage shopping cart
- Place orders from wholesalers
- Maintain inventory of purchased products
- Make payments for orders
- Track order status and payment history
- View retailer-specific performance reports

## 3. Core Use Cases

### 3.1 User Management

#### 3.1.1 User Registration (UC-UM-01)
**Actors:** Admin, Unregistered User  
**Description:** New users register with the system as either a retailer or wholesaler.  
**Flow:**
1. User enters registration details (username, email, password, business name, address)
2. System validates input and creates user account
3. Admin approves user account (optional)
4. User receives confirmation and can log in

#### 3.1.2 User Authentication (UC-UM-02)
**Actors:** All Users  
**Description:** Users log in to access system features based on their role.  
**Flow:**
1. User enters credentials (username/email and password)
2. System validates credentials and establishes session
3. User is redirected to appropriate dashboard based on role

### 3.2 Product Management

#### 3.2.1 Category Management (UC-PM-01)
**Actors:** Admin  
**Description:** Admin creates and manages product categories.  
**Flow:**
1. Admin creates/edits/deletes product categories
2. System updates category list visible to all users

#### 3.2.2 Base Product Management (UC-PM-02)
**Actors:** Admin  
**Description:** Admin manages the base product catalog.  
**Flow:**
1. Admin creates/edits/deletes base products with details (name, description, category)
2. System updates product catalog

#### 3.2.3 Wholesaler Product Management (UC-PM-03)
**Actors:** Wholesaler  
**Description:** Wholesalers create product listings with pricing and inventory.  
**Flow:**
1. Wholesaler selects base products to offer
2. Wholesaler sets pricing and inventory levels
3. Wholesaler activates/deactivates products
4. System makes products available to retailers

#### 3.2.4 Retailer Product Management (UC-PM-04)
**Actors:** Retailer  
**Description:** Retailers manage their inventory of purchased products.  
**Flow:**
1. Retailer views purchased products
2. Retailer sets selling price for end customers
3. Retailer updates inventory levels
4. Retailer adds notes to products

### 3.3 Order Management

#### 3.3.1 Shopping Cart Management (UC-OM-01)
**Actors:** Retailer  
**Description:** Retailers add products to cart before placing orders.  
**Flow:**
1. Retailer browses wholesaler products
2. Retailer adds products to cart with quantities
3. Retailer views cart contents
4. Retailer updates quantities or removes items

#### 3.3.2 Order Placement (UC-OM-02)
**Actors:** Retailer  
**Description:** Retailers place orders from their shopping cart.  
**Flow:**
1. Retailer reviews cart contents
2. Retailer proceeds to checkout
3. Retailer confirms order
4. System creates order and notifies wholesaler
5. System creates transaction record

#### 3.3.3 Order Processing (UC-OM-03)
**Actors:** Wholesaler  
**Description:** Wholesalers process orders from retailers.  
**Flow:**
1. Wholesaler views pending orders
2. Wholesaler updates order status (processing, shipped, etc.)
3. System updates order status and notifies retailer
4. Wholesaler's inventory is adjusted automatically

#### 3.3.4 Order Fulfillment (UC-OM-04)
**Actors:** Wholesaler, Retailer  
**Description:** Order progresses through fulfillment stages.  
**Flow:**
1. Wholesaler marks order as shipped
2. Retailer receives order and marks as delivered
3. Retailer's inventory is updated automatically
4. Order is marked as completed

### 3.4 Payment Management

#### 3.4.1 Payment Processing (UC-PM-01)
**Actors:** Retailer  
**Description:** Retailers make payments for orders.  
**Flow:**
1. Retailer views outstanding transactions
2. Retailer selects payment method
3. Retailer enters payment details and amount
4. System records payment and updates transaction status
5. Wholesaler is notified of payment

#### 3.4.2 Payment Tracking (UC-PM-02)
**Actors:** Wholesaler, Admin  
**Description:** Track payments received and outstanding balances.  
**Flow:**
1. Wholesaler/Admin views payment history
2. System displays payment details and transaction status
3. System calculates outstanding balances

#### 3.4.3 Partial Payments (UC-PM-03)
**Actors:** Retailer, Wholesaler  
**Description:** Retailers make partial payments on orders.  
**Flow:**
1. Retailer makes payment for part of order total
2. System updates transaction as partially paid
3. System tracks remaining balance
4. Retailer can make additional payments until fully paid

### 3.5 Reporting

#### 3.5.1 Admin Sales Reports (UC-RP-01)
**Actors:** Admin  
**Description:** Admin views comprehensive sales data across the platform.  
**Flow:**
1. Admin accesses sales reports
2. Admin applies filters (date range, retailer, wholesaler, status)
3. System displays order metrics, financial metrics, and charts
4. Admin can view top retailers, wholesalers, and products

#### 3.5.2 Admin Payment Reports (UC-RP-02)
**Actors:** Admin  
**Description:** Admin views payment data across the platform.  
**Flow:**
1. Admin accesses payment reports
2. Admin applies filters (date range, retailer, wholesaler, payment method)
3. System displays payment metrics, collection rates, and charts
4. Admin can view payment trends and outstanding balances

#### 3.5.3 Wholesaler Performance Reports (UC-RP-03)
**Actors:** Wholesaler  
**Description:** Wholesalers view their sales and performance data.  
**Flow:**
1. Wholesaler accesses performance reports
2. Wholesaler applies date filters
3. System displays order counts, revenue, outstanding payments
4. System shows top retailers and products
5. Charts visualize revenue trends

#### 3.5.4 Retailer Performance Reports (UC-RP-04)
**Actors:** Retailer  
**Description:** Retailers view their purchasing and payment history.  
**Flow:**
1. Retailer accesses performance reports
2. Retailer applies date filters
3. System displays order counts, total spent, outstanding balance
4. System shows top wholesalers and payment status
5. Charts visualize spending trends

### 3.6 Dashboard Views

#### 3.6.1 Admin Dashboard (UC-DV-01)
**Actors:** Admin  
**Description:** Admin views system-wide metrics and recent activity.  
**Flow:**
1. Admin logs in
2. System displays user counts, order counts, total revenue
3. System shows recent orders across the platform

#### 3.6.2 Wholesaler Dashboard (UC-DV-02)
**Actors:** Wholesaler  
**Description:** Wholesalers view their business metrics and recent orders.  
**Flow:**
1. Wholesaler logs in
2. System displays product count, pending orders, completed orders
3. System shows total revenue and low stock alerts
4. System displays recent orders from retailers

#### 3.6.3 Retailer Dashboard (UC-DV-03)
**Actors:** Retailer  
**Description:** Retailers view inventory metrics and order history.  
**Flow:**
1. Retailer logs in
2. System displays product count, inventory value, potential sales value
3. System shows low stock and out of stock counts
4. System displays recent orders and top products
5. Cart item count is visible for easy access

## 4. Business Rules

### 4.1 Order Processing
- BR-01: Orders start in "Pending" status
- BR-02: Orders follow a defined workflow: Pending → Processing → Shipped → Delivered → Completed
- BR-03: Orders can be cancelled at any stage before "Delivered"
- BR-04: Wholesaler inventory is reduced when an order is placed

### 4.2 Payment Processing
- BR-05: Transactions start as "Pending" until payment is received
- BR-06: Partial payments are allowed, updating status to "PartiallyPaid"
- BR-07: When full payment is received, transaction status becomes "Completed"
- BR-08: Multiple payment methods are supported (CreditCard, BankTransfer, Cash, Other)

### 4.3 Inventory Management
- BR-09: Retailer inventory increases when orders are marked as "Delivered"
- BR-10: Low stock alerts trigger when inventory falls below defined thresholds
- BR-11: Products can be marked as inactive if out of stock

### 4.4 Pricing
- BR-12: Wholesalers set their own product prices
- BR-13: Retailers set their own selling prices independent of purchase price
- BR-14: System calculates inventory value based on purchase price
- BR-15: System calculates potential sales value based on selling price

## 5. Technical Requirements

### 5.1 Security
- TR-01: Role-based access control for all features
- TR-02: Secure authentication using ASP.NET Identity
- TR-03: Data validation for all user inputs

### 5.2 Performance
- TR-04: Efficient database queries for reporting features
- TR-05: Pagination for large data sets
- TR-06: Optimized dashboard loading

### 5.3 Usability
- TR-07: Responsive design for all device types
- TR-08: Intuitive navigation between related features
- TR-09: Clear visualization of data through charts and graphs

## 6. Integration Points

### 6.1 Data Import/Export
- IP-01: Export reports to common formats (CSV, PDF)
- IP-02: Batch import of product data

### 6.2 Payment Gateway
- IP-03: Integration with payment processing services
- IP-04: Support for multiple payment methods

## 7. Exception Handling

### 7.1 Order Exceptions
- EH-01: Handle insufficient inventory during order placement
- EH-02: Manage order cancellation and impact on inventory

### 7.2 Payment Exceptions
- EH-03: Handle failed payment attempts
- EH-04: Process payment refunds when necessary

### 7.3 System Exceptions
- EH-05: Graceful error handling with user-friendly messages
- EH-06: Comprehensive logging for troubleshooting

## 8. Reporting and Analytics

### 8.1 Financial Reports
- RP-01: Revenue tracking by time period
- RP-02: Outstanding payment tracking
- RP-03: Collection rate analysis

### 8.2 Operational Reports
- RP-04: Order status distribution
- RP-05: Order fulfillment time analysis
- RP-06: Inventory turnover metrics

### 8.3 User Activity Reports
- RP-07: Top retailers by order volume and value
- RP-08: Top wholesalers by order volume and value
- RP-09: Top products by sales volume and value

## 9. Future Enhancements

### 9.1 Advanced Features
- FE-01: Automated reordering based on inventory levels
- FE-02: Enhanced product recommendation system
- FE-03: Mobile application for on-the-go management

### 9.2 Integration Opportunities
- FE-04: Integration with shipping providers
- FE-05: Integration with accounting systems
- FE-06: Integration with customer relationship management systems

## Appendix A: Entity Relationship Diagram

The DFTRK system is built around the following key entities:

- **ApplicationUser**: Represents users in the system (Admin, Wholesaler, Retailer)
- **Product**: Base product information shared across the system
- **Category**: Classification for products
- **WholesalerProduct**: Products offered by wholesalers with pricing and inventory
- **RetailerProduct**: Products in retailer inventory with pricing
- **Order**: Orders placed by retailers to wholesalers
- **OrderItem**: Individual line items in an order
- **Cart**: Shopping cart for retailers
- **CartItem**: Individual items in a shopping cart
- **Transaction**: Financial transaction record for an order
- **Payment**: Individual payment records against a transaction

## Appendix B: Order Status Flow

```
Pending → Processing → Shipped → Delivered → Completed
   ↓
Cancelled
```

## Appendix C: Transaction Status Flow

```
Pending → PartiallyPaid → Completed
   ↓            ↓
Failed       Refunded
``` 