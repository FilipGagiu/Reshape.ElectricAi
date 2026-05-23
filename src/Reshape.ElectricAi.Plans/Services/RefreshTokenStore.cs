using Microsoft.EntityFrameworkCore;
using Reshape.ElectricAi.Plans.Entities;
using Reshape.ElectricAi.Plans.Persistence;

namespace Reshape.ElectricAi.Plans.Services;

public sealed class RefreshTokenStore(PlansDbContext context) : IRefreshTokenStore
{
    public async Task<RotatedRefreshToken?> ClaimAndRotateAsync(
        string incomingHash,
        string newTokenHash,
        DateTime newTokenExpiresUtc,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var claimed = await context.RefreshTokens
            .Where(rt => rt.TokenHash == incomingHash
                      && rt.RevokedUtc == null
                      && rt.ExpiresUtc > nowUtc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.RevokedUtc, (DateTime?)nowUtc)
                .SetProperty(rt => rt.ReplacedByHash, newTokenHash),
                cancellationToken);

        if (claimed == 0)
        {
            return null;
        }

        var subject = await context.RefreshTokens
            .AsNoTracking()
            .Where(rt => rt.TokenHash == incomingHash)
            .Select(rt => new { rt.UserId, Email = rt.User!.Email, rt.User.Role })
            .FirstOrDefaultAsync(cancellationToken);

        if (subject is null)
        {
            return null;
        }

        context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = subject.UserId,
            TokenHash = newTokenHash,
            CreatedUtc = nowUtc,
            ExpiresUtc = newTokenExpiresUtc
        });

        await context.SaveChangesAsync(cancellationToken);

        return new RotatedRefreshToken(subject.UserId, subject.Email, subject.Role);
    }
}
