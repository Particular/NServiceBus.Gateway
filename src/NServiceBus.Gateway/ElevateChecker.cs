namespace NServiceBus.Installation
{
    using System.Runtime.InteropServices;
    using System.Security.Principal;

    static class ElevateChecker
    {
        public static bool IsCurrentUserElevated()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using (var windowsIdentity = WindowsIdentity.GetCurrent())
                {
                    var windowsPrincipal = new WindowsPrincipal(windowsIdentity);
                    return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }

            return false;
        }
    }
}