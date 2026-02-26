using EasySave.Core.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace EasySave.Core.Services
{
    public class CryptoService
    {
        private const int SaltSize = 16;

        private const int Iterations = 100_000;

        /// <summary>
        /// Encrypts a file using the AES-256 algorithm.
        /// </summary>
        /// <param name="sourcePath">Path of the source file to encrypt.</param>
        /// <param name="destPath">Path of the destination file.</param>
        /// <param name="password">Encryption password.</param>
        /// <returns>Execution time (ms), or error code.</returns>
        /// <exception cref="ArgumentException">Thrown if any path is empty.</exception>
        public static int EncryptFile(string sourcePath, string destPath, string password)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destPath) || string.IsNullOrWhiteSpace(password))
                throw new ArgumentException(Resources.ThrowWrongInput);

            if (!File.Exists(sourcePath))
                return -2;

            return ExecuteTimed(() =>
            {
                var (outputPath, replaceOriginal) = PrepareOutputPath(sourcePath, destPath);

                byte[] salt = new byte[SaltSize];
                using (var rng = RandomNumberGenerator.Create())
                    rng.GetBytes(salt);

                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256;

                    using (var key = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
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

                FinalizeOutput(outputPath, destPath, replaceOriginal);
            });
        }

        /// <summary>
        /// Decrypts a file using the AES-256 algorithm.
        /// </summary>
        /// <param name="encryptedPath">Path of the source encrypted file.</param>
        /// <param name="destPath">Path of the destination file.</param>
        /// <param name="password">Decryption password.</param>
        /// <returns>Execution time (ms), or error code.</returns>
        /// <exception cref="ArgumentException">Thrown if any path is empty.</exception>
        public static int DecryptFile(string encryptedPath, string destPath, string password)
        {
            if (string.IsNullOrWhiteSpace(encryptedPath) || string.IsNullOrWhiteSpace(destPath) || string.IsNullOrWhiteSpace(password))
                throw new ArgumentException(Resources.ThrowWrongInput);

            if (!File.Exists(encryptedPath))
                return -2;

            return ExecuteTimed(() =>
            {
                var (outputPath, replaceOriginal) = PrepareOutputPath(encryptedPath, destPath);

                using (FileStream fsCrypt = new FileStream(encryptedPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] salt = new byte[SaltSize];
                    fsCrypt.ReadExactly(salt, 0, SaltSize);

                    using (Aes aes = Aes.Create())
                    {
                        using (var key = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
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

                FinalizeOutput(outputPath, destPath, replaceOriginal);
            });
        }

        private static (string outputPath, bool replaceOriginal) PrepareOutputPath(string sourcePath, string destPath)
        {
            string? destDirectory = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrWhiteSpace(destDirectory))
                Directory.CreateDirectory(destDirectory);

            bool replaceOriginal = string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destPath), StringComparison.OrdinalIgnoreCase);
            string outputPath = destPath;
            if (replaceOriginal)
            {
                string fileName = Path.GetFileName(destPath);
                outputPath = Path.Combine(destDirectory ?? Path.GetTempPath(), $"{fileName}.{Guid.NewGuid():N}.tmp");
            }

            return (outputPath, replaceOriginal);
        }

        private static void FinalizeOutput(string outputPath, string destPath, bool replaceOriginal)
        {
            if (replaceOriginal)
                File.Move(outputPath, destPath, true);
        }

        private static int ExecuteTimed(Action action)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                action();
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