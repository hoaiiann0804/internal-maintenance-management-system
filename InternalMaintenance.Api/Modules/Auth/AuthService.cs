using InternalMaintenance.Api.Constants;
using InternalMaintenance.Api.Data;
using InternalMaintenance.Api.Common.Results;
using InternalMaintenance.Api.Models;
using InternalMaintenance.Api.Modules.Auth.Contracts;
using InternalMaintenance.Api.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace InternalMaintenance.Api.Modules.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public AuthService(
        AppDbContext context,
        JwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    public async Task<ServiceResult<LoginResponse>> LoginWithGoogleAsync(GoogleLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "Google ID token is required."
            );
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                IssuedAtClockTolerance = TimeSpan.FromMinutes(5)
            };

            var clientId = _configuration["Google:ClientId"];
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                settings.Audience = new[] { clientId };
            }

            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
        }
        catch (InvalidJwtException ex)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                $"Invalid Google authentication token: {ex.Message}"
            );
        }
        catch (Exception)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                "Google authentication verification failed."
            );
        }

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "Email claim not provided by Google account."
            );
        }

        var email = payload.Email.Trim().ToLower();

        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user is null)
        {
            // Auto-provision user (for recruiters or new staff using Google SSO)
            var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == UserRoles.Staff)
                ?? await _context.Roles.FirstAsync();

            var defaultDept = await _context.Departments.FirstOrDefaultAsync();

            user = new User
            {
                FullName = !string.IsNullOrWhiteSpace(payload.Name) ? payload.Name : payload.Email,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                RoleId = staffRole.Id,
                Role = staffRole,
                DepartmentId = defaultDept?.Id,
                Department = defaultDept,
                IsActive = true,
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        else
        {
            if (!user.IsActive)
            {
                return ServiceResult<LoginResponse>.Fail(
                    StatusCodes.Status403Forbidden,
                    "Your account has been deactivated. Please contact the administrator."
                );
            }

            user.LastLoginAt = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(user.FullName) && !string.IsNullOrWhiteSpace(payload.Name))
            {
                user.FullName = payload.Name;
            }
        }

        var token = _jwtTokenService.GenerateAccessToken(user);
        var expiresInMinutes = _jwtTokenService.GetAccessTokenLifeTime();
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtTokenService.GetRefreshTokenLifeTime())
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return ServiceResult<LoginResponse>.Success(
            new LoginResponse
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresInMinutes = expiresInMinutes,
                RefreshToken = refreshToken,
                MustChangePassword = user.MustChangePassword,
                User = BuildAuthUserResponse(user)
            }
        );
    }

    public async Task<ServiceResult<LoginResponse>> SwitchRoleAsync(int userId, SwitchRoleRequest request)
    {
        var targetRoleName = request.RoleName?.Trim();
        var validRoles = new[] { UserRoles.Admin, UserRoles.Manager, UserRoles.Technician, UserRoles.Staff };

        if (string.IsNullOrWhiteSpace(targetRoleName) || !validRoles.Contains(targetRoleName))
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status400BadRequest,
                $"Invalid role. Valid roles: {string.Join(", ", validRoles)}"
            );
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status404NotFound,
                "User not found"
            );
        }

        if (!user.IsActive)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status403Forbidden,
                "Your account has been deactivated. Please contact the administrator."
            );
        }

        var targetRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == targetRoleName);
        if (targetRole is null)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status400BadRequest,
                "Target role does not exist in database"
            );
        }

        // If switching to Manager or Technician, ensure user has an IT/Maintenance department for full maintenance capabilities
        if ((targetRoleName == UserRoles.Manager || targetRoleName == UserRoles.Technician) && (user.DepartmentId == null || user.Department?.IsMaintenanceTeam != true))
        {
            var maintenanceDept = await _context.Departments.FirstOrDefaultAsync(d => d.IsMaintenanceTeam)
                ?? await _context.Departments.FirstOrDefaultAsync();
            if (maintenanceDept != null)
            {
                user.DepartmentId = maintenanceDept.Id;
                user.Department = maintenanceDept;
            }
        }

        user.RoleId = targetRole.Id;
        user.Role = targetRole;
        user.UpdatedAt = DateTime.UtcNow;

        var token = _jwtTokenService.GenerateAccessToken(user);
        var expiresInMinutes = _jwtTokenService.GetAccessTokenLifeTime();
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtTokenService.GetRefreshTokenLifeTime())
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return ServiceResult<LoginResponse>.Success(
            new LoginResponse
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresInMinutes = expiresInMinutes,
                RefreshToken = refreshToken,
                MustChangePassword = user.MustChangePassword,
                User = BuildAuthUserResponse(user)
            }
        );
    }

    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLower();

        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user is null)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                "Invalid email or password"
            );
        }

        if (!user.IsActive)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status403Forbidden,
                "Your account has been deactivated. Please contact the administrator."
            );
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!passwordValid)
        {
            return ServiceResult<LoginResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                "Invalid email or password"
            );
        }

        user.LastLoginAt = DateTime.UtcNow;

        var token = _jwtTokenService.GenerateAccessToken(user);
        var expiresInMinutes = _jwtTokenService.GetAccessTokenLifeTime();
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtTokenService.GetRefreshTokenLifeTime())
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return ServiceResult<LoginResponse>.Success(
            new LoginResponse
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresInMinutes = expiresInMinutes,
                RefreshToken = refreshToken,
                MustChangePassword = user.MustChangePassword,
                User = BuildAuthUserResponse(user)
            }
        );
    }

    public async Task<ServiceResult<AuthUserResponse>> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return ServiceResult<AuthUserResponse>.Fail(
                StatusCodes.Status404NotFound,
                "User not found"
            );
        }

        if (!user.IsActive)
        {
            return ServiceResult<AuthUserResponse>.Fail(
                StatusCodes.Status403Forbidden,
                "Your account has been deactivated. Please contact the administrator."
            );
        }

        return ServiceResult<AuthUserResponse>.Success(BuildAuthUserResponse(user));
    }

    public async Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return ServiceResult.Fail(
                StatusCodes.Status401Unauthorized,
                "User not found"
            );
        }

        if (!user.IsActive)
        {
            return ServiceResult.Fail(
                StatusCodes.Status403Forbidden,
                "Your account has been deactivated. Please contact the administrator."
            );
        }

        var currentPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
        if (!currentPasswordValid)
        {
            return ServiceResult.Fail(
                StatusCodes.Status400BadRequest,
                "Current password is incorrect"
            );
        }

        var isSamePassword = BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash);
        if (isSamePassword)
        {
            return ServiceResult.Fail(
                StatusCodes.Status400BadRequest,
                "New password must be different from current password"
            );
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ServiceResult.Success(StatusCodes.Status200OK);
    }

    public async Task<ServiceResult<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var storedRefreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(rt => rt.Role)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedRefreshToken is null)
        {
            return ServiceResult<RefreshTokenResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                "Refresh token not found"
            );
        }

        if (storedRefreshToken.IsRevoked)
        {
            return ServiceResult<RefreshTokenResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                "Refresh token revoked"
            );
        }

        if (storedRefreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            return ServiceResult<RefreshTokenResponse>.Fail(
                StatusCodes.Status401Unauthorized,
                "Refresh token expired"
            );
        }

        var user = storedRefreshToken.User;
        if (!user.IsActive)
        {
            return ServiceResult<RefreshTokenResponse>.Fail(
                StatusCodes.Status403Forbidden,
                "Your account has been deactivated."
            );
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtTokenService.GetRefreshTokenLifeTime())
        };

        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.RevokedAt = DateTime.UtcNow;
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return ServiceResult<RefreshTokenResponse>.Success(
            new RefreshTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresInMinutes = _jwtTokenService.GetAccessTokenLifeTime()
            }
        );
    }

    public async Task<ServiceResult> LogoutAsync(LogoutRequest request)
    {
        var storedRefreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedRefreshToken is null)
        {
            return ServiceResult.Fail(
                StatusCodes.Status401Unauthorized,
                "Refresh token not found"
            );
        }

        if (storedRefreshToken.IsRevoked)
        {
            return ServiceResult.Success(StatusCodes.Status204NoContent);
        }

        storedRefreshToken.IsRevoked = true;
        storedRefreshToken.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ServiceResult.Success(StatusCodes.Status204NoContent);
    }

    private static AuthUserResponse BuildAuthUserResponse(User user)
    {
        return new AuthUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            RoleName = user.Role!.Name,
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.Name,
            // Kiểm tra user biết phòng của mình có phải là phònhg bảo trì hay không
            DepartmentIsMaintenanceTeam = user.Department?.IsMaintenanceTeam ?? false,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword
        };
    }
}
