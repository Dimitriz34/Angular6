#nullable disable

using System.Text.Json;

namespace TPEmail.Common.Helpers
{
    public static class TPAssistKnowledgeBaseHelper
    {
        private static KnowledgeBase _knowledgeBase;
        private static DateTime _lastLoaded = DateTime.MinValue;
        private static readonly object _loadLock = new();
        private static readonly string _knowledgeFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "tpassist-knowledge.json");
        private static Dictionary<string, List<int>> _keywordIndex = new();

        public static KnowledgeBase GetKnowledgeBase()
        {
            lock (_loadLock)
            {
                var fileInfo = new FileInfo(_knowledgeFilePath);

                if (_knowledgeBase == null || (fileInfo.Exists && fileInfo.LastWriteTime > _lastLoaded))
                {
                    try
                    {
                        if (fileInfo.Exists)
                        {
                            var json = System.IO.File.ReadAllText(_knowledgeFilePath);
                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                                ReadCommentHandling = JsonCommentHandling.Skip
                            };
                            _knowledgeBase = JsonSerializer.Deserialize<KnowledgeBase>(json, options);
                            _lastLoaded = DateTime.Now;
                            BuildKeywordIndex();
                        }
                        else
                        {
                            _knowledgeBase = new KnowledgeBase { Entries = new List<KBEntry>() };
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error in GetKnowledgeBase: {ex.Message}", ex);
                    }
                }

                return _knowledgeBase;
            }
        }

        public static void BuildKeywordIndex()
        {
            _keywordIndex = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

            if (_knowledgeBase?.Entries == null) return;

            for (int i = 0; i < _knowledgeBase.Entries.Count; i++)
            {
                var entry = _knowledgeBase.Entries[i];
                if (entry.Keywords == null) continue;

                foreach (var keyword in entry.Keywords)
                {
                    var key = keyword.ToLowerInvariant();
                    if (!_keywordIndex.ContainsKey(key))
                        _keywordIndex[key] = new List<int>();

                    if (!_keywordIndex[key].Contains(i))
                        _keywordIndex[key].Add(i);
                }
            }
        }

        public static void ReloadKnowledgeBase()
        {
            lock (_loadLock)
            {
                _lastLoaded = DateTime.MinValue; // Force reload
                GetKnowledgeBase();
            }
        }

        public static bool IsOutOfScope(string query)
        {
            var outOfScope = new[] { "weather", "news", "sports", "stock", "bitcoin", "crypto", "movie", "music", "game", "recipe", "joke", "translate" };
            return outOfScope.Any(p => query.Contains(p));
        }
    }

    public class KnowledgeBase
    {
        public KBMetadata Metadata { get; set; }
        public List<KBCategory> Categories { get; set; }
        public List<KBEntry> Entries { get; set; }
    }

    public class KBMetadata
    {
        public string Version { get; set; }
        public string LastUpdated { get; set; }
        public string Description { get; set; }
    }

    public class KBCategory
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
    }

    public class KBEntry
    {
        public int Id { get; set; }
        public string Category { get; set; }
        public string[] Keywords { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public string[] RelatedRoutes { get; set; }
        public bool AdminOnly { get; set; }
        public int Priority { get; set; }
    }
}
