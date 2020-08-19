namespace NServiceBus.Installation
{
#if NET472
    using System.Security.Principal;
#endif
    static class ElevateChecker
    {
        public static bool IsCurrentUserElevated()
        {
#if NET472
            using (var windowsIdentity = WindowsIdentity.GetCurrent())
            {
                var windowsPrincipal = new WindowsPrincipal(windowsIdentity);
                return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
            }
#endif
#if NETSTANDARD2_0
            return true;
#endif
        }
    }
}