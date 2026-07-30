using Microsoft.EntityFrameworkCore;
using UberGenius.Api.Data;

namespace UberGenius.Api.Trips;

public record TripListItem(
    int Id,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    string? City,
    string? Status,
    decimal DistanceMiles,
    decimal Earnings,
    string EarningsMatchQuality);

public record TripListResult(List<TripListItem> Items, int Page, int PageSize, int TotalCount);

public static class TripListEndpoints
{
    public static void MapTripListEndpoints(this WebApplication app)
    {
        app.MapGet("/api/trips", async (AppDbContext db, int page = 1, int pageSize = 50, string sortBy = "startTime", string sortDir = "desc") =>
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 200);
            var descending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

            IQueryable<Trip> query = db.Trips;
            query = sortBy switch
            {
                "earnings" => descending ? query.OrderByDescending(t => t.Earnings) : query.OrderBy(t => t.Earnings),
                "distance" => descending ? query.OrderByDescending(t => t.DistanceMiles) : query.OrderBy(t => t.DistanceMiles),
                _ => descending ? query.OrderByDescending(t => t.StartTimeUtc) : query.OrderBy(t => t.StartTimeUtc),
            };

            var totalCount = await db.Trips.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TripListItem(
                    t.Id, t.StartTimeUtc, t.EndTimeUtc, t.City, t.Status,
                    t.DistanceMiles, t.Earnings, t.EarningsMatchQuality.ToString()))
                .ToListAsync();

            return Results.Ok(new TripListResult(items, page, pageSize, totalCount));
        });
    }
}
