# API DESIGN DOCUMENT
## Used Clothing Lifecycle Management System

**Date:** 2026-05-11

---

# 1. API Design Overview

The system exposes RESTful APIs built with ASP.NET Core Web API.

## API Standards

- RESTful API design
- JSON request/response format
- JWT Bearer Authentication
- Role-Based Access Control (RBAC)
- UUID-based identifiers
- Versioned API structure
- Standard HTTP status codes

---

# 2. Base API Configuration

## Base URL

```
/api/v1
```

## Authentication Header

```
Authorization: Bearer {JWT_TOKEN}
```

---

# 3. Authentication & User APIs

## 3.1 Login

**Endpoint**

```
POST /api/v1/auth/login
```

**Related Tables**
- Users
- Roles

**Request Payload**

```json
{
  "email": "admin@example.com",
  "password": "123456"
}
```

**Sample Response**

```json
{
  "accessToken": "jwt_token_here",
  "refreshToken": "refresh_token_here",
  "user": {
    "id": "uuid",
    "fullName": "System Admin",
    "email": "admin@example.com",
    "role": "SystemAdmin"
  }
}
```

## 3.2 Register

**Endpoint**

```
POST /api/v1/auth/register
```

**Related Tables**
- Users
- Roles

**Request Payload**

```json
{
  "fullName": "John Doe",
  "email": "john@example.com",
  "phoneNumber": "0123456789",
  "password": "123456"
}
```

**Sample Response**

```json
{
  "message": "Registration successful"
}
```

## 3.3 Get Current User Profile

**Endpoint**

```
GET /api/v1/users/me
```

**Related Tables**
- Users
- Roles

**Sample Response**

```json
{
  "id": "uuid",
  "fullName": "John Doe",
  "email": "john@example.com",
  "phoneNumber": "0123456789",
  "role": "Donor"
}
```

---

# 4. Roles APIs

## 4.1 Get All Roles

**Endpoint**

```
GET /api/v1/roles
```

**Related Tables**
- Roles

**Sample Response**

```json
[
  {
    "id": "uuid",
    "name": "WarehouseStaff"
  },
  {
    "id": "uuid",
    "name": "Manager"
  }
]
```

---

# 5. Donation Request APIs

## 5.1 Create Donation Request

**Endpoint**

```
POST /api/v1/donation-requests
```

**Related Tables**
- DonationRequests
- DonationRequestItems
- ClothingCategories

**Request Payload**

```json
{
  "estimatedQuantity": 100,
  "pickupAddress": "District 1, Ho Chi Minh City",
  "note": "Mostly adult clothing",
  "items": [
    {
      "clothingCategoryId": "uuid",
      "estimatedQuantity": 50,
      "conditionNote": "Good condition"
    },
    {
      "clothingCategoryId": "uuid",
      "estimatedQuantity": 50,
      "conditionNote": "Used condition"
    }
  ]
}
```

**Sample Response**

```json
{
  "id": "uuid",
  "requestCode": "DR-2026-001",
  "status": "Pending"
}
```

## 5.2 Get Donation Requests

**Endpoint**

```
GET /api/v1/donation-requests
```

**Related Tables**
- DonationRequests
- Users

**Sample Response**

```json
[
  {
    "id": "uuid",
    "requestCode": "DR-2026-001",
    "estimatedQuantity": 100,
    "status": "Pending",
    "createdAt": "2026-05-11T10:00:00Z"
  }
]
```

## 5.3 Get Donation Request Detail

**Endpoint**

```
GET /api/v1/donation-requests/{id}
```

**Related Tables**
- DonationRequests
- DonationRequestItems
- ClothingCategories
- Users

**Sample Response**

```json
{
  "id": "uuid",
  "requestCode": "DR-2026-001",
  "estimatedQuantity": 100,
  "pickupAddress": "District 1",
  "status": "Approved",
  "items": [
    {
      "category": "T-Shirt",
      "estimatedQuantity": 50
    }
  ]
}
```

## 5.4 Approve Donation Request

**Endpoint**

```
PUT /api/v1/donation-requests/{id}/approve
```

**Related Tables**
- DonationRequests
- Users

**Request Payload**

```json
{
  "note": "Approved by receiving staff"
}
```

**Sample Response**

```json
{
  "message": "Donation request approved"
}
```

## 5.5 Reject Donation Request

**Endpoint**

```
PUT /api/v1/donation-requests/{id}/reject
```

**Related Tables**
- DonationRequests
- Users

**Request Payload**

```json
{
  "reason": "Invalid information"
}
```

**Sample Response**

```json
{
  "message": "Donation request rejected"
}
```

---

# 6. Intake Batch APIs

## 6.1 Create Intake Batch

**Endpoint**

```
POST /api/v1/intake-batches
```

**Related Tables**
- IntakeBatches
- DonationRequests
- Users
- BatchStatusHistories

