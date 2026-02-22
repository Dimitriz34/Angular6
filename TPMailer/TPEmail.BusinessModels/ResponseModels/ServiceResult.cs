namespace TPEmail.BusinessModels.ResponseModels
{
    public class ServiceResult
    {
        public int Value { get; set; }
        public Guid? EntityId { get; set; }
        public string? Data { get; set; }
        public bool Success { get; set; }

        public static ServiceResult FromCount(int count) => new() { Value = count, Success = true };
        public static ServiceResult FromRowsAffected(int rows) => new() { Value = rows, Success = rows > 0 };
        public static ServiceResult FromEntityId(Guid id) => new() { EntityId = id, Value = id != Guid.Empty ? 1 : 0, Success = id != Guid.Empty };
        public static ServiceResult FromBool(bool ok) => new() { Success = ok, Value = ok ? 1 : 0 };
        public static ServiceResult FromAppCode(string val) => new() { Data = val, Value = int.TryParse(val, out var v) ? v : 0, Success = !string.IsNullOrEmpty(val) };
    }
}
