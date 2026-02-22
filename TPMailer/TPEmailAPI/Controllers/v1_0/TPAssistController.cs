#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using TPEmail.BusinessModels.ResponseModels;
using TPEmail.BusinessModels.Constants;
using TPEmail.Common.Helpers;
using static TPEmail.Common.Helpers.TPAssistSuggestionHelper;

namespace TPEmailAPI.Controllers.v_1_0
{
    public class TPAssistSearchRequest
    {
        public string Query { get; set; }
        public int? AnswerId { get; set; }
    }

    public class TPAssistSearchResponse
    {
        public bool Success { get; set; }
        public string Route { get; set; }
        public string Label { get; set; }
        public string Message { get; set; }
        public bool IsHelpResponse { get; set; }
        public int Confidence { get; set; }
        public string Category { get; set; }
        public List<TPAssistSuggestion> Suggestions { get; set; } = new();
    }

    [Authorize]
    [ApiVersion("1.0")]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TPAssistController : ControllerBase
    {
        private static readonly List<RouteInfo> _routes = new()
        {
            new RouteInfo { Route = "/Dashboard", Label = "Dashboard", Description = "Home - Overview & Statistics", Roles = new[] { "ADMIN", "USER", "MODERATOR", "TESTER" }, Keywords = new[] { "dashboard", "home", "overview", "stats", "main", "start" }, Icon = "home" },
            new RouteInfo { Route = "/Application/List", Label = "Application List", Description = "View all registered applications", Roles = new[] { "ADMIN", "USER" }, Keywords = new[] { "applications", "apps", "list", "my apps", "registered" }, Icon = "cube" },
            new RouteInfo { Route = "/Application/Add?source=tpassist", Label = "Register Application", Description = "Register a new application with pre-filled info", Roles = new[] { "ADMIN", "USER" }, Keywords = new[] { "add app", "new app", "create app", "register application", "register" }, Icon = "plus" },
            new RouteInfo { Route = "/Application/List?status=pending", Label = "Pending Applications", Description = "Applications awaiting approval", Roles = new[] { "ADMIN" }, Keywords = new[] { "pending apps", "approve", "waiting" }, Icon = "clock" },
            new RouteInfo { Route = "/User/UserDetails", Label = "User List", Description = "View and manage all users", Roles = new[] { "ADMIN" }, Keywords = new[] { "users", "all users", "user list", "accounts", "members" }, Icon = "users" },
            new RouteInfo { Route = "/Admin/AppUser/Registration", Label = "Register User", Description = "Create a new user account", Roles = new[] { "ADMIN" }, Keywords = new[] { "add user", "new user", "create user", "register user" }, Icon = "user-plus" },
            new RouteInfo { Route = "/Email/List", Label = "Email Logs", Description = "View email history and delivery status", Roles = new[] { "ADMIN", "USER" }, Keywords = new[] { "emails", "email logs", "logs", "history", "sent emails", "email list" }, Icon = "envelope" },
            new RouteInfo { Route = "/Email/List?action=send", Label = "Send Test Email", Description = "Send a test email to verify configuration", Roles = new[] { "ADMIN", "USER" }, Keywords = new[] { "send test", "test email", "send email", "test mail", "send mail", "compose", "try email" }, Icon = "paper-plane" },
            new RouteInfo { Route = "/Email/List?date=today", Label = "Today's Emails", Description = "View emails sent today", Roles = new[] { "ADMIN", "USER" }, Keywords = new[] { "today", "today emails", "current day" }, Icon = "calendar-day" },
            new RouteInfo { Route = "/Email/List?date=yesterday", Label = "Yesterday's Emails", Description = "View emails sent yesterday", Roles = new[] { "ADMIN", "USER" }, Keywords = new[] { "yesterday", "previous day" }, Icon = "calendar-minus" },
            new RouteInfo { Route = "/Email/List?date=last7days", Label = "Last 7 Days", Description = "View emails from the past week", Roles = new[] { "ADMIN", "USER" }, Keywords = new[] { "last week", "7 days", "week", "weekly" }, Icon = "calendar-week" },
            new RouteInfo { Route = "/Email/List?date=last30days", Label = "Last 30 Days", Description = "View emails from the past month", Roles = new[] { "ADMIN", "USER" }, Keywords = new[] { "last month", "30 days", "month", "monthly" }, Icon = "calendar-alt" },
            new RouteInfo { Route = "/Email/List?status=failed", Label = "Failed Emails", Description = "View emails that failed to send", Roles = new[] { "ADMIN", "USER" }, Keywords = new[] { "failed emails", "errors", "bounced" }, Icon = "times-circle" },
            new RouteInfo { Route = "/Email/List?status=delivered", Label = "Delivered Emails", Description = "Successfully delivered emails", Roles = new[] { "ADMIN", "USER" }, Keywords = new[] { "delivered", "sent", "successful" }, Icon = "check-circle" },
            new RouteInfo { Route = "/Admin/Lookup/EmailServices/List", Label = "Email Services", Description = "Manage email service providers", Roles = new[] { "ADMIN" }, Keywords = new[] { "email services", "smtp", "providers", "sendgrid" }, Icon = "server" },
            new RouteInfo { Route = "/Admin/Lookup/EmailServices/Add", Label = "Add Email Service", Description = "Configure new email provider", Roles = new[] { "ADMIN" }, Keywords = new[] { "add service", "new provider", "configure smtp" }, Icon = "plus" },
            new RouteInfo { Route = "/Account/PasswordUpdate", Label = "Change Password", Description = "Update your account password", Roles = new[] { "ADMIN", "USER", "MODERATOR", "TESTER" }, Keywords = new[] { "password", "change password", "profile", "security" }, Icon = "key" }
        };

        [HttpPost]
        public IActionResult Search(TPAssistSearchRequest request)
        {
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "USER";
            var kb = TPAssistKnowledgeBaseHelper.GetKnowledgeBase();

            if (request.AnswerId.HasValue)
                return GetAnswerById(request.AnswerId.Value, userRole, kb);

            if (string.IsNullOrWhiteSpace(request?.Query))
            {
                return Ok(ApiResult(new TPAssistSearchResponse
                {
                    Success = true,
                    Suggestions = TPAssistSuggestionHelper.GetDefaultSuggestions(userRole, _routes)
                }));
            }

            var query = request.Query.Trim().ToLowerInvariant();
            var words = query.Split(new[] { ' ', '?', '!', '.', ',' }, StringSplitOptions.RemoveEmptyEntries)
                             .Where(w => w.Length >= 2)
                             .ToArray();

            if (TPAssistKnowledgeBaseHelper.IsOutOfScope(query))
            {
                return Ok(ApiResult(new TPAssistSearchResponse
                {
                    Success = false,
                    Message = MessageConstants.TPAssistDefaultMessage,
                    Suggestions = TPAssistSuggestionHelper.GetDefaultSuggestions(userRole, _routes)
                }));
            }

            var allMatches = new List<ScoredMatch>();
            allMatches.AddRange(TPAssistSearchHelper.ScoreKnowledgeEntries(words, query, kb, userRole));
            allMatches.AddRange(TPAssistSearchHelper.ScoreRoutes(words, query, userRole, _routes));
            var sorted = allMatches.OrderByDescending(m => m.Confidence).ToList();

            if (sorted.Count > 0 && sorted[0].Confidence >= 85)
            {
                var best = sorted[0];
                if (best.Type == "help" && best.KBEntry != null)
                {
                    return Ok(ApiResult(new TPAssistSearchResponse
                    {
                        Success = true,
                        IsHelpResponse = true,
                        Label = best.KBEntry.Question,
                        Message = best.KBEntry.Answer,
                        Confidence = best.Confidence,
                        Category = TPAssistSearchHelper.GetCategoryName(best.KBEntry.Category, kb),
                        Suggestions = TPAssistSuggestionHelper.GetRelatedRoutes(best.KBEntry, userRole, _routes)
                    }));
                }
                else if (best.Route != null)
                {
                    return Ok(ApiResult(new TPAssistSearchResponse
                    {
                        Success = true,
                        Route = best.Route.Route,
                        Label = best.Route.Label,
                        Message = best.Route.Description,
                        Confidence = best.Confidence,
                        Suggestions = TPAssistSuggestionHelper.GetContextualSuggestions(best.Route.Route, userRole, _routes)
                    }));
                }
            }

            if (sorted.Count == 0)
            {
                return Ok(ApiResult(new TPAssistSearchResponse
                {
                    Success = false,
                    Message = MessageConstants.NoMatchesFound,
                    Suggestions = TPAssistSuggestionHelper.GetDefaultSuggestions(userRole, _routes)
                }));
            }

            var suggestions = sorted.Take(8).Select(m => new TPAssistSuggestion
            {
                Label = m.Type == "help" ? m.KBEntry?.Question : m.Route?.Label,
                Description = m.Type == "help" ? $"Click for answer" : m.Route?.Description,
                Route = m.Route?.Route,
                Type = m.Type,
                AnswerId = m.Type == "help" ? m.KBEntry?.Id : null,
                Confidence = m.Confidence,
                Icon = m.Type == "help" ? "question-circle" : m.Route?.Icon
            }).ToList();

            return Ok(ApiResult(new TPAssistSearchResponse
            {
                Success = true,
                Suggestions = suggestions
            }));
        }

        [HttpGet]
        public IActionResult GetSuggestions()
        {
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "USER";
            return Ok(new ApiResponse<List<TPAssistSuggestion>>(ResultCodes.Success, new[] { TPAssistSuggestionHelper.GetDefaultSuggestions(userRole, _routes) }));
        }

        [HttpPost]
        public IActionResult ReloadKnowledgeBase()
        {
            TPAssistKnowledgeBaseHelper.ReloadKnowledgeBase();
            var kb = TPAssistKnowledgeBaseHelper.GetKnowledgeBase();
            return Ok(ApiResult(new TPAssistSearchResponse
            {
                Success = true,
                Message = string.Format(MessageConstants.KnowledgeBaseReloadedFormat, kb?.Entries?.Count ?? 0)
            }));
        }

        private IActionResult GetAnswerById(int id, string userRole, KnowledgeBase kb)
        {
            var entry = TPAssistSearchHelper.GetAnswerById(id, kb);
            if (entry == null)
                return Ok(ApiResult(new TPAssistSearchResponse { Success = false, Message = MessageConstants.AnswerNotFound }));
            if (entry.AdminOnly && userRole != "ADMIN")
                return Ok(ApiResult(new TPAssistSearchResponse { Success = false, Message = MessageConstants.AdministratorAccessRequired }));

            return Ok(ApiResult(new TPAssistSearchResponse
            {
                Success = true,
                IsHelpResponse = true,
                Label = entry.Question,
                Message = entry.Answer,
                Confidence = 100,
                Category = TPAssistSearchHelper.GetCategoryName(entry.Category, kb),
                Suggestions = TPAssistSuggestionHelper.GetRelatedRoutes(entry, userRole, _routes)
            }));
        }

        private ApiResponse<TPAssistSearchResponse> ApiResult(TPAssistSearchResponse response)
        {
            return new ApiResponse<TPAssistSearchResponse>(ResultCodes.Success, new[] { response });
        }
    }
}