**Request Payload**

```json
{
  "donationRequestId": "uuid",
  "totalQuantity": 95,
  "note": "Received successfully"
}
```

**Sample Response**

```json
{
  "id": "uuid",
  "batchCode": "BATCH-2026-001",
  "status": "Received"
}
```

## 6.2 Get Intake Batches

**Endpoint**

```
GET /api/v1/intake-batches
```

**Related Tables**
- IntakeBatches
- DonationRequests

**Sample Response**

```json
[
  {
    "id": "uuid",
    "batchCode": "BATCH-2026-001",
    "status": "UnderClassification",
    "totalQuantity": 95
  }
]
```

## 6.3 Get Intake Batch Detail

**Endpoint**

```
GET /api/v1/intake-batches/{id}
```

**Related Tables**
- IntakeBatches
- DonationRequests
- IntakeBatchImages
- BatchStatusHistories
- ClassificationResults

**Sample Response**

```json
{
  "id": "uuid",
  "batchCode": "BATCH-2026-001",
  "status": "Classified",
  "totalQuantity": 95,
  "images": [],
  "workflowHistory": [
    {
      "oldStatus": "Received",
      "newStatus": "UnderClassification"
    }
  ]
}
```

## 6.4 Update Batch Status

**Endpoint**

```
PUT /api/v1/intake-batches/{id}/status
```

**Related Tables**
- IntakeBatches
- BatchStatusHistories
- Users

**Request Payload**

```json
{
  "newStatus": "UnderClassification",
  "note": "Sent to classification department"
}
```

**Sample Response**

```json
{
  "message": "Batch status updated successfully"
}
```

## 6.5 Upload Batch Image

**Endpoint**

```
POST /api/v1/intake-batches/{id}/images
```

**Related Tables**
- IntakeBatchImages
- IntakeBatches

**Request Payload**

```json
{
  "imageUrl": "https://storage.com/image.jpg"
}
```

**Sample Response**

```json
{
  "message": "Image uploaded successfully"
}
```

---

# 7. Classification APIs

## 7.1 Create Classification Result

**Endpoint**

```
POST /api/v1/classification-results
```

**Related Tables**
- ClassificationResults
- IntakeBatches
- ClothingCategories
- Users

**Request Payload**

```json
{
  "intakeBatchId": "uuid",
  "clothingCategoryId": "uuid",
  "genderType": "Male",
  "sizeType": "L",
  "conditionType": "Good",
  "processingDirection": "Charity",
  "quantity": 40,
  "note": "Ready for donation"
}
```

**Sample Response**

```json
{
  "id": "uuid",
  "message": "Classification completed"
}
```

## 7.2 Get Classification Results By Batch

**Endpoint**

```
GET /api/v1/intake-batches/{id}/classification-results
```

**Related Tables**
- ClassificationResults
- ClothingCategories
- Users

**Sample Response**

```json
[
  {
    "category": "T-Shirt",
    "sizeType": "L",
    "conditionType": "Good",
    "processingDirection": "Charity",
    "quantity": 40
  }
]
```

---

# 8. Warehouse APIs

## 8.1 Create Warehouse

**Endpoint**

```
POST /api/v1/warehouses
```

**Related Tables**
- Warehouses

**Request Payload**

```json
{
  "name": "Main Warehouse",
  "location": "District 9",
  "description": "Central warehouse"
}
```

**Sample Response**

```json
{
  "id": "uuid",
  "message": "Warehouse created successfully"
}
```

## 8.2 Create Inventory Transaction

**Endpoint**

```
POST /api/v1/inventory-transactions
```

**Related Tables**
- InventoryTransactions
- Warehouses
- ClassificationResults
- Users

**Request Payload**

```json
{
  "warehouseId": "uuid",
  "classificationResultId": "uuid",
  "transactionType": "Import",
  "quantity": 40,
  "referenceCode": "IMPORT-001",
  "note": "Imported to warehouse"
}
```

**Sample Response**

```json
{
  "id": "uuid",
  "message": "Inventory transaction created"
}
```

## 8.3 Get Inventory Transactions

**Endpoint**

```
GET /api/v1/inventory-transactions
```

**Related Tables**
- InventoryTransactions
- Warehouses
- ClassificationResults

**Sample Response**

```json
[
  {
    "transactionType": "Import",
    "quantity": 40,
    "warehouseName": "Main Warehouse"
  }
]
```

---

# 9. Distribution APIs

## 9.1 Create Distribution

**Endpoint**

```
POST /api/v1/distributions
```

**Related Tables**
- Distributions
- DistributionItems
- PartnerOrganizations
- ClassificationResults
- InventoryTransactions

**Request Payload**

```json
{
  "partnerOrganizationId": "uuid",
  "note": "Donation delivery",
  "items": [
    {
      "classificationResultId": "uuid",
      "quantity": 20
    }
  ]
}
```

