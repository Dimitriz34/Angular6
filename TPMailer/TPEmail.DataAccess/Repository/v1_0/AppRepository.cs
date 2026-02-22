using Dapper;
using System.Data;
using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;
using TPEmail.Common.Helpers;
using TPEmail.DataAccess.Interface.Repository.v1_0;

namespace TPEmail.DataAccess.Repository.v1_0
{
    public class AppRepository : IAppRepository
    {
        private readonly Func<IDbConnection> _tpmailerdb;
        private readonly NLog.ILogger mm_logger;

        public AppRepository(Func<IDbConnection> dbFactory)
        {
            _tpmailerdb = dbFactory;
            mm_logger = NLog.LogManager.GetCurrentClassLogger();
        }

        public async Task<ServiceResult> GetCountAsync(string tableName, string condition)
        {
            bool wantActive = !string.IsNullOrWhiteSpace(condition) &&
                              condition.Replace(" ", "").Equals("Active=1", StringComparison.OrdinalIgnoreCase);

            return ServiceResult.FromCount(tableName.Trim() switch
            {
                "AppUser" => wantActive
                    ? (await GetUserCountAsync()).ActiveCount
                    : (await GetUserCountAsync()).TotalCount,

                "Application" => wantActive
                    ? (await GetApplicationCountAsync()).ActiveCount
                    : (await GetApplicationCountAsync()).TotalCount,

                _ => throw new ArgumentException($"Entity '{tableName}' is not supported for counting.", nameof(tableName))
            });
        }

        public async Task<IList<DashboardEmailDto>> FindAdminDashboardDataAsync(string? userId = null)
        {
            object? param = null;
            if (!string.IsNullOrWhiteSpace(userId) && Guid.TryParse(userId, out Guid userGuid))
                param = new { UserId = userGuid };
            return await DatabaseHelper.QueryListAsync<DashboardEmailDto>(_tpmailerdb, "sel_emailcount", param);
        }

        public async Task<IList<Top10AppsDto>> FindTop10AppsAsync() =>
            await DatabaseHelper.QueryListAsync<Top10AppsDto>(_tpmailerdb, "sel_toptenapps");

        public async Task<IList<Top5AppsUtilisationDto>> FindTop5AppsUtilisationAsync() =>
            await DatabaseHelper.QueryListAsync<Top5AppsUtilisationDto>(_tpmailerdb, "sel_topfiveappsutilisation");

        public async Task GenerateKeyConfigurationAsync(string key, byte[] salt) =>
            await Task.CompletedTask;

        public async Task<KeyConfig> GetKeyConfigAsync()
        {
            var result = await DatabaseHelper.QuerySingleAsync<KeyConfig>(_tpmailerdb, "sel_keyconfig");
            return result ?? new KeyConfig();
        }

        public async Task<ServiceResult> SaveUpdateEntityAsync(AppLookup data)
        {
            var param = new DynamicParameters(new
            {
                appcode = data.Id == 0 ? (int?)null : data.Id,
                appname = data.AppName.Trim(),
                appdesc = data.Description,
                appclient = data.AppClient == Guid.Empty ? (Guid?)null : data.AppClient,
                appsecret = data.AppSecret,
                isencrypted = data.IsEncrypted,
                tenantid = (string?)null,
                userid = data.UserId,
                appowner = data.AppOwner,
                coowner = data.CoOwner,
                owneremail = data.OwnerEmail,
                coowneremail = data.CoOwnerEmail,
                fromemailaddress = data.FromEmailAddress,
                fromdisplayname = data.FromEmailDisplayName,
                emailserver = data.EmailServer,
                port = data.EncryptedPort,
                emailserviceid = data.EmailServiceId,
                isinternalapp = data.IsInternalApp,
                usetpassist = data.UseTPAssist,
                encryptedfields = data.EncryptedFields,
                keyversion = data.KeyVersion ?? (int?)null,
                active = data.Active,
                modifiedby = data.Id == 0 ? data.CreatedBy : data.ModifiedBy
            });
            param.Add("@appcode", data.Id == 0 ? null : data.Id, DbType.Int32, ParameterDirection.InputOutput);

            using var conn = _tpmailerdb();
            await conn.ExecuteAsync("commit_application", param, commandType: CommandType.StoredProcedure);
            return ServiceResult.FromAppCode(param.Get<int>("@appcode").ToString());
        }

        public async Task<IList<AppLookup>> FindApplicationLookupAsync() =>
            await DatabaseHelper.QueryListAsync<AppLookup>(_tpmailerdb, "sel_application");

