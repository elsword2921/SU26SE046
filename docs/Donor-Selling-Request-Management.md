```mermaid
flowchart LR

subgraph Donor
    D1[Create Selling Request]
    D2[Update Selling Request]
    D3[View Selling Request]
    D4[Cancel Selling Request]
end

subgraph System
    S1[Validate Selling Request]
    S2[Store Selling Request]
    S3[Update Selling Request Information]
    S4[Display Selling Request]
    S5[Update Request Status]
end

D1 --> S1
S1 --> S2
S2 --> D3
D3 --> S4
D2 --> S3
D4 --> S5
```