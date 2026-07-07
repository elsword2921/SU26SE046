# DATABASE DESIGN DOCUMENT
## Used Clothing Lifecycle Management System

**Date:** 2026-05-11

---

# 1. Database Design Overview

The database is designed based on the following architectural principles:

- Workflow-driven architecture
- Batch-based clothing processing
- Inventory transaction management
- Role-Based Access Control (RBAC)
- Audit logging and traceability
- Scalability and maintainability

The system uses PostgreSQL as the relational database management system and follows normalized relational database design principles.

---

# 2. Database Design Principles

The database follows these design standards:

- UUID as Primary Key
- Soft delete support
- Audit fields for important entities
- Transaction-based inventory management
- Workflow state tracking
- Referential integrity using foreign keys
- Extensible structure for future enhancements

---

# 3. Core Modules

| Module | Purpose |
|---|---|
| Identity & Access Control | Authentication and RBAC |
| Donation Management | Manage donation requests |
| Intake Management | Manage intake batches |
| Classification Management | Manage clothing classification |
| Warehouse Management | Inventory management |
| Distribution Management | Charity distribution |
| Recycling Management | Recycling transfers |
| Audit & Workflow Tracking | Audit logs and workflow history |
| Notification Management | Realtime notifications |

---

# 4. Identity and Access Control Module

## 4.1 Roles Table

Stores system **roles** for RBAC.

### Roles

| Column | Type | Description |
|---|---|---|
| Id | UUID | Primary Key |
| Name | VARCHAR(50) | Role name |
| Description | TEXT | Role description |
| CreatedAt | TIMESTAMP | Creation date |
| UpdatedAt | TIMESTAMP | Last updated date |
| IsDeleted | BOOLEAN | Soft delete flag |

### Example Roles

- Donor
- ReceivingStaff
- ClassificationStaff
- WarehouseStaff
- Manager
- SystemAdmin

---

## 4.2 Users Table

Stores user account information.

### Users

| Column | Type | Description |
|---|---|---|
| Id | UUID | Primary Key |
| FullName | VARCHAR(100) | User full name |
| Email | VARCHAR(100) | User email |
| PhoneNumber | VARCHAR(20) | Phone number |
| PasswordHash | TEXT | Hashed password |
| AvatarUrl | TEXT | Avatar image |
| RoleId | UUID | FK to Roles |
| IsActive | BOOLEAN | Account status |
| CreatedAt | TIMESTAMP | Creation date |
| UpdatedAt | TIMESTAMP | Last updated date |
| IsDeleted | BOOLEAN | Soft delete flag |

---

# 5. Donation Management Module

## 5.1 DonationRequests Table

Stores clothing donation requests created by **donors**.

### DonationRequests

| Column | Type | Description |     
|---|---|---|
| Id | UUID | Primary Key |
| RequestCode | VARCHAR(50) | Unique request code |
| DonorId | UUID | FK to Users |
| EstimatedQuantity | INT | Estimated quantity |
| PickupAddress | TEXT | Pickup address |
| Note | TEXT | Additional notes |
| Status | VARCHAR(50) | Request status |
| ApprovedBy | UUID | FK to Users |
| ApprovedAt | TIMESTAMP | Approval date |
| CreatedAt | TIMESTAMP | Creation date |
| UpdatedAt | TIMESTAMP | Last updated date |
| IsDeleted | BOOLEAN | Soft delete flag |

### Donation Request Status

```text
Pending
Approved
Rejected
Received
Completed
```

## 5.2 DonationRequestItems Table

Stores clothing categories inside a donation request.

### DonationRequestItems

| Column | Type | Description |
|---|---|---|
| Id | UUID | Primary Key |
| DonationRequestId | UUID | FK to DonationRequests |
| ClothingCategoryId | UUID | FK to ClothingCategories |
| EstimatedQuantity | INT | Estimated quantity |
| ConditionNote | TEXT | Condition description |

## 6. Intake Management Module

### 6.1 IntakeBatches Table

Core entity representing a clothing intake batch.

### IntakeBatches

| Column | Type | Description |
|---|---|---|
| Id | UUID | Primary Key |
| BatchCode | VARCHAR(50) | Unique batch code |
| DonationRequestId | UUID | FK to DonationRequests |
| ReceivedBy | UUID | FK to Users |
| ReceivedDate | TIMESTAMP | Receiving date |
| Status | VARCHAR(50) | Batch workflow status |
| TotalQuantity | INT | Total quantity |
| Note | TEXT | Intake notes |
| CreatedAt | TIMESTAMP | Creation date |
| UpdatedAt | TIMESTAMP | Last updated date |
| IsDeleted | BOOLEAN | Soft delete flag |

