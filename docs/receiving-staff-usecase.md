# Receiving Staff Use Cases
## Used Clothing Lifecycle Management System

---

# 1. Core Use Cases

These are the most important business use cases for the Receiving Staff role.

---

## 1. Receive Donation/Selling Request

### Description
Receiving Staff receives and reviews donation or selling requests created by donors.

### Main Activities
- View donor request
- Review donor information
- Review estimated weight
- Review pickup address
- Accept or reject request

### Status Transition
```text
Pending → ReceivedCompleted
Pending → Rejected
```

---

## 2. Verify Clothing Information

### Description
Receiving Staff verifies the actual clothing information during pickup or receiving process.

### Main Activities
- Verify clothing quantity
- Check clothing condition
- Compare actual items with request
- Record discrepancies

---

## 3. Measure Actual Weight

### Description
Receiving Staff measures the actual weight of clothing received from donor.

### Main Activities
- Record actual weight
- Compare with estimated weight
- Update receiving information

### Important Data
- ActualWeight
- MeasurementDate

---

## 4. Accept Request

### Description
Receiving Staff confirms the clothing request is accepted after verification.

### Status Transition
```text
Pending → ReceivedCompleted
```

---

## 5. Reject Request

### Description
Receiving Staff rejects invalid or unsuitable requests.

### Possible Reasons
- Clothing too damaged
- Invalid information
- Unsafe materials

### Status Transition
```text
Pending → Rejected
```

---

## 6. Process Donor Payment

### Description
Receiving Staff processes payment for selling requests.

### Main Activities
- Calculate payment amount
- Confirm payment
- Record payment transaction
- Generate payment record

### Applies To
```text
Selling Request Only
```

---

## 7. Create Intake Batch

### Description
Receiving Staff creates an intake batch after receiving clothing.

### Main Activities
- Generate batch code
- Record quantity
- Add notes
- Assign receiver information
- Upload images

### Batch Status
```text
PendingClassification
```

---

## 8. Upload Batch Images

### Description
Receiving Staff uploads images of received clothing batches.

### Purpose
- Audit support
- Evidence tracking
- Batch verification

---

## 9. Record Receiving Notes

### Description
Receiving Staff records important notes related to the received batch.

### Example Notes
- Damaged items
- Missing quantity
- Special handling requirements

---

## 10. Transfer Batch to Classification

### Description
Receiving Staff transfers intake batch to Classification Staff.

### Main Activities
- Mark batch ready
- Update batch status
- Notify classification department

### Status Transition
```text
PendingClassification → Classifying
```

---

# 2. Supporting Use Cases

These use cases support the main workflow.

---

## 11. View Assigned Requests

### Description
Receiving Staff views assigned donation or selling requests.

---

## 12. View Request Detail

### Description
Receiving Staff views detailed donor request information.

### Includes
- Donor information
- Address
- Estimated weight
- Photos

---

## 13. Search Donation Requests

### Description
Receiving Staff searches requests by:
- Donor
- Date
- Status

---

## 14. Schedule Pickup

### Description
Receiving Staff schedules pickup time for donor requests.

### Main Activities
- Select pickup date
- Assign receiving staff
- Confirm schedule

---

## 15. Confirm Clothing Pickup

### Description
Receiving Staff confirms clothing has been picked up from donor.

---

## 16. Generate Receiving Record

### Description
System generates receiving document or record.

### Includes
- Donor information
- Weight
- Payment
- Received date

---

## 17. Print / Export Receiving Receipt

### Description
Receiving Staff exports or prints receiving receipt.

### Possible Formats
- PDF
- Printed receipt

---

## 18. Track Intake Batch Status

### Description
Receiving Staff tracks intake batch progress.

### Example Statuses
```text
PendingClassification
Classifying
WarehouseReceived
```

---

## 19. View Receiving History

### Description
Receiving Staff views history of previously processed batches.

---

## 20. Update Intake Information

### Description
Receiving Staff updates intake information before classification begins.

### Editable Information
- Quantity
- Notes
- Images

---

# 3. Advanced / Enterprise Use Cases

These use cases are optional but suitable for larger systems.

---

## 21. Assign Receiving Priority

### Description
Receiving Staff assigns priority level to requests.

### Example Priorities
- Urgent charity
- Large quantity

---

## 22. Report Invalid Donation

### Description
Receiving Staff reports invalid or unsafe donations.

### Examples
- Dirty clothes
- Hazardous materials
- Restricted items

---

## 23. Record Transportation Information

### Description
Receiving Staff records transportation details.

### Includes
- Vehicle
- Driver
- Transportation cost

---

## 24. Capture Donor Signature

### Description
Receiving Staff captures donor signature during receiving process.

---

## 25. Generate Audit Log

### Description
System records receiving activities for audit purposes.

### Audit Information
- User
- Timestamp
- Action performed

---

## 26. Notify Classification Staff

### Description
System notifies Classification Staff after batch transfer.

---

## 27. Cancel Intake Batch

### Description
Receiving Staff cancels intake batch if created incorrectly.

---

## 28. Split Intake Batch

### Description
Receiving Staff splits a large intake batch into smaller batches.

---

## 29. Merge Intake Batches

### Description
Receiving Staff merges multiple intake batches into one batch.

---

## 30. Upload Evidence Documents

### Description
Receiving Staff uploads supporting documents.

### Example Documents
- Payment receipt
- Transportation invoice
- Receiving confirmation

---

# 4. Recommended Use Cases for Main Diagram

The following use cases are highly recommended for the main Use Case Diagram.

| Priority | Use Case |
|---|---|
| Highest | Receive Donation/Selling Request |
| Highest | Verify Clothing Information |
| Highest | Process Donor Payment |
| Highest | Create Intake Batch |
| Highest | Transfer Batch to Classification |
| High | Upload Batch Images |
| High | Schedule Pickup |
| High | View Assigned Requests |
| Medium | Generate Receiving Record |
| Medium | Track Intake Batch Status |

---

# 5. Recommendation

## Small / Medium Use Case Diagram
Recommended:
```text
5–8 use cases
```

## Full Business Analysis Diagram
Recommended:
```text
10–15 use cases
```

## Avoid
```text
20+ use cases for one actor
```

because the diagram becomes too complex and difficult to read.