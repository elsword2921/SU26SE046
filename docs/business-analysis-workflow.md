# USED CLOTHING LIFECYCLE MANAGEMENT SYSTEM
## Business Analysis & Workflow Document

**Project Name:** Used Clothing Lifecycle Management System  

---

# 1. Business Background

Currently, the process of collecting, classifying, storing, and distributing used clothing is mostly handled manually through paper records or Excel files.

This causes many operational problems:

- Difficult to track actual inventory quantity
- High risk of lost or untracked items
- Difficult to manage clothing lifecycle
- No centralized workflow management
- Hard to generate operational reports
- Difficult to coordinate between departments

The proposed system aims to digitize the entire workflow of used clothing processing from donor request creation to warehouse distribution.

---

# 2. Business Problem

## Current Problems

### Manual Operations
- Clothing intake recorded manually
- Classification tracked using notes or spreadsheets
- Inventory managed inconsistently

### Poor Traceability
- Cannot track clothing lifecycle clearly
- Difficult to identify who handled each batch
- Difficult to audit inventory movement

### Inventory Issues
- Quantity mismatch
- Missing items
- No realtime stock visibility

### Reporting Difficulties
- Reports generated manually
- Difficult to monitor operational performance
- Difficult to analyze donation and distribution trends

---

# 3. Business Goals

The system aims to:

- Create an e-commerce platform for buying and selling used items, or donating used items to charity and recycling. 
- Digitize used clothing collection workflow
- Manage intake and classification processes
- Improve warehouse inventory tracking
- Track the entire clothing lifecycle
- Support recycling and charity distribution
- Provide statistical and operational reports

---

# Defined Business Flow (Draft)

- Donor
  - → Create Donation Request (Charity/Selling clothes based on kg)
- Receiving Staff
  - → Receive Request
  - → Receive Clothing (Confirm purchase if Donor create sell request)
  - → Create Intake Batch include charity/selling clothes
- Classification Staff
  - → Classify Clothing (Note condition Good/Fair/Damaged)
  - → Assign Processing Direction (Charity/Recycle/Resell)
- Warehouse Staff
  - → Store Inventory
- Organization
  - → Create Request based on avaiable batches in **Inventory**
- Manager
  - → Approve/Reject Organization Request
- Warehouse Staff
  - → Create Distribution Processing Data (Billing if not charity)
- Organization
  - → Confirm Receiving/Purchase (DistributionCompleted)
- System
  - → Generate Reports & Audit Logs

---

# 4. Business Model

## Revenue Sources

### Subscription Plans

Organizations pay monthly fees based on usage.

### Example Plans

| Plan | Features |
|---|---|
| Standard | All features in system |

- The Organization can request to buy **USED CLOTHES** that available in Inventory
- Casual users can buy resell clothes(secondhand) on website

---

# 5. Main Actors

| Actor | Description |
|---|---|
| Guest | Buy resell clothes |
| Donor | Sell or donate used clothing |
| Receiving Staff | Receive clothing and pay donor |
| Classification Staff | Classify and evaluate clothing |
| Warehouse Staff | Manage inventory and warehouse |
| Organization | Buy clothing batches |
| Manager | Approve requests and monitor operations |

---

# 6. Business Workflow Detail

The system supports the following workflow:

---

## Step 1 — Donor Creates Selling Request

The donor creates a request to sell used clothing.

### Donor Inputs
- Estimated weight (kg)
- Clothing description
- Pickup address
- Contact information
- Photos (optional)

### Initial Status

```text
Pending
```

---

## Step 2 — Receiving Staff Receives Clothing

Receiving staff reviews the request and visits the donor location.

### Actions
- Verify clothing quantity
- Measure actual weight
- Pay donor (If Donor create Sell Request)
- Accept or reject request

### Updated Status
- ReceivedCompleted

If rejected:
- Rejected

---

## Step 3 — Intake Batch Creation

After receiving clothing, the Receiving Staff creates an Intake Batch.

### Intake Batch Contains
- Batch code
- Received date
- Actual quantity
- Receiver information
- Batch images
- Notes

### Status
- PendingClassification

---

## Step 4 — Classification Process

Classification staff receives the intake batch.

### Classification Activities
- Separate clothing by category
- Evaluate clothing condition
- Determine resale/recycling/charity direction
- Record damaged items (All damaged items should **Recycling**)
- With **Resell** items Classification Staff need to create request to Managers to sell these items on website

### Classification Attributes
- Gender
  - Male
  - Female
  - Unisex
- Target User
  - Children
  - Adult
- Size
  - XS
  - S
  - M
  - L
  - XL
  - XXL
- Condition
  - Good
  - Fair
  - Damaged
- Processing Direction
  - Charity
  - Recycle

### Batch Status
- Classifying
→ Classified

---

## Step 5 — Transfer to Warehouse

After classification:
- Clothing is transferred to warehouse
- Warehouse staff confirms receiving

### Warehouse Activities
- Store inventory
- Group inventory
- Track quantity
- Manage stock movement

### Inventory Group Examples
- Male-Shirt-M-Good
- Female-Pants-L-Charity
- Children-Shirt-S-Recycle

### Batch Status
- WarehouseReceived

---

## Step 6 — Organization Purchases Clothing

Organizations can browse available inventory.

### Organization Activities
- View available stock
- Create purchase requests
- Track request status
- Confirm received orders

### Example Organization Types
- Charity organizations
- Recycling companies

---

## Step 7 — Manager Approval

Manager reviews organization requests.
Manager review Resell request.

### Manager Actions
- Approve request
- Reject request
- List resell items to website
- Monitor inventory
- Monitor warehouse movement

### Request Status
- PendingApproval
→ Approved
→ Completed

---

## Step 8 — Distribution / Delivery

Warehouse staff prepares and ships clothing.

### Activities
- Export inventory
- Generate delivery records
- Update inventory transactions
- Record shipment history

---

## Step 9 — Reporting & Analytics

The system generates reports including:

- Clothing intake reports
- Revenue reports
- Warehouse inventory reports
- Classification statistics
- Recycling statistics
- Organization purchasing reports
- Operational performance reports

---

# 7. Workflow State Transition

## Donation Request Workflow
- Pending
→ Received
→ Rejected

## Intake Batch Workflow
- ReceivingCompleted
→ Classifying
→ Classified
→ WarehouseReceived
→ ReadyForDistribution
→ Distributed
→ Completed

## Organization Request Workflow
- PendingApproval
→ Approved
→ Shipping
→ Confirmed
→ Completed

---