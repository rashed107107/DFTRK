# DFTRK Component Design Documentation

## 3.1 Project Overview

### 3.1.1 Project Background / Purpose

The DFTRK (Distribution Tracking) system is designed to address the challenges faced in the distribution supply chain between wholesalers and retailers. The primary purpose of the system is to:

- Streamline the order and fulfillment process between wholesalers and retailers
- Provide comprehensive inventory management for both parties
- Facilitate transparent financial transactions and payment tracking
- Offer detailed reporting and analytics for business decision-making
- Create a unified platform for product catalog management
- Improve supply chain visibility and operational efficiency

The system aims to replace manual processes, spreadsheets, and disconnected systems with a centralized platform that connects all stakeholders in the distribution chain.

### 3.1.2 Hosting Platform

- **Application Server**: Microsoft IIS on Windows Server
- **Database Server**: Microsoft SQL Server
- **Deployment Model**: On-premises or cloud-based (Azure App Service)
- **Scaling Strategy**: Vertical scaling for initial deployment with horizontal scaling capabilities for future growth
- **Backup and Recovery**: Automated daily database backups with point-in-time recovery
- **Monitoring**: Application Insights for performance monitoring and error tracking
- **Security**: SSL/TLS encryption, network security groups, and role-based access control

## 3.2 System Architecture

### 3.2.1 Architectural Design

The DFTRK system follows a multi-tier architecture:

1. **Presentation Layer**:
   - ASP.NET Core MVC for web interface
   - Responsive design using Bootstrap
   - JavaScript for client-side interactivity

2. **Application Layer**:
   - Controllers for handling HTTP requests
   - Services for business logic implementation
   - Authentication and authorization services

3. **Data Access Layer**:
   - Entity Framework Core for ORM
   - Repository pattern for data access abstraction
   - LINQ for data querying

4. **Database Layer**:
   - SQL Server relational database
   - Stored procedures for complex operations
   - Indexes for performance optimization

### 3.2.2 Decomposition Description

The system is decomposed into the following major components:

1. **User Management Component**:
   - User registration and authentication
   - Role management
   - Profile management

2. **Product Management Component**:
   - Category management
   - Base product management
   - Wholesaler product listings
   - Retailer inventory management

3. **Order Management Component**:
   - Shopping cart functionality
   - Order creation and processing
   - Order status tracking
   - Order history

4. **Payment Management Component**:
   - Transaction creation
   - Payment processing
   - Payment history
   - Outstanding balance tracking

5. **Reporting Component**:
   - Sales reporting
   - Payment reporting
   - Inventory reporting
   - User activity reporting

### 3.2.3 Design Rationale

The architectural design decisions were made based on the following rationales:

- **ASP.NET Core MVC**: Provides a structured approach to web application development with separation of concerns
- **Entity Framework Core**: Simplifies data access and provides an object-oriented approach to database operations
- **Multi-tier Architecture**: Enhances maintainability, scalability, and security through separation of concerns
- **Repository Pattern**: Abstracts data access logic and facilitates unit testing
- **Role-based Access Control**: Ensures users can only access features appropriate to their role
- **Responsive Design**: Ensures the application is usable across various devices and screen sizes

## 3.3 Data Design

### 3.3.1 Data Description

The data model is designed to support the core business processes of the distribution chain:

- **User Data**: Information about system users, including role-specific details
- **Product Data**: Base product information and category classifications
- **Inventory Data**: Stock levels for wholesalers and retailers
- **Order Data**: Order details, line items, and status information
- **Financial Data**: Transactions, payments, and payment methods
- **Reporting Data**: Aggregated data for business intelligence and reporting

### 3.3.2 Data Dictionary

**ApplicationUser**
| Field | Type | Description |
|-------|------|-------------|
| Id | string | Primary key, unique identifier |
| UserName | string | Login username |
| Email | string | User's email address |
| BusinessName | string | Name of the business |
| Address | string | Physical address |
| City | string | City location |
| State | string | State/province |
| PostalCode | string | Postal/ZIP code |
| Country | string | Country |
| TaxId | string | Tax identification number |
| UserType | enum | Admin, Wholesaler, or Retailer |

