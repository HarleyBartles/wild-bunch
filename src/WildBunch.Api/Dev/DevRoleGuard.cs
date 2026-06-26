using Microsoft.Extensions.Hosting;

namespace WildBunch.Api.Dev;

/// <summary>
/// Centralized dev-role guard seam. Currently checks development environment.
/// Future auth implementations replace the body of EnsureDevAccess without
/// changing call sites. Throws DevAccessDeniedException when access is denied;
/// dev endpoints catch this and return 403.
/// </summary>
public sealed class DevRoleGuard
{
    private readonly IHostEnvironment _environment;

    public DevRoleGuard(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public void EnsureDevAccess()
    {
        if (!_environment.IsDevelopment())
        {
            throw new DevAccessDeniedException("Dev endpoints are only available in the development environment.");
        }
    }
}
