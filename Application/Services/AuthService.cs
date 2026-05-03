using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using LogLens.Application.DTOs;
using LogLens.Application.Interfaces;
using LogLens.Domain.Entities;
using LogLens.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LogLens.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly DbContext _dbContext;

        public AuthService(IConfiguration configuration, DbContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
        }

        public async Task<AuthResult> RegisterAsync(string email, string password, UserRole role)
        {
            var normalizedEmail = NormalizeEmail(email);
            if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(password))
            {
                return Fail("Email and password are required.");
            }

            var users = _dbContext.Set<User>();
            var existing = await users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            if (existing != null)
            {
                return Fail("An account with this email already exists.");
            }

            var isFirstUser = !await users.AnyAsync();
            var effectiveRole = isFirstUser ? UserRole.Admin : role;

            var user = new User
            {
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = effectiveRole,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            var token = GenerateToken(user);
            return new AuthResult(true, token, user.Email, user.Role, null);
        }

        public async Task<AuthResult> LoginAsync(string email, string password)
        {
            var normalizedEmail = NormalizeEmail(email);
            if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(password))
            {
                return Fail("Email and password are required.");
            }

            var user = await _dbContext.Set<User>().FirstOrDefaultAsync(u => u.Email == normalizedEmail);
            if (user == null || !user.IsActive)
            {
                return Fail("Invalid credentials.");
            }

            var isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!isValid)
            {
                return Fail("Invalid credentials.");
            }

            var token = GenerateToken(user);
            return new AuthResult(true, token, user.Email, user.Role, null);
        }

        private string GenerateToken(User user)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection["Key"] ?? string.Empty;
            var issuer = jwtSection["Issuer"] ?? string.Empty;
            var audience = jwtSection["Audience"] ?? string.Empty;
            var expiresInMinutes = int.TryParse(jwtSection["ExpiresInMinutes"], out var minutes) ? minutes : 60;

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("Jwt:Key is not configured.");
            }

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static AuthResult Fail(string error) => new(false, null, null, null, error);

        private static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
    }
}