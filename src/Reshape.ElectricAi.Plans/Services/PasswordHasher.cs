using System.Security.Cryptography;
using BCrypt.Net;
using Reshape.ElectricAi.Core.Services;

namespace Reshape.ElectricAi.Plans.Services;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;
    private const int SaltSizeBytes = 16;

    private readonly string _dummyHash;
    private readonly byte[] _dummySalt;

    public PasswordHasher()
    {
        _dummySalt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        _dummyHash = BCrypt.Net.BCrypt.HashPassword(BuildInput("dummy-password-for-constant-time", _dummySalt), WorkFactor);
    }

    public PasswordHashResult Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = BCrypt.Net.BCrypt.HashPassword(BuildInput(password, salt), WorkFactor);
        return new PasswordHashResult(hash, salt);
    }

    public bool Verify(string password, string hash, byte[] salt)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash) || salt.Length == 0)
        {
            VerifyDummy();
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(BuildInput(password, salt), hash);
        }
        catch (SaltParseException)
        {
            return false;
        }
    }

    public void VerifyDummy()
    {
        try
        {
            _ = BCrypt.Net.BCrypt.Verify(BuildInput("dummy-password-for-constant-time", _dummySalt), _dummyHash);
        }
        catch (SaltParseException)
        {
            // swallowed: dummy verify exists solely for constant-time path
        }
    }

    private static string BuildInput(string password, byte[] salt) =>
        password + Convert.ToBase64String(salt);
}
