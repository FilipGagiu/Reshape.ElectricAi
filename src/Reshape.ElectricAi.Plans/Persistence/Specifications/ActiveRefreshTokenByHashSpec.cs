using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Specifications;

public sealed class ActiveRefreshTokenByHashSpec : Specification<RefreshToken>
{
    public ActiveRefreshTokenByHashSpec(string tokenHash, DateTime nowUtc)
    {
        Where(rt => rt.TokenHash == tokenHash
                 && rt.RevokedUtc == null
                 && rt.ExpiresUtc > nowUtc);
        AddInclude(rt => rt.User!);
    }
}
