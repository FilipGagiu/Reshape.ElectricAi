using Reshape.ElectricAi.Core.Dtos.Auth;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Extensions;

internal static class UserMappingExtensions
{
    public static UserDto ToUserDto(this User user) =>
        new(user.Id, user.Email, user.Role);
}
