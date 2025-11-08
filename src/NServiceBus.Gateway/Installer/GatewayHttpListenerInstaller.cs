namespace NServiceBus.Installation;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Gateway.Receiving;
using Logging;

class GatewayHttpListenerInstaller(IManageReceiveChannels channelManager) : INeedToInstallSomething
{
    public Task Install(string identity, CancellationToken cancellationToken = default)
    {
        if (Environment.OSVersion.Version.Major <= 5)
        {
            Logger.InfoFormat(
                @"Did not attempt to grant user '{0}' HttpListener permissions since you are running an old OS. Processing will continue.
To manually perform this action run the following command for each url from an admin console:
httpcfg set urlacl /u {{http://URL:PORT/[PATH/] | https://URL:PORT/[PATH/]}} /a D:(A;;GX;;;""{1}"")", identity, identity);
            return Task.CompletedTask;
        }

        if (!ElevateChecker.IsCurrentUserElevated())
        {
            Logger.InfoFormat(
                @"Did not attempt to grant user '{0}' HttpListener permissions since process is not running with elevate privileges. Processing will continue.
To manually perform this action run the following command for each url from an admin console:
netsh http add urlacl url={{http://URL:PORT/[PATH/] | https://URL:PORT/[PATH/]}} user=""{1}""", identity, identity);
            return Task.CompletedTask;
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
                var message = $@"Failed to grant user '{identity}' HttpListener permissions due to an Exception. Processing will continue.
To help diagnose the problem try running the following command from an admin console:
netsh http add urlacl url={uri} user=""{identity}""";
                Logger.Warn(message, exception);
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
        using var process = Process.Start(startInfo);

        if (process != null)
        {
            process.WaitForExit(5000);

            if (process.ExitCode == 0)
            {
                Logger.InfoFormat("Granted user '{0}' HttpListener permissions for {1}.", identity, uri);
                return;
            }

            var error = process.StandardOutput.ReadToEnd().Trim();
            var message = $@"Failed to grant user '{identity}' HttpListener permissions. Processing will continue.
Try running the following command from an admin console:
netsh http add urlacl url={uri} user=""{identity}""

The error message from running the above command is:
{error}";
            Logger.Warn(message);
        }
    }

    static readonly ILog Logger = LogManager.GetLogger<GatewayHttpListenerInstaller>();
}