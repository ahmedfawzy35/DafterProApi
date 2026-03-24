namespace StoreManagement.Server.Extensions;

public static class PathMatchingExtensions
{
    /// <summary>
    /// تحقق آمن لمطابقة المسارات العامة لتجنب ثغرات الـ Path Bypass 
    /// (مثل تطابق /api/public بشكل خاطئ مع /api/public-fake)
    /// </summary>
    public static bool IsSafePublicPath(this string? requestPath, string[] publicPaths)
    {
        if (string.IsNullOrWhiteSpace(requestPath))
            return false;

        var path = requestPath.ToLower();
        return publicPaths.Any(p => 
            path.Equals(p, StringComparison.OrdinalIgnoreCase) || 
            path.StartsWith($"{p}/", StringComparison.OrdinalIgnoreCase));
    }
}
