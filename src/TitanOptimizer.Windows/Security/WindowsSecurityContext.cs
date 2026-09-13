using System.Security.Principal;

namespace TitanOptimizer.Windows.Security;

public sealed class WindowsSecurityContext
{
    public bool IsAdministrator
    {
        get
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
