using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Neko.EFT.Manager.X.Classes;
public class LoginHelper
{
    private readonly string _installPath;
    private readonly string _serverAddress;
    private readonly string _username;
    private readonly string _password;


    public LoginHelper(string installPath, string serverAddress, string username, string password)
    {
        _installPath = installPath;
        _serverAddress = serverAddress;
        _username = username;
        _password = password;
    }

    public async Task<string> Connect()
    {
        if (!File.Exists(Path.Combine(_installPath, "EscapeFromTarkov.exe")))
        {
            return "error";
        }

        // 创建一个新的 serverAddress 变量，避免修改 readonly 字段
        string serverAddress = _serverAddress;
        if (!serverAddress.StartsWith("http://"))
        {
            serverAddress = "http://" + serverAddress;
        }

        return await LoginToServer(serverAddress);
    }

    private async Task<string> LoginToServer(string serverAddress)
    {
        TarkovRequesting requesting = new TarkovRequesting(null, serverAddress, false);
        Dictionary<string, string> data = new Dictionary<string, string>
    {
        { "username", _username },
        { "email", _username },
        { "edition", "Edge Of Darkness" },
        { "password", _password },
        { "backendUrl", serverAddress }
    };

        try
        {
            var returnData = await requesting.PostJsonAsync("/launcher/profile/login", JsonSerializer.Serialize(data));
            if (returnData == "FAILED")
            {
                return null;
            }
            else if (returnData == "INVALID_PASSWORD")
            {
                return "error";
            }

            return returnData;
        }
        catch (Exception ex)
        {
            return "error";
        }
    }


    public async Task StartGame(string token)
    {
        try
        {
            var patchRunner = new ProgressReportingPatchRunner(_installPath);
            await foreach (var result in patchRunner.PatchFiles())
            {
                if (!result.OK)
                {
                    return;
                }
            }

            string arguments = $"-token={token} -config={{'BackendUrl':'{_serverAddress}','MatchingVersion':'live','Version':'live'}}";
            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(_installPath, "EscapeFromTarkov.exe"),
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                using (StreamReader outputReader = process.StandardOutput)
                using (StreamReader errorReader = process.StandardError)
                {
                    string output = await outputReader.ReadToEndAsync();
                    string error = await errorReader.ReadToEndAsync();
                    Debug.WriteLine("Output: " + output);
                    Debug.WriteLine("Error: " + error);
                }

                await process.WaitForExitAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("启动游戏时发生错误: " + ex.Message);
        }
    }
}

