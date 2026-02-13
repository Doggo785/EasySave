using EasySave.Core.Properties;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EasySave.Core.Services
{
    public class CryptoService
    {
        private const int SaltSize = 16;

        private const int Iterations = 100_000;

        public static int EncryptFile(string sourcePath, string destPath, string password)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destPath) || string.IsNullOrWhiteSpace(password))
                throw new ArgumentException(Resources.ThrowWrongInput);

            if (!File.Exists(sourcePath))
                return -2;

            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                byte[] salt = new byte[SaltSize];
                using (var rng = RandomNumberGenerator.Create())
                    rng.GetBytes(salt);

                string? destDirectory = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrWhiteSpace(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

                string outputPath = destPath;
                bool replaceOriginal = string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destPath), StringComparison.OrdinalIgnoreCase);
                if (replaceOriginal)
                {
                    string fileName = Path.GetFileName(destPath);
                    outputPath = Path.Combine(destDirectory ?? Path.GetTempPath(), $"{fileName}.{Guid.NewGuid():N}.tmp");
                }

                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256;

                    using (var key = new Rfc2898DeriveBytes(password, salt, Iterations))
                    {
                        aes.Key = key.GetBytes(32);
                        aes.IV = key.GetBytes(16);
                    }

                    using (FileStream fsCrypt = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        fsCrypt.Write(salt, 0, salt.Length);

                        using (CryptoStream cryptoStream = new CryptoStream(
                            fsCrypt,
                            aes.CreateEncryptor(),
                            CryptoStreamMode.Write))
                        using (FileStream fsIn = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            fsIn.CopyTo(cryptoStream);
                        }
                    }
                }

                if (replaceOriginal)
                {
                    File.Copy(outputPath, destPath, true);
                    File.Delete(outputPath);
                }

                stopwatch.Stop();
                return (int)stopwatch.ElapsedMilliseconds;
            }
            catch
            {
                return -1;
            }
        }

        public static int DecryptFile(string encryptedPath, string destPath, string password)
        {
            if (string.IsNullOrWhiteSpace(encryptedPath) || string.IsNullOrWhiteSpace(destPath) || string.IsNullOrWhiteSpace(password))
                throw new ArgumentException(Resources.ThrowWrongInput);

            if (!File.Exists(encryptedPath))
                return -2;

            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                string? destDirectory = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrWhiteSpace(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

                string outputPath = destPath;
                bool replaceOriginal = string.Equals(Path.GetFullPath(encryptedPath), Path.GetFullPath(destPath), StringComparison.OrdinalIgnoreCase);
                if (replaceOriginal)
                {
                    string fileName = Path.GetFileName(destPath);
                    outputPath = Path.Combine(destDirectory ?? Path.GetTempPath(), $"{fileName}.{Guid.NewGuid():N}.tmp");
                }

                using (FileStream fsCrypt = new FileStream(encryptedPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] salt = new byte[SaltSize];
                    fsCrypt.Read(salt, 0, SaltSize);

                    using (Aes aes = Aes.Create())
                    {
                        using (var key = new Rfc2898DeriveBytes(password, salt, Iterations))
                        {
                            aes.Key = key.GetBytes(32);
                            aes.IV = key.GetBytes(16);
                        }

                        using (CryptoStream cryptoStream = new CryptoStream(
                            fsCrypt,
                            aes.CreateDecryptor(),
                            CryptoStreamMode.Read))
                        using (FileStream fsOut = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            cryptoStream.CopyTo(fsOut);
                        }
                    }
                }

                if (replaceOriginal)
                {
                    File.Copy(outputPath, destPath, true);
                    File.Delete(outputPath);
                }

                stopwatch.Stop();
                return (int)stopwatch.ElapsedMilliseconds;
            }
            catch
            {
                return -1; 
            }
        }
    }
}
