using Dapper;
using System.Data;
using System.Linq;
using TPEmail.BusinessModels.RequestModels;
using TPEmail.BusinessModels.ResponseModels;
using TPEmail.Common.Helpers;
using TPEmail.DataAccess.Interface.Repository.v1_0;

namespace TPEmail.DataAccess.Repository.v1_0
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IAppRepository _apprepository;
        private readonly Func<IDbConnection> _tpmailerdb;

        public AuthRepository(IAppRepository appRepository, Func<IDbConnection> dbFactory)
        {
            _apprepository = appRepository;
            _tpmailerdb = dbFactory;
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

        public async Task<RefreshTokenDto?> GetRefreshTokenByIdAsync(Guid tokenId)
        {
            using var conn = _tpmailerdb();
            return await conn.QueryFirstOrDefaultAsync<RefreshTokenDto>(
                "sel_refreshtokenbyid",
                new { TokenId = tokenId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task InsertRefreshTokenAsync(Guid tokenId, Guid userId, DateTime issuedAt, DateTime expiresAt, string createdByIp)
        {
            using var conn = _tpmailerdb();
            await conn.ExecuteAsync(
                "commit_insertrefreshtoken",
                new { TokenId = tokenId, UserId = userId, IssuedAt = issuedAt, ExpiresAt = expiresAt, CreatedByIp = createdByIp },
                commandType: CommandType.StoredProcedure);
        }

        public async Task RevokeRefreshTokenAsync(Guid tokenId, DateTime revokedAt, string revokedByIp, string reasonRevoked)
        {
            using var conn = _tpmailerdb();
            await conn.ExecuteAsync(
                "commit_revokerefreshtoken",
                new { TokenId = tokenId, RevokedAt = revokedAt, RevokedByIp = revokedByIp, ReasonRevoked = reasonRevoked },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<ServiceResult> CleanupExpiredTokensAsync()
        {
            using var conn = _tpmailerdb();
            return ServiceResult.FromRowsAffected(await conn.ExecuteScalarAsync<int>(
                "commit_refreshtoken",
                new { action = "CLEANUP" },
                commandType: CommandType.StoredProcedure));
        }
    }
}
