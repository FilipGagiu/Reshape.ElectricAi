using Reshape.ElectricAi.Plans.Services;

namespace Reshape.ElectricAi.Plans.Tests.Unit.Services;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesDifferentSaltPerCall()
    {
        var first = _hasher.Hash("CorrectHorseBattery1!");
        var second = _hasher.Hash("CorrectHorseBattery1!");

        first.Salt.Should().NotBeEquivalentTo(second.Salt);
        first.Hash.Should().NotBe(second.Hash);
    }

    [Fact]
    public void Verify_AcceptsOriginalPassword()
    {
        var result = _hasher.Hash("CorrectHorseBattery1!");

        _hasher.Verify("CorrectHorseBattery1!", result.Hash, result.Salt).Should().BeTrue();
    }

    [Fact]
    public void Verify_RejectsWrongPassword()
    {
        var result = _hasher.Hash("CorrectHorseBattery1!");

        _hasher.Verify("DifferentPassword2@", result.Hash, result.Salt).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithEmptyHash_ReturnsFalseAndDoesNotThrow()
    {
        var action = () => _hasher.Verify("anything", string.Empty, []);

        action.Should().NotThrow();
        action().Should().BeFalse();
    }

    [Fact]
    public void VerifyDummy_DoesNotThrow()
    {
        var action = () => _hasher.VerifyDummy();

        action.Should().NotThrow();
    }
}
