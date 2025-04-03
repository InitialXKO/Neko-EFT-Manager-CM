using System;
using System.Diagnostics;
using System.IO;

namespace Neko.EFT.Manager.X.Classes
{
    public class ServerCompatibilityInfo
    {
        public string ServerVersion { get; private set; }
        public string AkiServerPath { get; private set; }

        public ServerCompatibilityInfo(string akiServerPath)
        {
            AkiServerPath = akiServerPath;
            ServerVersion = GetServerVersion();
        }

        public string GetServerVersion()
        {
            string[] serverExeNames = { "Aki.Server.exe", "SPT.Server.exe", "Server.exe" };

            foreach (var exeName in serverExeNames)
            {
                string serverExePath = Path.Combine(AkiServerPath, exeName);
                if (File.Exists(serverExePath))
                {
                    FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(serverExePath);
                    // 返回标准化的版本号，转换为三部分格式
                    Version version = new Version(fileVersionInfo.FileVersion);
                    Version threeDigitVersion = new Version(version.Major, version.Minor, version.Build == -1 ? 0 : version.Build);
                    return threeDigitVersion.ToString();
                }
            }

            return "N/A";
        }



        public CompatibilityStatus CheckCompatibility(ServerModInfo modInfo)
        {
            // 解析模组的 AkiVersion 或 SPTVersion
            VersionRange modVersionRange = ParseVersionRange(modInfo.AkiVersion ?? modInfo.SPTVersion);

            // 解析服务端版本并标准化为三部分版本号
            Version serverVersion;
            try
            {
                serverVersion = ParseToThreeDigitVersion(ServerVersion);
            }
            catch (Exception ex)
            {
                // 处理服务端版本号无效
                Console.WriteLine($"服务端版本解析错误: {ex.Message}");
                return CompatibilityStatus.Incompatible;
            }

            // 检查模组的兼容性
            if (modVersionRange.IsExactVersion)
            {
                // 精确版本号比较
                Version modVersion = ParseToThreeDigitVersion(modVersionRange.ExactVersion);

                if (serverVersion == modVersion)
                {
                    return CompatibilityStatus.Compatible;
                }
                else
                {
                    return CompatibilityStatus.Incompatible;
                }
            }
            else if (modVersionRange.IsMinimumVersion)
            {
                // 最低版本要求比较
                Version minVersion = ParseToThreeDigitVersion(modVersionRange.MinimumVersion);

                if (serverVersion >= minVersion)
                {
                    return CompatibilityStatus.Compatible;
                }
                else
                {
                    return CompatibilityStatus.Incompatible;
                }
            }
            else if (modVersionRange.IsMaximumVersion)
            {
                // 最高版本要求比较
                Version maxVersion = ParseToThreeDigitVersion(modVersionRange.MaximumVersion);

                if (serverVersion <= maxVersion)
                {
                    return CompatibilityStatus.Compatible;
                }
                else
                {
                    return CompatibilityStatus.Incompatible;
                }
            }
            else
            {
                // 其他情况视为不兼容
                return CompatibilityStatus.Incompatible;
            }
        }




        private Version ParseToThreeDigitVersion(string versionString)
        {
            try
            {
                // 处理波浪号前缀
                if (versionString.StartsWith("~"))
                {
                    versionString = versionString.Substring(1).Trim();
                }

                Version version = new Version(versionString);

                // 补充缺失的部分，例如"3.9" 补充为 "3.9.0"
                if (version.Build == -1)
                {
                    return new Version(version.Major, version.Minor, 0);
                }
                else if (version.Revision == -1)
                {
                    return new Version(version.Major, version.Minor, version.Build);
                }
                return version;
            }
            catch (Exception ex)
            {
                // 处理无效版本号格式
                Console.WriteLine($"解析版本号错误: {ex.Message}");
                return new Version(0, 0, 0); // 返回一个默认的无效版本号
            }
        }




        private VersionRange ParseVersionRange(string versionString)
        {
            if (string.IsNullOrEmpty(versionString))
            {
                return new VersionRange();
            }

            versionString = versionString.Trim();

            if (versionString.StartsWith("~"))
            {
                // 处理波浪号前缀
                string version = versionString.Substring(1).Trim();
                return new VersionRange { MinimumVersion = version, IsMinimumVersion = true };
            }

            if (versionString.StartsWith(">="))
            {
                string version = versionString.Substring(2).Trim();
                return new VersionRange { MinimumVersion = version, IsMinimumVersion = true };
            }
            else if (versionString.StartsWith(">"))
            {
                string version = versionString.Substring(1).Trim();
                return new VersionRange { MinimumVersion = version, IsMinimumVersion = true };
            }
            else if (versionString.StartsWith("<="))
            {
                string version = versionString.Substring(2).Trim();
                return new VersionRange { MaximumVersion = version, IsMaximumVersion = true };
            }
            else if (versionString.StartsWith("<"))
            {
                string version = versionString.Substring(1).Trim();
                return new VersionRange { MaximumVersion = version, IsMaximumVersion = true };
            }
            else
            {
                return new VersionRange { ExactVersion = versionString, IsExactVersion = true };
            }
        }



    }

    public enum CompatibilityStatus
    {
        Compatible,
        Incompatible
    }

    public class VersionRange
    {
        public string ExactVersion { get; set; }
        public string MinimumVersion { get; set; }
        public string MaximumVersion { get; set; }

        public bool IsExactVersion { get; set; }
        public bool IsMinimumVersion { get; set; }
        public bool IsMaximumVersion { get; set; }
    }

    public static class CompatibilityStatusExtensions
    {
        public static string ToChinese(this CompatibilityStatus status)
        {
            return status switch
            {
                CompatibilityStatus.Compatible => "兼容",
                CompatibilityStatus.Incompatible => "不兼容",
                _ => "未知",
            };
        }
    }
}
