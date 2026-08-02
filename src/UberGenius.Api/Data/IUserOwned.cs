namespace UberGenius.Api.Data;

public interface IUserOwned
{
    int UserId { get; set; }
}

public static class UserOwnedExtensions
{
    public static void StampUserId(this IEnumerable<IUserOwned> items, int userId)
    {
        foreach (var item in items)
        {
            item.UserId = userId;
        }
    }
}
