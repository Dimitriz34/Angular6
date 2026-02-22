using System.Data;
using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Dapper;
using NLog;
using TPEmail.BusinessModels.Constants;
using TPEmail.BusinessModels.ResponseModels;

namespace TPEmail.Common.Helpers
{
    public class KeyVaultHelper
    {
        private static SecretClient? _client;

        public static void Initialize(string vaultUrl, string clientId, string clientSecret, string tenantId)
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            _client = new SecretClient(new Uri(vaultUrl), credential);
        }

        public static bool IsConfigured => _client != null;

        public static async Task<string?> GetSecretAsync(string secretName)
        {
            if (_client == null) return null;
            try
            {
                var secret = await _client.GetSecretAsync(secretName);
                return secret?.Value?.Value;
            }
            catch { return null; }
        }

        public static async Task SetSecretAsync(string secretName, string secretValue)
        {
            if (_client == null) return;
            try
            {
                await _client.SetSecretAsync(secretName, secretValue);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in SetSecretAsync for '{secretName}': {ex.Message}", ex);
            }
        }
    }

    public class KeyConfigProvider
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly Func<IDbConnection> _dbFactory;
        private readonly string _vaultSecretName;
        private List<KeyConfig>? _allKeys;
        private KeyConfig? _activeKey;
        private readonly object _lock = new();

        public string ResolvedSource { get; private set; } = "None";

        public KeyConfigProvider(Func<IDbConnection> dbFactory, string vaultSecretName = "tpmailer-encryption-keys")
        {
            _dbFactory = dbFactory;
            _vaultSecretName = vaultSecretName;
        }

        public KeyConfig GetActiveKey() { EnsureLoaded(); return _activeKey!; }
        public List<KeyConfig> GetAllKeys() { EnsureLoaded(); return _allKeys!; }

        public void Invalidate()
        {
            lock (_lock) { _allKeys = null; _activeKey = null; ResolvedSource = "None"; }
        }

        private void EnsureLoaded()
        {
            if (_activeKey != null) return;
            lock (_lock) { if (_activeKey != null) return; LoadKeys(); }
        }

        private void LoadKeys()
        {
            if (KeyVaultHelper.IsConfigured && TryLoadFromKeyVault()) return;
            LoadFromDatabase();
        }

        private bool TryLoadFromKeyVault()
        {
            try
            {
                var json = KeyVaultHelper.GetSecretAsync(_vaultSecretName).GetAwaiter().GetResult();
                if (string.IsNullOrWhiteSpace(json)) return false;

                json = json.Trim();
                var keys = json.StartsWith('[')
                    ? JsonSerializer.Deserialize<List<KeyConfig>>(json, _jsonOpts)
                    : WrapSingle(JsonSerializer.Deserialize<KeyConfig>(json, _jsonOpts));

                if (keys == null || keys.Count == 0) return false;

                _allKeys = keys;
                _activeKey = keys.FirstOrDefault(k => k.Active) ?? keys.First();
                ResolvedSource = "AzureKeyVault";
                _logger.Info("Encryption keys loaded from Azure Key Vault ({Count} keys)", keys.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Key Vault fetch failed, falling back to database");
                return false;
            }
        }

        private static List<KeyConfig>? WrapSingle(KeyConfig? k) => k != null ? new List<KeyConfig> { k } : null;
        private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

        private void LoadFromDatabase()
        {
            using var conn = _dbFactory();
            try { _allKeys = conn.Query<KeyConfig>("sel_keyconfigall", commandType: CommandType.StoredProcedure).ToList(); }
            catch { _allKeys = null; }

            if (_allKeys == null || _allKeys.Count == 0)
            {
                var single = conn.QuerySingleOrDefault<KeyConfig>("sel_keyconfig", commandType: CommandType.StoredProcedure);
                _allKeys = single != null ? new List<KeyConfig> { single }
                    : throw new InvalidOperationException(MessageConstants.EncryptionKeyNotLoaded);
            }

            _activeKey = _allKeys.FirstOrDefault(k => k.Active) ?? _allKeys.First();
            ResolvedSource = "Database";
            _logger.Info("Encryption keys loaded from Database ({Count} keys)", _allKeys.Count);
        }
    }
}
