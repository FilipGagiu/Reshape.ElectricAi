namespace Reshape.ElectricAi.Core.Dtos.Groups;

public sealed record GroupMemberDto(Guid UserId, string Email, DateTime JoinedUtc);