### Intake Batch Workflow Status

```text
Received
UnderClassification
Classified
Stored
Distributed
Recycled
Completed
```

### 6.2 IntakeBatchImages Table

Stores intake batch images.

### IntakeBatchImages

| Column | Type | Description |
|---|---|---|
| Id | UUID | Primary Key |
| IntakeBatchId | UUID | FK to IntakeBatches |
| ImageUrl | TEXT | Image path |
| CreatedAt | TIMESTAMP | Upload date |

## 7. Classification Management Module

### 7.1 ClothingCategories Table

Stores clothing categories.

### ClothingCategories

| Column | Type | Description |
|---|---|---|
| Id | UUID | Primary Key |
| Name | VARCHAR(100) | Category name |
| Description | TEXT | Category description |

### Example Categories

- T-Shirt
- Jeans
- Jacket
- Children Clothing
- Shoes

### 7.2 ClassificationResults Table

Stores clothing classification results for each intake batch.

### ClassificationResults

| Column | Type | Description |
|---|---|---|
| Id | UUID | Primary Key |
| IntakeBatchId | UUID | FK to IntakeBatches |
| ClothingCategoryId | UUID | FK to ClothingCategories |
| GenderType | VARCHAR(20) | Gender type |
| SizeType | VARCHAR(20) | Size |
| ConditionType | VARCHAR(20) | Condition |
| ProcessingDirection | VARCHAR(20) | Processing direction |
| Quantity | INT | Quantity |
| Note | TEXT | Additional notes |
| ClassifiedBy | UUID | FK to Users |
| ClassifiedAt | TIMESTAMP | Classification date |

### ProcessingDirection

```text
Charity
Reuse
Recycle
Reject
```

## 8. Warehouse Management Module

### 8.1 Warehouses Table

Stores warehouse information.

### Warehouses

| Column | Type | Description |
|---|---|---|
| Id | UUID | Primary Key |
| Name | VARCHAR(100) | Warehouse name |
| Location | TEXT | Warehouse location |
| Description | TEXT | Description |
| CreatedAt | TIMESTAMP | Creation date |

### 8.2 InventoryTransactions Table

Stores all warehouse inventory movements.

### InventoryTransactions

| Column | Type | Description |
|---|---|---|
| Id | UUID | Primary Key |
| WarehouseId | UUID | FK to Warehouses |
| ClassificationResultId | UUID | FK to ClassificationResults |
| TransactionType | VARCHAR(50) | Transaction type |
| Quantity | INT | Transaction quantity |
| ReferenceCode | VARCHAR(50) | Reference document code |
| Note | TEXT | Additional notes |
| CreatedBy | UUID | FK to Users |
| CreatedAt | TIMESTAMP | Creation date |

### Inventory Transaction Types

```text
Import
Distribution
Recycling
Adjustment
Return
```

### Inventory Calculation Formula

**Current Inventory** = SUM(All Inventory Transactions)

## 9. Distribution Management Module

### 9.1 PartnerOrganizations Table

Stores charity organizations and recycling units.

### PartnerOrganizations

| Column | Type | Description |
|---|---|---|
| Id | UUID | Primary Key |
| Name | VARCHAR(150) | Organization name |
| OrganizationType | VARCHAR(50) | Organization type |
| PhoneNumber | VARCHAR(20) | Phone number |
| Email | VARCHAR(100) | Email |
| Address | TEXT | Address |
| CreatedAt | TIMESTAMP | Creation date |

### Organization Types

```text
Charity
Recycling
```

### 9.2 Distributions Table

Stores charity distribution records.

### Distributions

| Column | Type | Description |
|---|---|---|
| Id | UUID | Primary Key |
| DistributionCode | VARCHAR(50) | Unique distribution code |
| PartnerOrganizationId | UUID | FK to PartnerOrganizations |
| DistributedBy | UUID | FK to Users |
| DistributedDate | TIMESTAMP | Distribution date |
| Note | TEXT | Additional notes |
| CreatedAt | TIMESTAMP | Creation date |

### 9.3 DistributionItems Table

Stores distributed clothing items.

