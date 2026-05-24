using AnzoneCore.Security;
using Xunit;

public class PasswordHasherTests
{
    [Fact]
    public void SameInputsProduceSameHash()
    {
        var salt = PasswordHasher.GenerateSalt();
        Assert.Equal(PasswordHasher.Hash("secret123", salt), PasswordHasher.Hash("secret123", salt));
    }

    [Fact]
    public void DifferentSaltDiffersHash()
    {
        Assert.NotEqual(
            PasswordHasher.Hash("secret", PasswordHasher.GenerateSalt()),
            PasswordHasher.Hash("secret", PasswordHasher.GenerateSalt()));
    }

    [Fact]
    public void VerifyAcceptsCorrect()
    {
        var salt = PasswordHasher.GenerateSalt();
        var hash = PasswordHasher.Hash("correct", salt);
        Assert.True(PasswordHasher.Verify("correct", salt, hash));
    }

    [Fact]
    public void VerifyRejectsWrong()
    {
        var salt = PasswordHasher.GenerateSalt();
        var hash = PasswordHasher.Hash("correct", salt);
        Assert.False(PasswordHasher.Verify("wrong", salt, hash));
    }
}
