using System.Security.Cryptography;
using System.Text;
using EquipmentIMS.BLL.Interfaces;

namespace EquipmentIMS.BLL.Services;

public class AesEncryptionService : IAesEncryptionService
{
    private readonly byte[] _key;
    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("EquipmentIMS.Salt");

    public AesEncryptionService()
    {
        var keyStr = Environment.GetEnvironmentVariable("EQUIPMENT_AES_KEY") ?? "EquipmentIMS.DefaultAesKey2024!@#";
        _key = DeriveKey(keyStr);
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        var fullBytes = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = _key;
        var iv = new byte[16];
        Buffer.BlockCopy(fullBytes, 0, iv, 0, 16);
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = new byte[fullBytes.Length - 16];
        Buffer.BlockCopy(fullBytes, 16, cipherBytes, 0, cipherBytes.Length);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[] DeriveKey(string password)
    {
        using var derive = new Rfc2898DeriveBytes(password, Salt, 10000, HashAlgorithmName.SHA256);
        return derive.GetBytes(32);
    }
}
