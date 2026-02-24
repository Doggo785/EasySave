using EasySave.Core.Services;
using FluentAssertions;
using System;
using System.IO;
using Xunit;

namespace EasySave.Tests
{
    public class CryptoServiceTests
    {
        [Fact]
        public void EncryptFile_FileValid_PositiveTimeReturn()
        {
            string sourcePath = Path.GetTempFileName();
            string destPath = Path.GetTempFileName();
            File.WriteAllText(sourcePath, "Secret data to be encrypted");

            try
            {
                int result = CryptoService.EncryptFile(sourcePath, destPath, "MotDePasse123!");

                result.Should().BeGreaterThanOrEqualTo(0,
                    because: "the encryption should return the elapsed time in milliseconds (>= 0); a negative value indicates an error");
            }
            finally
            {
                File.Delete(sourcePath);
                File.Delete(destPath);
            }
        }

        [Fact]
        public void EncryptFile_FileNonexistent_ReturnMinusTwo()
        {
            string sourcePath = @"C:\FichierQuiNExistePas_XYZ.txt";
            string destPath = Path.GetTempFileName();

            try
            {
                int result = CryptoService.EncryptFile(sourcePath, destPath, "MotDePasse");

                result.Should().Be(-2,
                    because: "The code -2 means that the source file cannot be found on the disk");
            }
            finally
            {
                File.Delete(destPath);
            }
        }

        [Fact]
        public void EncryptFile_EmptyPassword_LeveException()
        {
            string sourcePath = Path.GetTempFileName();

            try
            {
                var act = () => CryptoService.EncryptFile(sourcePath, sourcePath, "");

                act.Should().Throw<ArgumentException>(
                    because: "an empty password is not acceptable for encryption; it is mandatory");
            }
            finally
            {
                File.Delete(sourcePath);
            }
        }

        [Fact]
        public void DecryptFile_IncorrectPassword_ReturnMinusOne()
        {
            string sourcePath = Path.GetTempFileName();
            string encryptedPath = Path.GetTempFileName();
            string decryptedPath = Path.GetTempFileName();
            File.WriteAllText(sourcePath, "Confidential content");
            CryptoService.EncryptFile(sourcePath, encryptedPath, "GoodPassword");

            try
            {
                int result = CryptoService.DecryptFile(encryptedPath, decryptedPath, "IncorrectPassword");

                result.Should().Be(-1,
                    because: "the code -1 means that decryption failed, probably due to an incorrect password");
            }
            finally
            {
                File.Delete(sourcePath);
                File.Delete(encryptedPath);
                if (File.Exists(decryptedPath)) File.Delete(decryptedPath);
            }
        }

        [Fact]
        public void EncryptThenDecrypt_OriginalContentRetrieved()
        {
            string originalContent = "Top secret content to be protected !";
            string sourcePath = Path.GetTempFileName();
            string encryptedPath = Path.GetTempFileName();
            string decryptedPath = Path.GetTempFileName();
            string password = "SupersecretPassword!123";
            File.WriteAllText(sourcePath, originalContent);

            try
            {
                int encryptResult = CryptoService.EncryptFile(sourcePath, encryptedPath, password);
                int decryptResult = CryptoService.DecryptFile(encryptedPath, decryptedPath, password);
                string recoveredContent = File.ReadAllText(decryptedPath);

                encryptResult.Should().BeGreaterThanOrEqualTo(0,
                    because: "the encryption must succeed before the decryption can be tested.");
                decryptResult.Should().BeGreaterThanOrEqualTo(0,
                    because: "Decryption with the correct password should always succeed");
                recoveredContent.Should().Be(originalContent,
                    because: "After encryption and then decryption with the same password, the content must be identical to the original");
            }
            finally
            {
                File.Delete(sourcePath);
                File.Delete(encryptedPath);
                File.Delete(decryptedPath);
            }
        }

        [Fact]
        public void EncryptFile_FileDigitIsDifferentFromOriginal()
        {
            string originalContent = "Plain text";
            string sourcePath = Path.GetTempFileName();
            string destPath = Path.GetTempFileName();
            File.WriteAllText(sourcePath, originalContent);

            try
            {
                CryptoService.EncryptFile(sourcePath, destPath, "password");
                string encryptedContent = File.ReadAllText(destPath);

                encryptedContent.Should().NotBe(originalContent,
                    because: "an encrypted file should never be identical to the original unencrypted file; otherwise, the encryption is ineffective.");
            }
            finally
            {
                File.Delete(sourcePath);
                File.Delete(destPath);
            }
        }
    }
}