        public async Task<IList<AppLookup>> FindApplicationLookupAsync(int pageIndex, int pageSize, string? searchTerm = null, string? userId = null, bool? active = null)
        {
            var result = await DatabaseHelper.QueryListAsync<AppLookup>(_tpmailerdb, "sel_application", new
            {
                pageindex = pageIndex,
                pagesize = pageSize,
                searchterm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm,
                userid = string.IsNullOrWhiteSpace(userId) ? (Guid?)null : Guid.Parse(userId),
                active
            });
            return result;
        }

        public async Task<ApplicationCountDto> GetApplicationCountAsync(string? searchTerm = null, string? userId = null, bool? active = null)
        {
            var result = await DatabaseHelper.QuerySingleAsync<ApplicationCountDto>(_tpmailerdb, "sel_applicationcount", new
            {
                searchterm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm,
                userid = string.IsNullOrWhiteSpace(userId) ? (Guid?)null : Guid.Parse(userId),
                active
            });
            return result ?? new ApplicationCountDto();
        }

        public async Task<AppLookup?> FindApplicationAsync(int id)
        {
            var result = await DatabaseHelper.QuerySingleAsync<AppLookup>(_tpmailerdb, "sel_application", new { appcode = id });
            return result;
        }

        public async Task<AppLookup?> FindApplicationAsync(Guid id, bool? active = null)
        {
            using var conn = _tpmailerdb();
            return await conn.QueryFirstOrDefaultAsync<AppLookup?>("sel_application", new { appclient = id, active }, commandType: CommandType.StoredProcedure);
        }

        public async Task<ServiceResult> UpdateApplicationApprovalAsync(int appId)
        {
            using var conn = _tpmailerdb();
            return ServiceResult.FromRowsAffected(await conn.ExecuteScalarAsync<int>("commit_approveapp", new { appcode = appId }, commandType: CommandType.StoredProcedure));
        }

        public async Task<ServiceResult> GetAppCountAsync()
        {
            var result = await GetApplicationCountAsync();
            return ServiceResult.FromCount(result.TotalCount);
        }

        public async Task<ServiceResult> LogAsync(ActivityLog data)
        {
            using var conn = _tpmailerdb();
            return ServiceResult.FromRowsAffected(await conn.ExecuteAsync("commit_activitylog", new
            {
                logtypeid = Convert.ToInt32(data.LogTypeLookupId),
                action = data.Description,
                requestpath = data.Url,
                createdby = data.LoggedBy
            }, commandType: CommandType.StoredProcedure));
        }

        public async Task<ServiceResult> AppLoginAsync(AppLogin data)
        {
            using var conn = _tpmailerdb();
            return ServiceResult.FromRowsAffected(await conn.ExecuteAsync("commit_loginaudit", new
            {
                userid = data.UserId,
                email = data.Email,
                ipaddress = data.IPAddress,
                success = data.Success
            }, commandType: CommandType.StoredProcedure));
        }

        public async Task<IEnumerable<ActivityLog>> GetAllAsync()
        {
            var result = await DatabaseHelper.QueryListAsync<ActivityLog>(_tpmailerdb, "sel_activitylog");
            return result ?? Enumerable.Empty<ActivityLog>();
        }

        public async Task<IList<ActivityLog>> GetAllAsync(int pageIndex, int pageSize)
        {
            var result = await DatabaseHelper.QueryListAsync<ActivityLog>(_tpmailerdb, "sel_activitylog", new
            {
                pageindex = pageIndex,
                pagesize = pageSize
            });
            return result;
        }

        public async Task<ServiceResult> GetActivityLogCountAsync()
        {
            var result = await DatabaseHelper.QuerySingleAsync<int>(_tpmailerdb, "sel_activitylog", new { countonly = true });
            return ServiceResult.FromCount(result);
        }

        public async Task<ServiceResult> SaveAsync(ErrorLog data)
        {
            using var conn = _tpmailerdb();
            return ServiceResult.FromRowsAffected(await conn.ExecuteAsync("commit_errorlog", new
            {
                errormessage = data.Error,
                source = data.ErrorSource,
                createdby = data.LoggedBy
            }, commandType: CommandType.StoredProcedure));
        }

        public async Task<ServiceResult> SaveAppUserAsync(AppUser data)
        {
            using var conn = _tpmailerdb();
            return ServiceResult.FromEntityId(await conn.ExecuteScalarAsync<Guid>("commit_user", new
            {
                email = data.Email,
                emailblindindex = data.EmailBlindIndex,
                upn = data.Upn,
                username = data.Username,
                appsecret = data.AppSecret,
                salt = data.Salt,
                encryptionkey = data.EncryptionKey,
                appcode = data.AppCode,
                active = data.Active,
                modifiedby = data.CreatedBy
            }, commandType: CommandType.StoredProcedure));
        }

