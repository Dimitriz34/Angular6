namespace TPEmail.BusinessModels.ResponseModels
{

    public class KeyConfig
    {

        public int Id { get; set; }
        public int KeyVersion { get; set; }
        public string EncryptionKey { get; set; } = string.Empty;
        public byte[] SaltBytes { get; set; } = Array.Empty<byte>();
        public bool Active { get; set; } = true;
    }
}
