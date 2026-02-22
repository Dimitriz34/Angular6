using Dapper;
using System.Data;
using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;
using TPEmail.Common.Helpers;
using TPEmail.BusinessModels.Enums;
using TPEmail.DataAccess.Interface.Repository.v1_0;
using TPEmail.BusinessModels.Constants;

namespace TPEmail.DataAccess.Repository.v1_0
{
    public class GuidToStringHandler : SqlMapper.TypeHandler<string>
    {
        public override string Parse(object value)
        {
            if (value is Guid guidValue)
            {
                return guidValue.ToString();
            }
            return value?.ToString() ?? string.Empty;
        }

        public override void SetValue(IDbDataParameter parameter, string? value)
        {
            if (Guid.TryParse(value, out Guid guidValue))
            {
                parameter.Value = guidValue;
            }
            else
            {
                parameter.Value = DBNull.Value;
            }
        }
    }

    public class EmailRepository : IEmailRepository
    {
        private readonly Func<IDbConnection> _tpmailerdb;
        private readonly NLog.ILogger _logger;

        static EmailRepository()
        {
            SqlMapper.AddTypeHandler(new GuidToStringHandler());
        }

        public EmailRepository(Func<IDbConnection> dbFactory)
        {
            _tpmailerdb = dbFactory;
            _logger = NLog.LogManager.GetCurrentClassLogger();
        }

        public async Task<ServiceResult> SaveUpdateEntityAsync(Email data)
        {
            Guid.TryParse(data.UserId, out Guid userIdGuid);
            var result = await DatabaseHelper.QuerySingleAsync<Guid>(_tpmailerdb, "commit_email", new
            {
                emailid = data.EmailId == Guid.Empty ? (Guid?)null : data.EmailId,
                userid = userIdGuid,
                appcode = data.AppId,
                senderfrom = data.FromEmailAddress,
                replyto = data.FromEmailAddress,
                recipients = data.To?.Count > 0 ? string.Join(",", data.To.Select(r => r.Recipient)) : string.Empty,
                ccrecipients = data.Cc?.Count > 0 ? string.Join(",", data.Cc.Select(r => r.Recipient)) : (string?)null,
                bccrecipients = (string?)null,
                subject = data.Subject,
                body = data.Body,
                ishtmlbody = data.IsHtml,
                priority = "Normal",
                status = data.EmailId == Guid.Empty ? "Pending" : "Sent",
                scheduleddatetime = (DateTime?)null,
                trackingid = (string?)null,
                keyversion = data.KeyVersion > 0 ? data.KeyVersion : (int?)null,
                modifiedby = data.CreatedBy
            });
            return ServiceResult.FromEntityId(result);
        }

        public async Task<IList<EmailGetDto>> FindAllEmailsAsync() =>
            await DatabaseHelper.QueryListAsync<EmailGetDto>(_tpmailerdb, "sel_email");

        public async Task<IList<EmailGetDto>> FindAllEmailsAsync(int pageIndex, int pageSize, string? searchTerm = null, string? appName = null, DateTime? startDate = null, DateTime? endDate = null, string? userId = null, int? appCode = null)
        {
            var result = await DatabaseHelper.QueryListAsync<EmailGetDto>(_tpmailerdb, "sel_email", new
            {
                pageindex = pageIndex,
                pagesize = pageSize,
                searchterm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm,
                appname = string.IsNullOrWhiteSpace(appName) ? null : appName,
                datefrom = startDate,
                dateto = endDate.HasValue ? endDate.Value.Date.AddDays(1).AddSeconds(-1) : (DateTime?)null,
                userid = string.IsNullOrWhiteSpace(userId) ? (Guid?)null : Guid.Parse(userId),
                appcode = appCode
            });
            return result;
        }

        public async Task<ServiceResult> GetEmailListCountAsync(string? searchTerm = null, string? appName = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var result = await DatabaseHelper.QuerySingleAsync<int>(_tpmailerdb, "sel_email", new
            {
                countonly = true,
                searchterm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm,
                appname = string.IsNullOrWhiteSpace(appName) ? null : appName,
                datefrom = startDate,
                dateto = endDate.HasValue ? endDate.Value.Date.AddDays(1).AddSeconds(-1) : (DateTime?)null
            });
            return ServiceResult.FromCount(result);
        }

