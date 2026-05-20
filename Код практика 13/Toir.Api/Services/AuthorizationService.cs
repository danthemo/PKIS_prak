namespace Toir.Api.Services;

public sealed class AuthorizationService
{
    private static readonly Dictionary<string, HashSet<string>> Permissions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Dispatcher"] = ["requests:create", "requests:read", "requests:assign", "requests:update", "workorders:read", "equipment:read"],
        ["Engineer"] = ["requests:read", "workorders:read", "workorders:close"],
        ["Chief"] = ["requests:read", "requests:create", "requests:update", "workorders:read", "equipment:read"],
        ["Admin"] = ["equipment:read"],
        ["SecurityOfficer"] = ["audit:read"]
    };

    public CurrentUser Require(HttpContext context, string permission)
    {
        var userId = context.Request.Headers["X-User-Id"].FirstOrDefault();
        var role = context.Request.Headers["X-User-Role"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Не переданы заголовки X-User-Id и X-User-Role.");
        }

        if (!Permissions.TryGetValue(role, out var permissions) || !permissions.Contains(permission))
        {
            throw new ApiException(StatusCodes.Status403Forbidden, "FORBIDDEN", "У роли нет прав на выполнение операции.");
        }

        return new CurrentUser(userId, role);
    }
}
