using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UberGenius.Api.Data;

namespace UberGenius.Api.Auth;

public record SignupRequest(string Email, string Password, string DisplayName, string TimeZoneId);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, int Id, string Email, string DisplayName);
public record MeResponse(int Id, string Email, string DisplayName);

public static class AuthEndpoints
{
    private const string DemoEmail = "demo@ubergenius.local";

    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/signup", async (
            SignupRequest request,
            AppDbContext db,
            IPasswordHasher<User> hasher,
            JwtTokenService tokens) =>
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return Results.BadRequest("Email and display name are required.");
            }
            if (request.Password.Length < 8)
            {
                return Results.BadRequest("Password must be at least 8 characters.");
            }

            // Browser-supplied, so validated before trusting it — a bad value here would
            // otherwise surface much later as a TimeZoneNotFoundException on the analytics page.
            if (!TimeZoneInfo.TryFindSystemTimeZoneById(request.TimeZoneId, out _))
            {
                return Results.BadRequest("Unrecognized time zone.");
            }

            var user = new User
            {
                Email = email,
                DisplayName = request.DisplayName.Trim(),
                CreatedAtUtc = DateTime.UtcNow,
                TimeZoneId = request.TimeZoneId,
            };
            user.PasswordHash = hasher.HashPassword(user, request.Password);

            db.Users.Add(user);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Results.Conflict("An account with that email already exists.");
            }

            return Results.Ok(new AuthResponse(tokens.CreateToken(user), user.Id, user.Email, user.DisplayName));
        });

        app.MapPost("/api/auth/login", async (
            LoginRequest request,
            AppDbContext db,
            IPasswordHasher<User> hasher,
            JwtTokenService tokens) =>
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verification == PasswordVerificationResult.Failed)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new AuthResponse(tokens.CreateToken(user), user.Id, user.Email, user.DisplayName));
        });

        app.MapPost("/api/auth/demo", async (AppDbContext db, JwtTokenService tokens) =>
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == DemoEmail);
            if (user is null)
            {
                user = new User
                {
                    Email = DemoEmail,
                    DisplayName = "Demo Driver",
                    // Never logged into via the password form, so the hash never needs to
                    // verify against anything — a random unusable value is enough.
                    PasswordHash = Guid.NewGuid().ToString(),
                    CreatedAtUtc = DateTime.UtcNow,
                    // No real trip data exists for this account, so the exact value is low
                    // stakes — picked for consistency with the app's real historical data.
                    TimeZoneId = "America/Chicago",
                };
                db.Users.Add(user);
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Lost a race with another concurrent first-demo-call; the row exists now.
                    user = await db.Users.SingleAsync(u => u.Email == DemoEmail);
                }
            }

            return Results.Ok(new AuthResponse(tokens.CreateToken(user), user.Id, user.Email, user.DisplayName));
        });

        app.MapGet("/api/auth/me", async (ClaimsPrincipal principal, AppDbContext db) =>
        {
            var user = await db.Users.FindAsync(principal.GetUserId());
            return user is null
                ? Results.Unauthorized()
                : Results.Ok(new MeResponse(user.Id, user.Email, user.DisplayName));
        }).RequireAuthorization();
    }
}
