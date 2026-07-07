# Context Diagram Summary

## Used Clothing Lifecycle Management System

### 1. System

**Used Clothing Lifecycle Management System**

The system acts as the central platform that manages the complete lifecycle of used clothing processing, including:

- Donation request management
- Clothing receiving and intake
- Clothing classification
- Warehouse inventory management
- Distribution and recycling processing
- Organization request management
- Reporting and workflow tracking

The system coordinates communication and data exchange between all external entities involved in the operational workflow.

---

### 2. External Entities and Data Flows

#### 2.1 Donor

**Description**

A donor is an individual who creates requests to donate or sell used clothing to the organization.

The donor can:

- Register and login
- Create donation requests
- Provide pickup information
- Track donation status
- View donation history
- Communicate with staff

**Data Flows**

**Donor → System**

| Data Flow | Meaning |
|---|---|
| Account Information | User registration and login information |
| Donation Request Data | Clothing donation request details submitted by donor |
| Pickup Information | Pickup address and scheduling information |
| Communication Messages | Messages sent to receiving staff |

**System → Donor**

| Data Flow | Meaning |
|---|---|
| Donation Status Information | Current status of donation request |
| Pickup Confirmation Information | Confirmation of receiving schedule |
| Donation History | Historical donation records |
| Notification Information | System notifications and updates |

---

#### 2.2 Receiving Staff

**Description**

Receiving Staff are responsible for handling the physical receiving process of clothing donations.

Their responsibilities include:

- Reviewing donation requests
- Confirming clothing pickup
- Recording received items
- Creating intake batches
- Updating receiving statuses

**Data Flows**

**Receiving Staff → System**

| Data Flow | Meaning |
|---|---|
| Receiving Confirmation Data | Confirmation that clothing has been received |
| Intake Batch Data | Batch creation information for received clothing |
| Pickup Result Data | Actual receiving result including quantity and condition |
| Receiving Status Update Data | Updates to receiving workflow status |

**System → Receiving Staff**

| Data Flow | Meaning |
|---|---|
| Donation Request Information | Donation requests assigned for processing |
| Donor Information | Donor contact and request details |
| Pickup Schedule Information | Pickup scheduling information |
| Notification Information | Realtime operational notifications |

---

#### 2.3 Classification Staff

**Description**

Classification Staff are responsible for inspecting and categorizing clothing items after intake.

Their responsibilities include:

- Reviewing intake batches
- Classifying clothing
- Determining clothing condition
- Assigning processing direction
- Recording classification results

**Data Flows**

**Classification Staff → System**

| Data Flow | Meaning |
|---|---|
| Classification Result Data | Classification details for clothing items |
| Condition Assessment Data | Clothing condition evaluation results |
| Processing Direction Data | Decision whether items go to charity or recycling |
| Damaged Item Notes | Notes for damaged or rejected items |

**System → Classification Staff**

| Data Flow | Meaning |
|---|---|
| Intake Batch Information | Intake batches waiting for classification |
| Batch Detail Information | Detailed information about clothing batches |
| Workflow Status Information | Current processing workflow state |
| Notification Information | Realtime notifications and updates |

---

#### 2.4 Warehouse Staff

**Description**

Warehouse Staff manage inventory storage and warehouse operations.

Their responsibilities include:

- Receiving classified batches
- Managing inventory
- Tracking stock movement
- Processing organization requests
- Managing distribution and recycling transfers

**Data Flows**

**Warehouse Staff → System**

| Data Flow | Meaning |
|---|---|
| Warehouse Receiving Data | Confirmation that warehouse received a batch |
| Inventory Transaction Data | Inventory import/export transaction records |
| Distribution Processing Data | Processing information for outgoing distributions |
| Recycling Transfer Data | Recycling shipment and transfer records |

**System → Warehouse Staff**

| Data Flow | Meaning |
|---|---|
| Inventory Information | Current warehouse inventory status |
| Batch Transfer Information | Incoming classified batch information |
| Organization Request Information | Requests from organizations |
| Notification Information | Warehouse operation notifications |

---

#### 2.5 Charity Organization

**Description**

A Charity Organization requests usable clothing items for charitable activities.

Their responsibilities include:

- Viewing available inventory
- Requesting clothing distributions
- Tracking request status
- Confirming item receipt

**Data Flows**

**Charity Organization → System**

| Data Flow | Meaning |
|---|---|
| Distribution Request Data | Requests for receiving charity clothing |
| Organization Information | Organization profile and contact details |
| Distribution Confirmation Data | Confirmation after receiving clothing |

**System → Charity Organization**

| Data Flow | Meaning |
|---|---|
| Available Charity Inventory | Clothing available for charity distribution |
| Distribution Status Information | Current request processing status |
| Distribution History | Historical distribution records |
| Notification Information | Realtime updates and notifications |

---

#### 2.6 Recycling Organization

**Description**

A Recycling Organization purchases or receives recyclable clothing materials for recycling purposes.

Their responsibilities include:

- Viewing recyclable inventory
- Creating recycling purchase requests
- Tracking transfer status
- Confirming transfer completion

**Data Flows**

**Recycling Organization → System**

| Data Flow | Meaning |
|---|---|
| Recycling Purchase Request Data | Requests to purchase recyclable clothing |
| Organization Information | Organization profile and contact details |
| Transfer Confirmation Data | Confirmation of recycling transfer receipt |

**System → Recycling Organization**

| Data Flow | Meaning |
|---|---|
| Recycling Inventory Information | Available recyclable inventory |
| Transfer Status Information | Current recycling transfer status |
| Recycling Transfer History | Historical recycling transfer records |
| Notification Information | Realtime operational notifications |

---

#### 2.7 Manager

**Description**

The Manager supervises the entire operational workflow and system administration activities.

Their responsibilities include:

- Monitoring operations
- Managing workflow configuration
- Reviewing reports and analytics
- Managing partner organizations
- Approving organization requests

**Data Flows**

**Manager → System**

| Data Flow | Meaning |
|---|---|
| Workflow Configuration Data | Workflow settings and operational rules |
| Approval Decision Data | Approval or rejection decisions for organization requests |
| User Management Data | User and role management operations |
| Report Request Data | Requests for reports and statistics |

**System → Manager**

| Data Flow | Meaning |
|---|---|
| Operational Dashboard Information | Overall operational monitoring data |
| Report Information | Reports and statistical summaries |
| Pending Approval Information | Organization requests waiting for approval |
| Audit Log Information | System activity and audit records |
