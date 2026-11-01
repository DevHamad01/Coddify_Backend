using Application.Interfaces.AppUsers;
using Application.VMs;
using Application.VMs.AppUsers.Authentication;
using CommonOperations.Constants.Common;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Auth.AppUsers
{
    /// <summary>Block, unblock, delete and search learner accounts (admin only).</summary>
    public class AdminUserService : IAdminUserService
    {
        private readonly AppDbContext _context;

        public AdminUserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ResponseVM> SearchAsync(string? query, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var users = _context.AppUsers.AsNoTracking().Where(x => !x.IsDeleted);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                users = users.Where(x => x.Email.Contains(q) || x.FullName.Contains(q) || x.UserName.Contains(q));
            }

            var total = await users.CountAsync();
            var items = await users
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    id = x.Id.ToString(),
                    x.FullName,
                    x.Email,
                    username = x.UserName,
                    x.Role,
                    emailVerified = x.IsEmailVerified,
                    x.IsBlocked,
                    x.BlockedReason,
                    x.CreatedAt,
                    x.LastSeen
                })
                .ToListAsync();

            return ResponseVM.Ok(new { items, total, page, pageSize });
        }

        public async Task<ResponseVM> BlockAsync(DeclineUserVM model)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.Id == model.Id && !x.IsDeleted);
            if (user == null)
            {
                return ResponseVM.NotFound("User not found.", ErrorCodes.UserNotFound);
            }
            if (user.IsBlocked)
            {
                return ResponseVM.Fail(409, ErrorCodes.Conflict, "User is already blocked.");
            }

            user.IsBlocked = true;
            user.BlockedReason = model.Reason;
            user.RefreshToken = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ResponseVM.Ok(null, "User blocked.");
        }

        public async Task<ResponseVM> UnblockAsync(long id)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (user == null)
            {
                return ResponseVM.NotFound("User not found.", ErrorCodes.UserNotFound);
            }
            if (!user.IsBlocked)
            {
                return ResponseVM.Fail(409, ErrorCodes.Conflict, "User is not blocked.");
            }

            user.IsBlocked = false;
            user.BlockedReason = "";
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ResponseVM.Ok(null, "User unblocked.");
        }

        public async Task<ResponseVM> DeleteAsync(long id)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (user == null)
            {
                return ResponseVM.NotFound("User not found.", ErrorCodes.UserNotFound);
            }

            // Soft delete keeps progress and submissions consistent.
            user.IsDeleted = true;
            user.RefreshToken = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ResponseVM.Ok(null, "User deleted.");
        }
    }
}