        public async Task<EmailDetailDto?> GetEmailDetailAsync(Guid emailId) =>
            await DatabaseHelper.QuerySingleAsync<EmailDetailDto>(_tpmailerdb, "sel_emaildetail", new { emailid = emailId });

        public async Task<ServiceResult> SaveEmailRecipientsAsync(EmailRecipients data)
        {
            using var conn = _tpmailerdb();
            return ServiceResult.FromRowsAffected(await conn.ExecuteAsync("commit_emailrecipient", new
            {
                emailid = data.EmailId,
                recipienttype = data.Type,
                recipientemail = data.Recipient,
                recipientname = data.ToDisplayName,
                createdby = "SYSTEM"
            }, commandType: CommandType.StoredProcedure));
        }

        public async Task<IList<EmailRecipientsGetDto>> FindAllEmailRecipientsAsync() =>
            await DatabaseHelper.QueryListAsync<EmailRecipientsGetDto>(_tpmailerdb, "sel_emailrecipient");

        public async Task<IList<EmailRecipientsGetDto>> FindEmailRecipientsByEmailIdAsync(Guid emailId) =>
            await DatabaseHelper.QueryListAsync<EmailRecipientsGetDto>(_tpmailerdb, "sel_emailrecipient", new { emailid = emailId });

        public async Task<ServiceResult> SaveEmailAttachmentAsync(EmailAttachment data)
        {
            using var conn = _tpmailerdb();
            return ServiceResult.FromRowsAffected(await conn.ExecuteAsync("commit_emailattachment", new
            {
                emailid = data.EmailId,
                filename = data.AttachmentName,
                filepath = data.AttachmentPath,
                contenttype = data.AttachmentType,
                filesize = 0,
                modifiedby = "system"
            }, commandType: CommandType.StoredProcedure));
        }

        public async Task<IList<EmailAttachment>> FindAllEmailAttachmentsAsync() =>
            await DatabaseHelper.QueryListAsync<EmailAttachment>(_tpmailerdb, "sel_emailattachment");

        public async Task<IList<EmailAttachment>> FindEmailAttachmentsByEmailIdAsync(Guid emailId) =>
            await DatabaseHelper.QueryListAsync<EmailAttachment>(_tpmailerdb, "sel_emailattachment", new { emailid = emailId });

        public async Task<ServiceResult> GetNumberOfAppUserEmailCountAsync(string? userId = null)
        {
            var result = await DatabaseHelper.QuerySingleAsync<int>(_tpmailerdb, "sel_emailcount", string.IsNullOrEmpty(userId) ? null : new { senderid = userId });
            return ServiceResult.FromCount(result);
        }

        public async Task<ServiceResult> GetNumberOfMonthlyAppUserEmailCountAsync(string? userId = null)
        {
            var result = await DatabaseHelper.QuerySingleAsync<int>(_tpmailerdb, "sel_emailcount", string.IsNullOrEmpty(userId) ? new { period = "monthly" } : new { period = "monthly", senderid = userId });
            return ServiceResult.FromCount(result);
        }

        public async Task<ServiceResult> GetNumberOfTodayAppUserEmailCountAsync(string? userId = null)
        {
            var result = await DatabaseHelper.QuerySingleAsync<int>(_tpmailerdb, "sel_emailcount", string.IsNullOrEmpty(userId) ? new { period = "today" } : new { period = "today", senderid = userId });
            return ServiceResult.FromCount(result);
        }

        public async Task<ServiceResult> UpdateEmailStatusAsync(Guid emailId, string status, string? errorCode = null, string? errorMessage = null, string? modifiedBy = null)
        {
            try
            {
                using var conn = _tpmailerdb();
                var result = await conn.QueryFirstOrDefaultAsync<int>("commit_emailstatus", new
                {
                    emailid = emailId,
                    status,
                    errorcode = errorCode,
                    errormessage = errorMessage,
                    graphmessageid = (string?)null,
                    modifiedby = modifiedBy ?? "SYSTEM"
                }, commandType: CommandType.StoredProcedure);
                return ServiceResult.FromBool(result == 1);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in UpdateEmailStatusAsync: {ex.Message}", ex);
            }
        }
    }
}

