#nullable disable

namespace TPEmail.Common.Helpers
{
    public static class TPAssistSearchHelper
    {
        public static List<ScoredMatch> ScoreKnowledgeEntries(string[] words, string query, KnowledgeBase kb, string userRole)
        {
            var matches = new List<ScoredMatch>();
            if (kb?.Entries == null || words.Length == 0) return matches;

            foreach (var entry in kb.Entries)
            {
                if (entry.AdminOnly && userRole != "ADMIN") continue;

                int score = 0;
                int matchedKeywords = 0;

                foreach (var word in words)
                {
                    if (entry.Keywords?.Any(k => k.Equals(word, StringComparison.OrdinalIgnoreCase)) == true)
                    {
                        score += 15;
                        matchedKeywords++;
                    }
                    else if (entry.Keywords?.Any(k => k.Contains(word, StringComparison.OrdinalIgnoreCase)) == true)
                    {
                        score += 8;
                        matchedKeywords++;
                    }
                    else if (entry.Question?.ToLowerInvariant().Contains(word) == true)
                    {
                        score += 4;
                    }
                }

                if (entry.Keywords?.Any(k => query.Contains(k.ToLowerInvariant())) == true)
                    score += 20;

                score += entry.Priority / 20;

                if (score > 0)
                {
                    int maxPossible = words.Length * 15 + 25;
                    int confidence = Math.Min(100, (score * 100) / maxPossible);

                    if (matchedKeywords >= 2) confidence = Math.Min(100, confidence + 15);
                    if (matchedKeywords >= 3) confidence = Math.Min(100, confidence + 10);

                    if (confidence >= 25)
                    {
                        matches.Add(new ScoredMatch { Type = "help", KBEntry = entry, Confidence = confidence });
                    }
                }
            }

            return matches;
        }

        public static List<ScoredMatch> ScoreRoutes(string[] words, string query, string userRole, List<RouteInfo> routes)
        {
            var matches = new List<ScoredMatch>();

            foreach (var route in routes)
            {
                if (!route.Roles.Contains(userRole.ToUpper())) continue;

                int score = 0;
                int matchedKeywords = 0;

                foreach (var word in words)
                {
                    if (route.Keywords?.Any(k => k.Equals(word, StringComparison.OrdinalIgnoreCase)) == true)
                    {
                        score += 12;
                        matchedKeywords++;
                    }
                    else if (route.Keywords?.Any(k => k.Contains(word, StringComparison.OrdinalIgnoreCase)) == true)
                    {
                        score += 6;
                        matchedKeywords++;
                    }
                    else if (route.Label?.ToLowerInvariant().Contains(word) == true)
                    {
                        score += 4;
                    }
                }

                if (route.Keywords?.Any(k => query.Contains(k.ToLowerInvariant())) == true)
                    score += 18;

                if (score > 0)
                {
                    int maxPossible = words.Length * 12 + 20;
                    int confidence = Math.Min(100, (score * 100) / maxPossible);

                    if (matchedKeywords >= 2) confidence = Math.Min(100, confidence + 12);

                    if (confidence >= 20)
                    {
                        matches.Add(new ScoredMatch { Type = "navigate", Route = route, Confidence = confidence });
                    }
                }
            }

            return matches;
        }

        public static KBEntry GetAnswerById(int id, KnowledgeBase kb)
        {
            return kb?.Entries?.FirstOrDefault(e => e.Id == id);
        }

        public static string GetCategoryName(string categoryId, KnowledgeBase kb)
        {
            return kb?.Categories?.FirstOrDefault(c => c.Id == categoryId)?.Name ?? categoryId;
        }
    }

    public class ScoredMatch
    {
        public string Type { get; set; }
        public KBEntry KBEntry { get; set; }
        public RouteInfo Route { get; set; }
        public int Confidence { get; set; }
    }

    public class RouteInfo
    {
        public string Route { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public string[] Roles { get; set; }
        public string[] Keywords { get; set; }
        public string Icon { get; set; }
    }
}
