# Purpose and Meaning of Each Database Table
## Used Clothing Lifecycle Management System

---

# 1. Roles

## Purpose

The `Roles` table stores system roles used for:

```text
Role-Based Access Control (RBAC)
```

## Why This Table Exists

The system contains multiple user types:

- Donor
- ReceivingStaff
- ClassificationStaff
- WarehouseStaff
- Manager
- SystemAdmin

Each role has different permissions and responsibilities.

## Business Meaning

This table helps the system:

- Control access permissions
- Separate business responsibilities
- Protect APIs and system resources
- Support secure authentication and authorization

## Relationship

```
Role
1
→ N
Users
```

# 2. Users

## Purpose

The Users table stores user account information.

## Why This Table Exists

The system requires:

- Login authentication
- User identification
- Audit logging
- Activity tracking

## Business Meaning

Every important operation must be traceable.

Examples:

- Who classified a batch?
- Who approved a donation request?
- Who distributed inventory?
- Who updated warehouse stock?

## Relationship

```
Users → DonationRequests
Users → IntakeBatches
Users → AuditLogs
```

# 3. DonationRequests

## Purpose

The DonationRequests table stores donation requests created by donors.

## Why This Table Exists

Donors need to provide:

- Donation information
- Estimated quantity
- Pickup address
- Additional notes

## Business Meaning

This table represents:

The starting point of the workflow

```
Workflow
DonationRequest
↓
Intake
↓
Classification
↓
Warehouse
```

## Relationship

```
DonationRequest
1
→ N
DonationRequestItems
```

# 4. DonationRequestItems

## Purpose

The DonationRequestItems table stores clothing item details inside a donation request.

## Why This Table Exists

One donation request may contain multiple clothing categories:

- T-Shirts
- Jeans
- Jackets
- Shoes

This requires a detail table structure.

## Business Meaning

This table represents:

Estimated incoming clothing items

## Relationship

```
DonationRequest
1
→ N
DonationRequestItems
```

# 5. IntakeBatches

## Purpose

The IntakeBatches table represents:

A clothing intake batch

## Why This Table Exists

The system processes clothing using:

Batch-based workflow management

instead of managing individual clothing items separately.

## Business Meaning

This is:

The core business entity of the entire system

## Workflow Role

```
Received → Classified → Stored → Distributed
```

## Relationship

```
DonationRequest → IntakeBatch
IntakeBatch → ClassificationResults
IntakeBatch → BatchStatusHistories
```

# 6. IntakeBatchImages

## Purpose

Stores images related to intake batches.

## Why This Table Exists

Staff may upload:

- Clothing photos
- Batch condition evidence
- Verification images

## Business Meaning

Supports:

- Traceability
- Verification
- Audit support

# 7. ClothingCategories

## Purpose

Stores standardized clothing categories.

## Why This Table Exists

Avoids hardcoded category values such as:

- T-Shirt
- Jeans
- Jacket

## Business Meaning

Supports:

- Standardized classification
- Filtering
- Reporting
- Analytics

## Relationship

```
ClothingCategories → DonationRequestItems
ClothingCategories → ClassificationResults
```

# 8. ClassificationResults

## Purpose

Stores the actual classification results of clothing batches.

## Why This Table Exists

After intake, staff classify clothing by:

- Clothing type
- Gender
- Size
- Condition
- Processing direction

## Business Meaning

This table represents:

Actual classified inventory

## Important Concept

Difference between:

| Table | Meaning |
|---|---|
| DonationRequestItems | Estimated data |
| ClassificationResults | Actual classified data |

## Relationship

```
IntakeBatch
1
→ N
ClassificationResults
```

# 9. Warehouses

## Purpose

Stores warehouse information.

## Why This Table Exists

Warehouse management requires:

- Warehouse locations
- Inventory ownership
- Warehouse tracking

## Business Meaning

Supports future scalability such as:

- Multiple warehouses
- Warehouse analytics
- Regional inventory management

# 10. InventoryTransactions

## Purpose

Stores all inventory movements.

## Why This Table Exists

Enterprise inventory systems:

Do not directly update stock quantity

Instead:

```
Stock = SUM(all inventory transactions)
```

## Business Meaning

This table acts as:

The inventory ledger

## Example Transactions

| Transaction Type | Quantity |
|---|---|
| Import | +50 |
| Distribution | -20 |
| Recycling | -10 |

## Why This Design Is Important

Supports:

- Inventory traceability
- Historical tracking
- Inventory auditing
- Accurate reporting

## Relationship

```
ClassificationResults → InventoryTransactions
```

# 11. PartnerOrganizations

## Purpose

Stores charity organizations and recycling units.

## Why This Table Exists

The system must track:

Where clothing items are transferred

## Business Meaning

Supports tracking of:

- Charity distributions
- Recycling transfers
- Partner history

# 12. Distributions

## Purpose

Stores charity distribution records.

## Why This Table Exists

Warehouse staff must record:

- Which organization received items
- Distribution date
- Responsible staff member

## Business Meaning

This table represents:

Charity outbound transaction documents

## Relationship

```
Distributions
1
→ N
DistributionItems
```

# 13. DistributionItems

## Purpose

Stores clothing item details inside distributions.

## Why This Table Exists

One distribution may contain multiple clothing types.

## Business Meaning

This table represents:

Distribution detail records

# 14. RecyclingTransfers

## Purpose

Stores recycling transfer records.

## Why This Table Exists

The system must track:

- Recycling destinations
- Transfer quantities
- Transfer dates

## Business Meaning

This table represents:

Recycling outbound transaction documents

# 15. RecyclingTransferItems

## Purpose

Stores detailed clothing items for recycling transfers.

## Why This Table Exists

One recycling transfer may contain multiple clothing categories.

## Business Meaning

Represents:

Recycling transfer detail records

# 16. BatchStatusHistories

## Purpose

Stores workflow state transition history.

## Why This Table Exists

Workflow systems must track:

- When did the batch change status?
- Who changed it?

## Business Meaning

This table acts as:

Workflow audit trail

## Example Workflow

```
Received → Classified → Stored
```

## Why This Table Is Important

Supports:

- Workflow traceability
- Operational transparency
- Audit tracking

# 17. AuditLogs

## Purpose

Stores critical system activity logs.

## Why This Table Exists

Tracks important actions such as:

- Inventory updates
- User management
- Workflow changes
- Record deletions

## Business Meaning

Supports:

- Accountability
- Debugging
- Security monitoring
- Compliance mindset

# 18. Notifications

## Purpose

Stores realtime notification records.

## Why This Table Exists

SignalR requires:

- Notification persistence
- Read/unread tracking

## Business Meaning

Examples:

- New donation request
- Batch classified
- Warehouse updated

## Overall Database Mindset

### Workflow Core

```
DonationRequest
↓
IntakeBatch
↓
ClassificationResults
↓
InventoryTransactions
↓
Distribution/Recycling
```

### Audit Core

```
BatchStatusHistories + AuditLogs
```

### Security Core

```
Users + Roles
```

### Inventory Core

```
InventoryTransactions
```

## Why This Database Design Is Professional

Because it is no longer just a:

Simple CRUD database

Instead, it becomes a:

Workflow-driven operation