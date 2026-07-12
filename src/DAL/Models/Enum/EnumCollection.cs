namespace DAL.Models.Enum
{
    public enum DonationRequestStatus
    {
        PendingStaffAssign,
        ReceivingStaffAssigned,
        WaitingReceivingStaff,
        Confirmed,
        Reject,
        Cancelled,
        SendToClassification,
        Classifying,
        Classified,
        Stored,
    }

    public enum ClothCondition
    {
        Good,
        Fair,
        Damaged,
        Unusable
    }

    public enum ClothGender
    {
        Male,
        Female,
        Unisex
    }

    public enum ClothSize
    {
        Unknown = 0,
        Baby,
        Kid,
        XS,
        S,
        M,
        L,
        XL,
        XXL,
    }

    public enum ClothTargetAge
    {
        Adult,
        Children
    }
}
