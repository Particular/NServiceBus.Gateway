namespace NServiceBus.Installation
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Gateway.Installer;
    using Gateway.Receiving;
    using Logging;
    using Settings;

    class GatewayHttpListenerInstaller : INeedToInstallSomething
    {
        IManageReceiveChannels channelManager;
        bool enabled;

        public GatewayHttpListenerInstaller(IReadOnlySettings settings)
        {
            if (!settings.TryGet<InstallerSettings>(out var installerSettings))
            {
                return;
            }

            channelManager = installerSettings.ChannelManager;
            enabled = installerSettings.Enabled;
        }

        static ILog logger = LogManager.GetLogger<GatewayHttpListenerInstaller>();

        public Task Install(string identity, CancellationToken cancellationToken = default)
        {
            if (!enabled)
            {
                return Task.FromResult(0);
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var platform = RuntimeInformation.OSDescription;

                logger.InfoFormat("Installer does not support platform {0}. Ensure that the process has required permissions to listen to configured urls.", platform);
                return Task.FromResult(0);
            }

            if (Environment.OSVersion.Version.Major <= 5)
            {
                logger.InfoFormat(
@"Did not attempt to grant user '{0}' HttpListener permissions since you are running an old OS. Processing will continue.
To manually perform this action run the following command for each url from an admin console:
httpcfg set urlacl /u {{http://URL:PORT/[PATH/] | https://URL:PORT/[PATH/]}} /a D:(A;;GX;;;""{0}"")", identity);
                return Task.FromResult(0);
            }
            if (!ElevateChecker.IsCurrentUserElevated())
            {
                logger.InfoFormat(
@"Did not attempt to grant user '{0}' HttpListener permissions since process is not running with elevate privileges. Processing will continue.
To manually perform this action run the following command for each url from an admin console:
netsh http add urlacl url={{http://URL:PORT/[PATH/] | https://URL:PORT/[PATH/]}} user=""{0}""", identity);
                return Task.FromResult(0);
            }

            foreach (var receiveChannel in channelManager.GetReceiveChannels())
            {
                if (receiveChannel.Type.ToLower() != "http")
                {
                    continue;
                }

                var uri = receiveChannel.Address;
                if (!uri.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }
                try
                {
                    StartNetshProcess(identity, uri);
                }
                catch (Exception exception)
                {
                    var message = string.Format(
@"Failed to grant user '{0}' HttpListener permissions due to an Exception. Processing will continue.
To help diagnose the problem try running the following command from an admin console:
netsh http add urlacl url={1} user=""{0}""", uri, identity);
                    logger.Warn(message, exception);
                }
            }

            return Task.CompletedTask;
        }

        static void StartNetshProcess(string identity, string uri)
        {
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                Verb = "runas",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                Arguments = $@"http add urlacl url={uri} user=""{identity}""",
                FileName = "netsh",
                WorkingDirectory = Path.GetTempPath()
            };
            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit(5000);

                if (process.ExitCode == 0)
                {
                    logger.InfoFormat("Granted user '{0}' HttpListener permissions for {1}.", identity, uri);
                    return;
                }
                var error = process.StandardOutput.ReadToEnd().Trim();
                var message = string.Format(
@"Failed to grant user '{0}' HttpListener permissions. Processing will continue.
Try running the following command from an admin console:
netsh http add urlacl url={2} user=""{0}""

The error message from running the above command is:
{1}", identity, error, uri);
                logger.Warn(message);
            }
        }
    }
}