        public async Task<ServiceResult> SaveUserRoleAsync(AppUserRole data)
        {
            using var conn = _tpmailerdb();
            return ServiceResult.FromRowsAffected(await conn.ExecuteScalarAsync<int>("commit_userrole", new { userid = data.UserId, roleid = data.RoleId }, commandType: CommandType.StoredProcedure));
        }

        public async Task<ServiceResult> UpdateUserRoleAsync(Guid userId, int newRoleId, string modifiedBy)
        {
            using var conn = _tpmailerdb();
            return ServiceResult.FromRowsAffected(await conn.ExecuteScalarAsync<int>("commit_userroleupdate", new { userid = userId, newroleid = newRoleId, modifiedby = modifiedBy }, commandType: CommandType.StoredProcedure));
        }

        public async Task<ServiceResult> UpdateToVerifiedUserAsync(Guid userId, int active = 1)
        {
            using var conn = _tpmailerdb();
            return ServiceResult.FromRowsAffected(await conn.ExecuteScalarAsync<int>("commit_verifyuser", new { userid = userId, active = (active == 1) }, commandType: CommandType.StoredProcedure));
        }

        public async Task<ServiceResult> UpdateAppUserCredentialsAsync(AppUser data)
        {
            using var conn = _tpmailerdb();
            return ServiceResult.FromRowsAffected(await conn.ExecuteScalarAsync<int>("commit_updatesecret", new
            {
                userid = data.UserId,
                appsecret = data.AppSecret,
                salt = data.Salt,
                encryptionkey = data.EncryptionKey,
                modifiedby = data.ModifiedBy
            }, commandType: CommandType.StoredProcedure));
        }

        public async Task<ServiceResult> AppSecreteUpdateLogAsync(AppUser data)
        {
            using var conn = _tpmailerdb();
            return ServiceResult.FromRowsAffected(await conn.ExecuteScalarAsync<int>("commit_updatesecret", new { userid = data.UserId, modifiedby = data.ModifiedBy }, commandType: CommandType.StoredProcedure));
        }

        public async Task<ServiceResult> SaveAppUserCredentialsLogAsync(PasswordUpdate data)
        {
            using var conn = _tpmailerdb();
            return ServiceResult.FromRowsAffected(await conn.ExecuteScalarAsync<int>("commit_secretlog", new
            {
                userid = data.UserId,
                noofupdate = data.NoOfUpdate,
                createdby = data.CreatedBy,
                modifiedby = data.ModifiedBy
            }, commandType: CommandType.StoredProcedure));
        }

        public async Task<IList<AppUserGetDto>> FindAppUsersAsync()
        {
            using var conn = _tpmailerdb();
            var userList = (await conn.QueryAsync<AppUserGetDto>("sel_user", commandType: CommandType.StoredProcedure)).ToList();
            
            if (userList.Any())
            {
                var allRoles = await conn.QueryAsync<UserRoleLookup>(
                    "sel_userrole",
                    commandType: CommandType.StoredProcedure
                );
                
                var roleLookup = allRoles.ToLookup(r => r.UserId.ToString());
                
                foreach (var user in userList)
                {
                    user.Roles = roleLookup[user.UserId]
                        .Select(r => new AppUserRoleDto
                        {
                            RoleId = r.RoleId,
                            RoleName = r.RoleName,
                            Description = r.RoleDescription
                        }).ToList();
                }
            }
            
            return userList;
        }
        
        private class UserRoleLookup
        {
            public Guid UserId { get; set; }
            public int RoleId { get; set; }
            public string RoleName { get; set; } = string.Empty;
            public string RoleDescription { get; set; } = string.Empty;
        }

        public async Task<IList<AppUserGetDto>> FindAppUsersAsync(int pageIndex, int pageSize, string? searchTerm = null, bool? active = null, int? roleId = null, string? sortBy = null)
        {
            using var conn = _tpmailerdb();
            var userList = (await conn.QueryAsync<AppUserGetDto>("sel_user", new
            {
                pageindex = pageIndex,
                pagesize = pageSize,
                searchterm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm,
                active,
                roleid = roleId,
                sortby = string.IsNullOrWhiteSpace(sortBy) ? null : sortBy
            }, commandType: CommandType.StoredProcedure)).ToList();

            if (userList.Any())
            {
                var userIdGuids = userList
                    .Select(u => Guid.TryParse(u.UserId, out var guid) ? guid : Guid.Empty)
                    .Where(g => g != Guid.Empty)
                    .ToList();
                var allRoles = await conn.QueryAsync<UserRoleLookup>(
                    "sel_userrole",
                    commandType: CommandType.StoredProcedure
                );
                
                var roleLookup = allRoles.Where(r => userIdGuids.Contains(r.UserId)).ToLookup(r => r.UserId.ToString());
                
                foreach (var user in userList)
                {
                    user.Roles = roleLookup[user.UserId]
                        .Select(r => new AppUserRoleDto
                        {
                            RoleId = r.RoleId,
                            RoleName = r.RoleName,
                            Description = r.RoleDescription
                        }).ToList();
                }
            }

            return userList;
        }


