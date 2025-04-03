/* ProgressReportingPatchRunner.cs
 * License: NCSA Open Source License
 * 
 * Copyright: Merijn Hendriks
 * AUTHORS:
 * waffle.lord
 */

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Neko.EFT.Manager.X.Classes
{
    public class ProgressReportingPatchRunner
    {
        private string GamePath;
        private string[] Patches;

        public ProgressReportingPatchRunner(string gamePath, string[] patches = null)
        {
            GamePath = gamePath;
            Patches = patches ?? GetCorePatches();

            FilePatcher.PatchProgress += OnPatchProgress;
        }

        private void OnPatchProgress(object sender, ProgressInfo e)
        {
            ToastNotificationHelper.ShowNotification("补丁进度", $"{e.Message} - {e.Percentage}%", "确认", (arg) =>
            {
                // 可选的确认操作
            }, "通知", "信息");
        }

        private async IAsyncEnumerable<PatchResultInfo> TryPatchFiles(bool ignoreInputHashMismatch)
        {
            FilePatcher.Restore(GamePath);

            int processed = 0;
            int countPatches = Patches.Length;

            foreach (var patch in Patches)
            {
                var result = await Task.Run(() => FilePatcher.Run(GamePath, patch, ignoreInputHashMismatch));
                if (!result.OK)
                {
                    yield return new PatchResultInfo(result.Status, processed, countPatches);
                    yield break;
                }

                processed++;
                yield return new PatchResultInfo(PatchResultType.Success, processed, countPatches);
            }
        }

        public async IAsyncEnumerable<PatchResultInfo> PatchFiles()
        {
            await foreach (var info in TryPatchFiles(false))
            {
                yield return info;

                if (info.OK)
                    continue;

                await foreach (var secondInfo in TryPatchFiles(true))
                {
                    yield return secondInfo;
                }

                yield break;
            }
        }

        private string[] GetCorePatches()
        {
            string patchesPath = null;

            // 尝试找到 Aki_Data 目录
            string akiDataPath = Path.Combine(App.ManagerConfig.AkiServerPath, "Aki_Data", "Launcher", "Patches");
            if (Directory.Exists(akiDataPath))
            {
                patchesPath = akiDataPath;
            }
            else
            {
                // 尝试找到 SPT_Data 目录
                string sptDataPath = Path.Combine(App.ManagerConfig.AkiServerPath, "SPT_Data", "Launcher", "Patches");
                if (Directory.Exists(sptDataPath))
                {
                    patchesPath = sptDataPath;
                }
            }

            if (patchesPath != null)
            {
                return VFS.GetDirectories(patchesPath);
            }
            else
            {
                ToastNotificationHelper.ShowNotification("启动失败", $"启动客户端失败：\n 未找到补丁目录，请确保客户端与服务端在同一目录下。", "确认", (arg) =>
                {
                    // 执行其他操作...
                }, "通知", "错误");
                throw new DirectoryNotFoundException("未找到补丁目录。");


            }
        }
    }
}