**Sample Response**

```json
{
  "id": "uuid",
  "distributionCode": "DIST-2026-001"
}
```

## 9.2 Get Distribution Detail

**Endpoint**

```
GET /api/v1/distributions/{id}
```

**Related Tables**
- Distributions
- DistributionItems
- PartnerOrganizations
- ClassificationResults

**Sample Response**

```json
{
  "distributionCode": "DIST-2026-001",
  "organization": "Hope Charity",
  "items": [
    {
      "category": "T-Shirt",
      "quantity": 20
    }
  ]
}
```

---

# 10. Recycling APIs

## 10.1 Create Recycling Transfer

**Endpoint**

```
POST /api/v1/recycling-transfers
```

**Related Tables**
- RecyclingTransfers
- RecyclingTransferItems
- PartnerOrganizations
- ClassificationResults
- InventoryTransactions

**Request Payload**

```json
{
  "partnerOrganizationId": "uuid",
  "note": "Recycling transfer",
  "items": [
    {
      "classificationResultId": "uuid",
      "quantity": 30
    }
  ]
}
```

**Sample Response**

```json
{
  "id": "uuid",
  "transferCode": "REC-2026-001"
}
```

---

# 11. Partner Organization APIs

## 11.1 Create Partner Organization

**Endpoint**

```
POST /api/v1/partner-organizations
```

**Related Tables**
- PartnerOrganizations

**Request Payload**

```json
{
  "name": "Green Recycling",
  "organizationType": "Recycling",
  "phoneNumber": "0123456789",
  "email": "green@example.com",
  "address": "Ho Chi Minh City"
}
```

**Sample Response**

```json
{
  "id": "uuid",
  "message": "Partner organization created"
}
```

## 11.2 Get Partner Organizations

**Endpoint**

```
GET /api/v1/partner-organizations
```

**Related Tables**
- PartnerOrganizations

**Sample Response**

```json
[
  {
    "id": "uuid",
    "name": "Hope Charity",
    "organizationType": "Charity"
  }
]
```

---

# 12. Notification APIs

## 12.1 Get My Notifications

**Endpoint**

```
GET /api/v1/notifications
```

**Related Tables**
- Notifications

**Sample Response**

```json
[
  {
    "id": "uuid",
    "title": "New Donation Request",
    "message": "A new donation request was created",
    "isRead": false
  }
]
```

## 12.2 Mark Notification As Read

**Endpoint**

```
PUT /api/v1/notifications/{id}/read
```

**Related Tables**
- Notifications

**Sample Response**

```json
{
  "message": "Notification marked as read"
}
```

---

# 13. Workflow & Audit APIs

## 13.1 Get Batch Status History

**Endpoint**

```
GET /api/v1/intake-batches/{id}/status-history
```

**Related Tables**
- BatchStatusHistories
- IntakeBatches
- Users

**Sample Response**

```json
[
  {
    "oldStatus": "Received",
    "newStatus": "UnderClassification",
    "changedBy": "Warehouse Staff",
    "changedAt": "2026-05-11T10:00:00Z"
  }
]
```

## 13.2 Get Audit Logs

**Endpoint**

```
GET /api/v1/audit-logs
```

**Related Tables**
- AuditLogs
- Users

**Sample Response**

```json
[
  {
    "action": "UPDATE_BATCH_STATUS",
    "entityName": "IntakeBatch",
    "createdAt": "2026-05-11T10:00:00Z"
  }
]
```

---

# 14. Clothing Category APIs

## 14.1 Create Clothing Category

**Endpoint**

```
POST /api/v1/clothing-categories
```

**Related Tables**
- ClothingCategories

**Request Payload**

```json
{
  "name": "T-Shirt",
  "description": "Casual shirts"
}
```

**Sample Response**

```json
{
  "id": "uuid",
  "message": "Category created"
}
```

## 14.2 Get Clothing Categories

**Endpoint**

```
GET /api/v1/clothing-categories
```

**Related Tables**
- ClothingCategories

**Sample Response**

```json
[
  {
    "id": "uuid",
    "name": "T-Shirt"
  }
]
```

---

# 15. Recommended API Security

The system should implement:

- JWT Bearer Authentication
- Role-based authorization
- API validation
- Request throttling
- Global exception handling
- Audit logging middleware

---

# 16. Recommended Authorization Rules

| API Module | Allowed Roles |
|---|---|
| Donation Requests | Donor, ReceivingStaff, Manager |
| Intake Batches | ReceivingStaff |
| Classification | ClassificationStaff |
| Warehouse | WarehouseStaff |
| Distribution | WarehouseStaff, Manager |
| Recycling | WarehouseStaff, Manager |
| Audit Logs | Manager, SystemAdmin |
| Administration | SystemAdmin |
