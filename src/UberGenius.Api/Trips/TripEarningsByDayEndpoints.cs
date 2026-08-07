using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using UberGenius.Api.Auth;
using UberGenius.Api.Data;

namespace UberGenius.Api.Trips;

// Raw (StartTimeUtc, Earnings) pairs for completed trips — bucketing by day-of-week happens
// client-side using the browser's local timezone (Date.getDay()), which is simpler and more
// correct than doing UTC-to-local conversion server-side without a stored per-trip timezone.
public record TripEarningsPoint(DateTime StartTimeUtc, decimal Earnings);

public static class TripEarningsByDayEndpoints
{
    public static void MapTripEarningsByDayEndpoints(this WebApplication app)
    {
        app.MapGet("/api/trips/earnings-points", async (ClaimsPrincipal principal, AppDbContext db) =>
        {
            // No Status filter, deliberately — see TripSummaryEndpoints.cs for why: a cancelled
            // trip can still carry a real matched cancellation fee, and Trip.Earnings is already
            // 0 for genuinely unpaid trips either way.
            var userId = principal.GetUserId();
            var points = await db.Trips
                .Where(t => t.UserId == userId)
                .Select(t => new TripEarningsPoint(t.StartTimeUtc, t.Earnings))
                .ToListAsync();

            return Results.Ok(points);
        }).RequireAuthorization();
    }
}
