// Client/Services/NetworkConfigurator.cs
using System;
using System.Diagnostics;

public static class NetworkConfigurator
{
    public static void SetStaticIP(string interfaceName, string ip, string subnet, string gateway)
    {
        ExecuteNetshCommand($"interface ip set address name=\"{interfaceName}\" static {ip} {subnet} {gateway}");
    }

    private static void ExecuteNetshCommand(string command)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = command,
                Verb = "runas", // 需要管理员权限
                UseShellExecute = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception($"网络配置失败 (错误码: {process.ExitCode})");
    }
}