**Product**
| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key, unique identifier |
| Name | string | Product name |
| Description | string | Product description |
| CategoryId | int | Foreign key to Category |
| SKU | string | Stock keeping unit |
| IsActive | bool | Product availability status |

**Category**
| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key, unique identifier |
| Name | string | Category name |
| Description | string | Category description |
| CreatedById | string | Foreign key to ApplicationUser |

**WholesalerProduct**
| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key, unique identifier |
| ProductId | int | Foreign key to Product |
| WholesalerId | string | Foreign key to ApplicationUser |
| Price | decimal | Wholesale price |
| StockQuantity | int | Available stock |
| IsActive | bool | Listing availability status |

**RetailerProduct**
| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key, unique identifier |
| WholesalerProductId | int | Foreign key to WholesalerProduct |
| RetailerId | string | Foreign key to ApplicationUser |
| PurchasePrice | decimal | Price paid by retailer |
| SellingPrice | decimal | Price for end customers |
| StockQuantity | int | Available stock |
| Notes | string | Additional notes |

**Order**
| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key, unique identifier |
| RetailerId | string | Foreign key to ApplicationUser |
| WholesalerId | string | Foreign key to ApplicationUser |
| OrderDate | DateTime | Date and time of order |
| Status | enum | Order status (Pending, Processing, etc.) |
| TotalAmount | decimal | Total order amount |
| Notes | string | Additional notes |

**OrderItem**
| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key, unique identifier |
| OrderId | int | Foreign key to Order |
| WholesalerProductId | int | Foreign key to WholesalerProduct |
| Quantity | int | Quantity ordered |
| UnitPrice | decimal | Price per unit |
| Subtotal | decimal | Total for line item |

**Transaction**
| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key, unique identifier |
| OrderId | int | Foreign key to Order |
| RetailerId | string | Foreign key to ApplicationUser |
| WholesalerId | string | Foreign key to ApplicationUser |
| Amount | decimal | Total transaction amount |
| AmountPaid | decimal | Amount paid so far |
| TransactionDate | DateTime | Date and time of transaction |
| Status | enum | Transaction status |
| PaymentMethod | string | Method of payment |
| TransactionReference | string | Reference number |

**Payment**
| Field | Type | Description |
|-------|------|-------------|
| Id | int | Primary key, unique identifier |
| TransactionId | int | Foreign key to Transaction |
| Amount | decimal | Payment amount |
| PaymentDate | DateTime | Date and time of payment |
| Method | enum | Payment method |
| ReferenceNumber | string | Reference number |
| Notes | string | Additional notes |

## 3.4 Component Design

### 3.4.1 User Authentication Module

**Features:**
- User registration with role selection (Admin, Wholesaler, Retailer)
- Secure login with email/username and password
- Role-based authorization for system features
- Password reset functionality
- Session management
- Account profile management

**Security:**
- ASP.NET Identity framework for user authentication
- Password hashing and salting
- HTTPS protocol for secure data transmission
- Anti-forgery tokens for form submissions
- Protection against common web vulnerabilities (XSS, CSRF)
- Input validation and sanitization
- Role-based access control (RBAC) for feature authorization

### 3.4.2 Data Storage Module

**Database:**
- Microsoft SQL Server database
- Entity Framework Core ORM for data access
- Code-first approach with migrations
- Relational database design with proper foreign key constraints

**Entities:**
- ApplicationUser: System users with role-specific properties
- Product: Base product information
- Category: Product categorization
- WholesalerProduct: Products offered by wholesalers
- RetailerProduct: Products in retailer inventory
- Order & OrderItem: Order management
- Cart & CartItem: Shopping cart functionality
- Transaction: Financial transaction records
- Payment: Individual payment records

**Design:**
- Normalized database schema to minimize redundancy
- Appropriate indexes for performance optimization
- Soft delete pattern for data retention
- Audit trails for critical operations
- Transaction support for data integrity
- Navigation properties for efficient relationship traversal
- Lazy loading for complex object graphs

