/* FilePatcher.cs
 * License: NCSA Open Source License
 * 
 * Copyright: Merijn Hendriks
 * AUTHORS:
 * Merijn Hendriks
 * waffle.lord
 */

using System;
using System.IO;

namespace Neko.EFT.Manager.X.Classes
{

    public static class FilePatcher
    {
        public static event EventHandler<ProgressInfo> PatchProgress;

        private static void RaisePatchProgress(int percentage, string message)
        {
            PatchProgress?.Invoke(null, new ProgressInfo(percentage, message));
        }

        public static PatchResultInfo Patch(string targetfile, string patchfile, int current, int total, bool ignoreInputHashMismatch = false)
        {
            byte[] target = VFS.ReadFile(targetfile);
            byte[] patch = VFS.ReadFile(patchfile);

            PatchResult result = PatchUtil.Patch(target, PatchInfo.FromBytes(patch));



            int progress = (int)Math.Floor((double)current / total * 100);
            RaisePatchProgress(progress, $"补丁中... {Path.GetFileName(targetfile)}");

            switch (result.Result)
            {
                case PatchResultType.Success:
                    File.Copy(targetfile, $"{targetfile}.bak");
                    VFS.WriteFile(targetfile, result.PatchedData);
                    break;

                case PatchResultType.InputChecksumMismatch:
                    if (ignoreInputHashMismatch)
                        return new PatchResultInfo(PatchResultType.Success, 1, 1);
                    break;
            }

            return new PatchResultInfo(result.Result, 1, 1);
        }

        private static PatchResultInfo PatchAll(string targetPath, string patchPath, bool ignoreInputHashMismatch = false)
        {
            DirectoryInfo di = new DirectoryInfo(patchPath);

            var patchFiles = di.GetFiles("*.bpf", SearchOption.AllDirectories);
            int countFiles = patchFiles.Length;
            int processed = 0;

            foreach (FileInfo file in patchFiles)
            {
                var relativeFile = file.FullName.Substring(patchPath.Length).TrimStart('\\', '/');
                var target = new FileInfo(VFS.Combine(targetPath, relativeFile.Replace(".bpf", "")));

                PatchResultInfo result = Patch(target.FullName, file.FullName, processed, countFiles, ignoreInputHashMismatch);
                if (!result.OK)
                {
                    return result;
                }

                processed++;
            }

            RaisePatchProgress(100, "客户端补丁完成！");
            return new PatchResultInfo(PatchResultType.Success, processed, countFiles);
        }

        public static PatchResultInfo Run(string targetPath, string patchPath, bool ignoreInputHashMismatch = false)
        {
            return PatchAll(targetPath, patchPath, ignoreInputHashMismatch);
        }

        public static void Restore(string filePath)
        {
            RestoreRecurse(new DirectoryInfo(filePath));
        }

        private static void RestoreRecurse(DirectoryInfo baseDir)
        {
            foreach (var dir in baseDir.EnumerateDirectories())
            {
                RestoreRecurse(dir);
            }

            foreach (var file in baseDir.GetFiles())
            {
                if (file.Extension == ".bak")
                {
                    var target = Path.ChangeExtension(file.FullName, null);
                    try
                    {
                        var patched = new FileInfo(target);
                        patched.IsReadOnly = false;
                        patched.Delete();

                        File.Copy(file.FullName, target);
                        file.IsReadOnly = false;
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                        // Handle exception
                    }
                }
            }
        }
    }

}