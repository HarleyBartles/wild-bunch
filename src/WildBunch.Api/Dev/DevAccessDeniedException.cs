namespace WildBunch.Api.Dev;

/// <summary>
/// Thrown by DevRoleGuard when dev endpoint access is denied.
/// Dev endpoints catch this and return 403 Forbid.
/// </summary>
public sealed class DevAccessDeniedException : Exception
{
    public DevAccessDeniedException(string message) : base(message) { }
}