### 3.4.3 User Interface Module

- Bootstrap-based responsive design for cross-device compatibility
- MVC architecture with Razor views
- Clean separation of concerns (presentation, business logic, data access)
- Consistent layout and navigation across the application
- Role-specific dashboards and navigation menus
- Form validation with client-side and server-side validation
- Interactive data visualizations using JavaScript charts
- AJAX for asynchronous operations to improve user experience
- Partial views for reusable UI components
- Notification system for important events and alerts

### 3.4.4 Data Processing Module

**Responsibilities:**
- Business logic implementation
- Data validation and processing
- Order workflow management
- Inventory tracking and updates
- Payment processing
- Report generation and analytics
- Notification handling
- Data import/export capabilities

**Technologies:**
- C# programming language
- ASP.NET Core MVC framework
- LINQ for data querying
- Task-based asynchronous pattern for performance
- Dependency injection for loose coupling
- Service layer pattern for business logic
- Repository pattern for data access
- Unit of Work pattern for transaction management
- Background services for scheduled tasks
- Logging framework for error tracking and auditing

### 3.4.5 Data Flow Diagram (DFD)

```
+----------------+     +-----------------+     +------------------+
|                |     |                 |     |                  |
|  Retailer UI   |<--->|  Controllers    |<--->|  Data Services   |
|                |     |                 |     |                  |
+----------------+     +-----------------+     +------------------+
                              ^                        ^
                              |                        |
                              v                        v
+----------------+     +-----------------+     +------------------+
|                |     |                 |     |                  |
| Wholesaler UI  |<--->| Authentication  |<--->|  Entity Models   |
|                |     |    Service      |     |                  |
+----------------+     +-----------------+     +------------------+
                              ^                        ^
                              |                        |
                              v                        v
+----------------+     +-----------------+     +------------------+
|                |     |                 |     |                  |
|   Admin UI     |<--->|  Reporting      |<--->|   Database       |
|                |     |   Service       |     |                  |
+----------------+     +-----------------+     +------------------+
```

**Key Data Flows:**

1. **User Authentication Flow:**
   - User credentials → Authentication Service → Database validation → User session

2. **Product Management Flow:**
   - Admin creates base products → Wholesalers add pricing/inventory → Products available to retailers

3. **Order Processing Flow:**
   - Retailer creates cart → Checkout → Order creation → Wholesaler processing → Status updates → Delivery → Payment

4. **Inventory Management Flow:**
   - Order placed → Wholesaler inventory reduced → Order delivered → Retailer inventory increased

5. **Reporting Flow:**
   - User requests report → Report parameters → Data retrieval → Data processing → Visualization → User display

6. **Payment Processing Flow:**
   - Payment initiated → Transaction validation → Payment recording → Transaction status update → Notification

## 3.5 User Interface Design

### Design Tools:
- Visual Studio built-in design tools
- Bootstrap 5 framework for responsive design
- Font Awesome for iconography
- Custom CSS for specific styling requirements
- JavaScript and jQuery for client-side interactivity
- Chart.js for data visualization
- Figma for wireframing and prototyping

### Screens Include:
1. **Authentication Screens**:
   - Login page
   - Registration page
   - Password reset page

2. **Dashboard Screens**:
   - Admin dashboard with system-wide metrics
   - Wholesaler dashboard with sales and inventory metrics
   - Retailer dashboard with inventory and order metrics

3. **Product Management Screens**:
   - Category management (Admin)
   - Product catalog management (Admin)
   - Wholesaler product management (Wholesaler)
   - Retailer inventory management (Retailer)

4. **Order Management Screens**:
   - Shopping cart view (Retailer)
   - Checkout process (Retailer)
   - Order listing and filtering (All roles)
   - Order details with status tracking (All roles)

5. **Payment Screens**:
   - Payment form (Retailer)
   - Payment history (All roles)
   - Transaction details (All roles)

