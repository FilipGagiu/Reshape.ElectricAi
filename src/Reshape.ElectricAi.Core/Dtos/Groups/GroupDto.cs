namespace Reshape.ElectricAi.Core.Dtos.Groups;

public sealed record GroupDto(
    Guid Id,
    string Name,
    Guid OwnerUserId,
    DateTime CreatedUtc,
    IReadOnlyList<GroupMemberDto> Members);
