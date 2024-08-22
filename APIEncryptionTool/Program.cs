using System;
using System.Security.Cryptography;
using System.Text;

public class ApiAddressEncryptionTool
{
    // You should generate these randomly and keep them secure
    private static readonly byte[] Key = new byte[32] 
    {
        23, 124, 67, 88, 190, 12, 45, 91,
        255, 7, 89, 45, 168, 42, 109, 187,
        23, 100, 76, 217, 154, 200, 43, 79,
        19, 176, 62, 9, 201, 33, 95, 128
    };
    private static readonly byte[] IV = new byte[16]
    {
        88, 145, 23, 200, 56, 178, 12, 90,
        167, 34, 78, 191, 78, 23, 12, 78
    };

    public static string Encrypt(string plainText)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
            aesAlg.IV = IV;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (var msEncrypt = new System.IO.MemoryStream())
            {
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }
                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }
    }

    public static string Decrypt(string cipherText)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
            aesAlg.IV = IV;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (var msDecrypt = new System.IO.MemoryStream(Convert.FromBase64String(cipherText)))
            using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
            {
                return srDecrypt.ReadToEnd();
            }
        }
    }

    public static void Main(string[] args)
    {
        Console.WriteLine("API Address Encryption/Decryption Tool");
        Console.WriteLine("=====================================");
        Console.WriteLine("1. Encrypt API Address");
        Console.WriteLine("2. Decrypt API Address");
        Console.Write("Choose an option (1 or 2): ");

        string choice = Console.ReadLine();

        if (choice == "1")
        {
            Console.Write("Enter the API address to encrypt: ");
            string apiAddress = Console.ReadLine();
            string encrypted = Encrypt(apiAddress);
            Console.WriteLine($"Encrypted API address: {encrypted}");
        }
        else if (choice == "2")
        {
            Console.Write("Enter the encrypted API address: ");
            string encryptedAddress = Console.ReadLine();
            string decrypted = Decrypt(encryptedAddress);
            Console.WriteLine($"Decrypted API address: {decrypted}");
        }
        else
        {
            Console.WriteLine("Invalid option. Please run the program again and choose 1 or 2.");
        }
    }
}