6. **Reporting Screens**:
   - Sales reports with filters and charts (Admin)
   - Payment reports with filters and charts (Admin)
   - Wholesaler performance reports (Wholesaler)
   - Retailer performance reports (Retailer)

### UI Features:
- Responsive design for desktop, tablet, and mobile devices
- Role-specific navigation menus and dashboards
- Interactive data tables with sorting, filtering, and pagination
- Dynamic charts and graphs for data visualization
- Form validation with immediate feedback
- Modal dialogs for quick actions
- Toast notifications for system messages
- Progress indicators for long-running operations
- Consistent color scheme and typography
- Accessibility compliance with WCAG guidelines
- Dark mode support
- Printable report views

## 3.6 Testing and Quality Assurance

### Testing Types:
1. **Unit Testing**:
   - Testing individual components in isolation
   - Controller method testing
   - Service method testing
   - Data access layer testing

2. **Integration Testing**:
   - Testing component interactions
   - API endpoint testing
   - Database integration testing
   - Authentication flow testing

3. **System Testing**:
   - End-to-end workflow testing
   - Cross-browser compatibility testing
   - Responsive design testing
   - Performance testing

4. **User Acceptance Testing (UAT)**:
   - Testing with actual users (admin, wholesalers, retailers)
   - Scenario-based testing
   - Usability testing

5. **Security Testing**:
   - Authentication and authorization testing
   - Input validation testing
   - SQL injection testing
   - Cross-site scripting (XSS) testing
   - Cross-site request forgery (CSRF) testing

6. **Performance Testing**:
   - Load testing
   - Stress testing
   - Endurance testing
   - Database query optimization

### Tools:
- **Unit Testing**: xUnit, Moq for mocking
- **Integration Testing**: TestServer, WebApplicationFactory
- **UI Testing**: Selenium WebDriver
- **API Testing**: Postman, RestSharp
- **Performance Testing**: JMeter, Azure Load Testing
- **Security Testing**: OWASP ZAP, SonarQube
- **Code Coverage**: Coverlet
- **Continuous Integration**: Azure DevOps, GitHub Actions
- **Bug Tracking**: Azure DevOps Boards, Jira
- **Test Management**: TestRail

## 3.7 Requirements Matrix

| Requirement ID | Description | Priority | Status | Test Cases |
|---------------|-------------|----------|--------|------------|
| REQ-001 | User registration and authentication | High | Implemented | TC-001, TC-002 |
| REQ-002 | Role-based access control | High | Implemented | TC-003, TC-004 |
| REQ-003 | Product category management | Medium | Implemented | TC-005 |
| REQ-004 | Base product management | High | Implemented | TC-006, TC-007 |
| REQ-005 | Wholesaler product management | High | Implemented | TC-008, TC-009 |
| REQ-006 | Retailer inventory management | High | Implemented | TC-010, TC-011 |
| REQ-007 | Shopping cart functionality | High | Implemented | TC-012, TC-013 |
| REQ-008 | Order placement and processing | High | Implemented | TC-014, TC-015 |
| REQ-009 | Order status tracking | Medium | Implemented | TC-016 |
| REQ-010 | Payment processing | High | Implemented | TC-017, TC-018 |
| REQ-011 | Partial payment support | Medium | Implemented | TC-019 |
| REQ-012 | Admin reporting dashboard | Medium | Implemented | TC-020 |
| REQ-013 | Wholesaler reporting dashboard | Medium | Implemented | TC-021 |
| REQ-014 | Retailer reporting dashboard | Medium | Implemented | TC-022 |
| REQ-015 | Data export functionality | Low | Planned | TC-023 |
| REQ-016 | Email notifications | Medium | Implemented | TC-024 |
| REQ-017 | Mobile-responsive design | High | Implemented | TC-025 |
| REQ-018 | Low stock alerts | Medium | Implemented | TC-026 |
| REQ-019 | Multi-language support | Low | Planned | TC-027 |
| REQ-020 | System backup and recovery | High | Implemented | TC-028 | 