        public async Task<UserCountDto> GetUserCountAsync(string? searchTerm = null, bool? active = null, int? roleId = null)
        {
            var result = await DatabaseHelper.QuerySingleAsync<UserCountDto>(_tpmailerdb, "sel_usercount", new
            {
                searchterm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm,
                active,
                roleid = roleId
            });
            return result ?? new UserCountDto();
        }

        public async Task<AppUserGetDto?> FindAppUserByEmailAsync(string email)
        {
            using var conn = _tpmailerdb();
            var appUser = await conn.QuerySingleOrDefaultAsync<AppUserGetDto>("sel_user", new { emailblindindex = email }, commandType: CommandType.StoredProcedure);
            if (appUser != null)
            {
                if (Guid.TryParse(appUser.UserId, out Guid userIdGuid))
                    appUser.Roles = (await conn.QueryAsync<AppUserRoleDto>("sel_userrole", new { userid = userIdGuid }, commandType: CommandType.StoredProcedure)).ToList();
                else
                    appUser.Roles = new List<AppUserRoleDto>();
                appUser.Applications = (await conn.QueryAsync<ApplicationGetDto>("sel_userapplication", new { userid = appUser.UserId }, commandType: CommandType.StoredProcedure)).ToList();
            }
            return appUser;
        }

        public async Task<AppUserGetDto?> FindAppUserByUpnAsync(string upn)
        {
            using var conn = _tpmailerdb();
            var appUser = await conn.QuerySingleOrDefaultAsync<AppUserGetDto>("sel_user", new { upn }, commandType: CommandType.StoredProcedure);
            if (appUser != null)
            {
                if (Guid.TryParse(appUser.UserId, out Guid userIdGuid))
                    appUser.Roles = (await conn.QueryAsync<AppUserRoleDto>("sel_userrole", new { userid = userIdGuid }, commandType: CommandType.StoredProcedure)).ToList();
                else
                    appUser.Roles = new List<AppUserRoleDto>();
                appUser.Applications = (await conn.QueryAsync<ApplicationGetDto>("sel_userapplication", new { userid = appUser.UserId }, commandType: CommandType.StoredProcedure)).ToList();
            }
            return appUser;
        }

        public async Task<AppUserGetDto?> FindAppUserByIdAsync(string userId)
        {
            using var conn = _tpmailerdb();
            var appUser = await conn.QuerySingleOrDefaultAsync<AppUserGetDto>("sel_user", new { userid = userId }, commandType: CommandType.StoredProcedure);
            if (appUser != null)
            {
                if (Guid.TryParse(appUser.UserId, out Guid userIdGuid))
                    appUser.Roles = (await conn.QueryAsync<AppUserRoleDto>("sel_userrole", new { userid = userIdGuid }, commandType: CommandType.StoredProcedure)).ToList();
                else
                    appUser.Roles = new List<AppUserRoleDto>();
                appUser.Applications = (await conn.QueryAsync<ApplicationGetDto>("sel_userapplication", new { userid = appUser.UserId }, commandType: CommandType.StoredProcedure)).ToList();
            }
            return appUser;
        }

        public async Task<ServiceResult> GetAppUserCountAsync()
        {
            var result = await DatabaseHelper.QuerySingleAsync<int>(_tpmailerdb, "sel_usercount");
            return ServiceResult.FromCount(result);
        }

        private async Task<IList<ApplicationGetDto>?> FindUserApplicationAsync(string userId) =>
            await DatabaseHelper.QueryListAsync<ApplicationGetDto>(_tpmailerdb, "sel_userapplication", new { userid = userId });

        private async Task<IList<AppUserRoleDto>?> FindAppUserRoleAsync(string userId)
        {
            if (!Guid.TryParse(userId, out Guid userIdGuid))
                return new List<AppUserRoleDto>();
            return await DatabaseHelper.QueryListAsync<AppUserRoleDto>(_tpmailerdb, "sel_userrole", new { userid = userIdGuid });
        }

        public async Task<IList<AppRole>> FindAppRoleAsync() =>
            await DatabaseHelper.QueryListAsync<AppRole>(_tpmailerdb, "sel_role");
    }
}