### DistributionItems

| Column | Type | Description |
|---|---|---|
| Id | UUID | Primary Key |
| DistributionId | UUID | FK to Distributions |
| ClassificationResultId | UUID | FK to ClassificationResults |
| Quantity | INT | Distributed quantity |

## 10. Recycling Management Module

### 10.1 RecyclingTransfers Table

Stores recycling transfer records.

### RecyclingTransfers

| Column | Type | Description |
|---|---|---|
| Id | UUID | Primary Key |
| TransferCode | VARCHAR(50) | Unique transfer code |
| PartnerOrganizationId | UUID | FK to PartnerOrganizations |
| TransferredBy | UUID | FK to Users |
| TransferredDate | TIMESTAMP | Transfer date |
| Note | TEXT | Additional notes |
| CreatedAt | TIMESTAMP | Creation date |

### 10.2 RecyclingTransferItems Table

Stores recycling transfer items.

### RecyclingTransferItems

| Column | Type | Description |
|---|---|---|
| Id | UUID | Primary Key |
| RecyclingTransferId | UUID | FK to RecyclingTransfers |
| ClassificationResultId | UUID | FK to ClassificationResults |
| Quantity | INT | Transfer quantity |

## 11. Workflow and Audit Module

### 11.1 BatchStatusHistories Table

Stores intake batch workflow history.

### BatchStatusHistories

| Column | Type | Description |
|---|---|---|
| Id | UUID | Primary Key |
| IntakeBatchId | UUID | FK to IntakeBatches |
| OldStatus | VARCHAR(50) | Previous status |
| NewStatus | VARCHAR(50) | New status |
| ChangedBy | UUID | FK to Users |
| ChangedAt | TIMESTAMP | Status changed date |
| Note | TEXT | Additional notes |

### 11.2 AuditLogs Table

Stores important system activity logs.

### AuditLogs

| Column | Type | Description |
|---|---|---|
| Id | UUID | Primary Key |
| UserId | UUID | FK to Users |
| Action | VARCHAR(100) | Action type |
| EntityName | VARCHAR(100) | Affected entity |
| EntityId | UUID | Entity identifier |
| OldValues | JSONB | Previous values |
| NewValues | JSONB | New values |
| CreatedAt | TIMESTAMP | Action date |

## 12. Notification Module

### 12.1 Notifications Table

Stores realtime notification records.

### Notifications

| Column | Type | Description |
|---|---|---|
| Id | UUID | Primary Key |
| UserId | UUID | FK to Users |
| Title | VARCHAR(200) | Notification title |
| Message | TEXT | Notification content |
| IsRead | BOOLEAN | Read status |
| CreatedAt | TIMESTAMP | Creation date |

## 13. Entity Relationship Summary

### Core Relationships

- Role → Users
- Users → DonationRequests
- DonationRequests → DonationRequestItems
- DonationRequests → IntakeBatches
- IntakeBatches → ClassificationResults
- ClassificationResults → InventoryTransactions
- InventoryTransactions → Distribution/Recycling
- IntakeBatches → BatchStatusHistories

## 14. Workflow-driven Design

The system follows a workflow-driven architecture.

Each intake batch moves through controlled workflow states:

```
Received → UnderClassification → Classified → Stored → Distributed / Recycled → Completed
```

The system validates all workflow transitions to ensure operational consistency.

## 15. Inventory Transaction-based Design

The system manages inventory using transaction history instead of directly updating stock quantities.

Benefits include:

- Better auditability
- Inventory traceability
- Historical tracking
- Easier reporting and analytics
- Improved warehouse transparency

## 16. Audit and Traceability

The system records:

- Workflow state changes
- Inventory transactions
- User operations
- Distribution activities
- Recycling transfers

This improves operational traceability and accountability.

## 17. Recommended Database Indexes

```sql
CREATE INDEX idx_users_email
ON "Users"("Email");

CREATE INDEX idx_batches_status
ON "IntakeBatches"("Status");

CREATE INDEX idx_inventory_transactions_type
ON "InventoryTransactions"("TransactionType");

CREATE INDEX idx_notifications_user
ON "Notifications"("UserId");
```

## 18. Database Design Strengths

This database design supports:

- Workflow lifecycle management
- Batch-based processing
- Inventory transaction management
- Audit logging
- Role-based access control
- Realtime notifications
- Warehouse management
- Reporting and analytics
- Future scalability