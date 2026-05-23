namespace Reshape.ElectricAi.Core.Services;

public interface IPasswordHasher
{
    PasswordHashResult Hash(string password);

    bool Verify(string password, string hash, byte[] salt);

    void VerifyDummy();
}
