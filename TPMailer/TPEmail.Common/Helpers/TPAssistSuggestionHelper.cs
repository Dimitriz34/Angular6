#nullable disable

namespace TPEmail.Common.Helpers
{
    public static class TPAssistSuggestionHelper
    {
        public static List<TPAssistSuggestion> GetRelatedRoutes(KBEntry entry, string userRole, List<RouteInfo> allRoutes)
        {
            var suggestions = new List<TPAssistSuggestion>();

            if (entry.RelatedRoutes != null)
            {
                foreach (var routePath in entry.RelatedRoutes)
                {
                    var route = allRoutes.FirstOrDefault(r => r.Route == routePath && r.Roles.Contains(userRole.ToUpper()));
                    if (route != null)
                    {
                        suggestions.Add(new TPAssistSuggestion
                        {
                            Label = route.Label,
                            Route = route.Route,
                            Description = route.Description,
                            Type = "navigate",
                            Icon = route.Icon
                        });
                    }
                }
            }

            return suggestions.Take(3).ToList();
        }

        public static List<TPAssistSuggestion> GetContextualSuggestions(string currentRoute, string userRole, List<RouteInfo> allRoutes)
        {
            var suggestions = new List<TPAssistSuggestion>();

            if (currentRoute.Contains("Application"))
            {
                AddRouteIfAllowed(suggestions, "/Email/List", userRole, allRoutes);
                AddRouteIfAllowed(suggestions, "/Dashboard", userRole, allRoutes);
            }
            else if (currentRoute.Contains("Email"))
            {
                AddRouteIfAllowed(suggestions, "/Application/List", userRole, allRoutes);
                AddRouteIfAllowed(suggestions, "/Dashboard", userRole, allRoutes);
            }
            else
            {
                AddRouteIfAllowed(suggestions, "/Application/List", userRole, allRoutes);
                AddRouteIfAllowed(suggestions, "/Email/List", userRole, allRoutes);
            }

            return suggestions.Take(3).ToList();
        }

        public static List<TPAssistSuggestion> GetDefaultSuggestions(string userRole, List<RouteInfo> allRoutes)
        {
            return allRoutes
                .Where(r => r.Roles.Contains(userRole.ToUpper()))
                .Take(5)
                .Select(r => new TPAssistSuggestion
                {
                    Label = r.Label,
                    Route = r.Route,
                    Description = r.Description,
                    Type = "navigate",
                    Icon = r.Icon
                })
                .ToList();
        }

        private static void AddRouteIfAllowed(List<TPAssistSuggestion> suggestions, string routePath, string userRole, List<RouteInfo> allRoutes)
        {
            var route = allRoutes.FirstOrDefault(r => r.Route == routePath && r.Roles.Contains(userRole.ToUpper()));
            if (route != null)
            {
                suggestions.Add(new TPAssistSuggestion
                {
                    Label = route.Label,
                    Route = route.Route,
                    Description = route.Description,
                    Type = "navigate",
                    Icon = route.Icon
                });
            }
        }
    }

    public class TPAssistSuggestion
    {
        public string Label { get; set; }
        public string Route { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public int? AnswerId { get; set; }
        public int Confidence { get; set; }
        public string Icon { get; set; }
    }
}
