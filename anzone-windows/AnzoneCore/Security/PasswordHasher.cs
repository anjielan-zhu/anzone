using System.Security.Cryptography;

namespace AnzoneCore.Security;

public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int KeyLength = 32;   // 256-bit
    private const int SaltLength = 16;

    public static byte[] GenerateSalt() => RandomNumberGenerator.GetBytes(SaltLength);

    public static byte[] Hash(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeyLength);

    public static bool Verify(string password, byte[] salt, byte[] expected) =>
        CryptographicOperations.FixedTimeEquals(Hash(password, salt), expected);
}
