using System.Collections.Concurrent;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using NLog;
using TPEmail.BusinessModels.ResponseModels;
using TPEmail.BusinessModels.Constants;

namespace TPEmail.Common.Helpers
{
    public static class EncryptionHelper
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static KeyConfigProvider? _provider;

        private static KeyConfig? _activeKeyConfig;
        private static List<KeyConfig>? _allKeyConfigs;
        private static readonly ConcurrentDictionary<int, byte[]> _derivedKeys = new();
        private static readonly object _configLock = new();
        private const int MinEncryptedBase64Length = 40;

        public static void Initialize(KeyConfigProvider provider)
        {
            _provider = provider;
        }

        public static void Initialize(Func<IDbConnection> dbFactory)
        {
            _provider = new KeyConfigProvider(dbFactory);
        }

        #region Config Loading

        private static void EnsureConfig()
        {
            if (_activeKeyConfig != null) return;

            lock (_configLock)
            {
                if (_activeKeyConfig != null) return;
                LoadAllConfigs();
            }
        }

        private static void LoadAllConfigs()
        {
            if (_provider == null)
                throw new InvalidOperationException(MessageConstants.EncryptionKeyNotLoaded);

            _allKeyConfigs = _provider.GetAllKeys();
            _activeKeyConfig = _provider.GetActiveKey();
            _logger.Info("EncryptionHelper keys loaded via {Source}", _provider.ResolvedSource);
        }

        public static void ReloadConfigs()
        {
            lock (_configLock)
            {
                _activeKeyConfig = null;
                _allKeyConfigs = null;
                _derivedKeys.Clear();
                _provider?.Invalidate();
            }
        }

        #endregion

        #region Key Derivation

        private static byte[] GetDerivedKey(KeyConfig keyConfig)
        {
            return _derivedKeys.GetOrAdd(keyConfig.Id, _ =>
            {
                return Rfc2898DeriveBytes.Pbkdf2(
                    keyConfig.EncryptionKey,
                    keyConfig.SaltBytes,
                    1000,
                    HashAlgorithmName.SHA256,
                    32);
            });
        }

        public static int GetActiveKeyVersion()
        {
            EnsureConfig();
            return _activeKeyConfig!.Id;
        }

        #endregion

        #region Encryption

        public static string DataEncryptAsync(string clearText, object? errorLog = null, string? path = null, string? user = null)
        {
            try
            {
                EnsureConfig();
                var keyConfig = _activeKeyConfig ?? throw new InvalidOperationException(MessageConstants.EncryptionKeyNotLoaded);

                byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
                byte[] key = GetDerivedKey(keyConfig);

                byte[] iv = new byte[12];
                using (var rng = RandomNumberGenerator.Create())
                    rng.GetBytes(iv);

                using (var aes = new AesGcm(key, 16))
                {
                    byte[] tag = new byte[16];
                    byte[] cipherBytes = new byte[clearBytes.Length];

                    aes.Encrypt(iv, clearBytes, cipherBytes, tag);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        ms.Write(iv, 0, iv.Length);
                        ms.Write(cipherBytes, 0, cipherBytes.Length);
                        ms.Write(tag, 0, tag.Length);
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format(MessageConstants.EncryptionFailedFormat, ex.Message);
                throw new Exception(errorMsg, ex);
            }
        }

        #endregion

        #region Decryption

        private static string DecryptWithKey(string cipherText, KeyConfig keyConfig)
        {
            byte[] encryptedData = Convert.FromBase64String(cipherText);

            if (encryptedData.Length < 28)
                throw new ArgumentException("Data too short for AES-GCM format");

            byte[] iv = new byte[12];
            byte[] tag = new byte[16];
            byte[] cipher = new byte[encryptedData.Length - 28];

            Array.Copy(encryptedData, 0, iv, 0, 12);
            Array.Copy(encryptedData, 12, cipher, 0, cipher.Length);
            Array.Copy(encryptedData, encryptedData.Length - 16, tag, 0, 16);

            byte[] key = GetDerivedKey(keyConfig);

            byte[] plainBytes = new byte[cipher.Length];
            using (var aes = new AesGcm(key, 16))
            {
                aes.Decrypt(iv, cipher, tag, plainBytes);
            }

            return Encoding.Unicode.GetString(plainBytes);
        }

        public static string DataDecrypt(string cipherText, object? errorLog = null, string? path = null, string? user = null)
        {
            try
            {
                EnsureConfig();
                return DecryptWithKey(cipherText, _activeKeyConfig!);
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format(MessageConstants.DecryptionFailedFormat, ex.Message);
                throw new Exception(errorMsg, ex);
            }
        }

        public static string DecryptOrDefault(string? cipherText, object? errorLog = null, string? callerContext = null, string? userId = null)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText ?? string.Empty;
            if (!IsEncryptedFormat(cipherText)) return cipherText;

            try
            {
                EnsureConfig();
                return DecryptWithKey(cipherText, _activeKeyConfig!);
            }
            catch
            {
                return TryDecryptWithAllKeys(cipherText, callerContext, userId);
            }
        }

        private static string TryDecryptWithAllKeys(string cipherText, string? callerContext = null, string? userId = null)
        {
            if (_allKeyConfigs == null || _allKeyConfigs.Count <= 1)
            {
                _logger.Warn("Decryption failed with active key and no alternate keys available. " +
                    "Caller: {Caller}, UserId: {UserId}, CipherLength: {Length}. " +
                    "Data may have been encrypted with a key that was deleted.",
                    callerContext ?? "unknown", userId ?? "unknown", cipherText?.Length ?? 0);
                return cipherText;
            }

            foreach (var keyConfig in _allKeyConfigs)
            {
                if (keyConfig.Id == _activeKeyConfig?.Id)
                    continue;

                try
                {
                    string result = DecryptWithKey(cipherText, keyConfig);
                    _logger.Info("Decrypted with alternate key version {KeyId} (caller: {Caller})",
                        keyConfig.Id, callerContext ?? "unknown");
                    return result;
                }
                catch
                {
                    continue;
                }
            }

            _logger.Warn("All {Count} key versions failed for decryption. " +
                "Caller: {Caller}, UserId: {UserId}, CipherLength: {Length}. " +
                "Data is orphaned - encrypted with a deleted key.",
                _allKeyConfigs.Count, callerContext ?? "unknown",
                userId ?? "unknown", cipherText?.Length ?? 0);
            return cipherText;
        }

        #endregion

        #region Blind Index

        public static string GenerateBlindIndex(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                EnsureConfig();
                var keyConfig = _activeKeyConfig ?? throw new InvalidOperationException(MessageConstants.EncryptionKeyNotLoaded);

                string normalizedText = plainText.Trim().ToLowerInvariant();

                using (var hmac = new HMACSHA256(keyConfig.SaltBytes))
                {
                    byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(normalizedText));
                    return Convert.ToBase64String(hashBytes);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GenerateBlindIndex: {ex.Message}", ex);
            }
        }

        #endregion

        #region Utility

        public static bool IsEncryptedFormat(string? text)
        {
            if (string.IsNullOrEmpty(text) || text.Length % 4 != 0) return false;
            if (text.Length < MinEncryptedBase64Length) return false;
            if (text.Contains(' ') || text.Contains('\t') || text.Contains('\r') || text.Contains('\n')) return false;
            try { Convert.FromBase64String(text); return true; } catch { return false; }
        }

        #endregion
    }
}
