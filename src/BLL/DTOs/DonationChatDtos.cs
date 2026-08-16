namespace BLL.DTOs;

public record SendDonationChatMessageDto(string Message);
public record DonationChatMessageDto(Guid Id, Guid SenderId, string SenderName, string SenderRole,
    string Message, DateTime SentAt, bool IsMine);
public record DonationChatConversationDto(Guid RequestId, string RequestCode, string ParticipantLabel,
    string? AvatarUrl, string? LastMessage, DateTime? LastMessageAt);
