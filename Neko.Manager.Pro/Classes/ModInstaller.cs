using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System;
using SharpCompress.Archives;
using SharpCompress.Common;

public class ModInstaller
{
    private readonly string _modsDirectoryPath;
    private readonly IModOrderService _modOrderService;

    public ModInstaller(string modsDirectoryPath, IModOrderService modOrderService)
    {
        _modsDirectoryPath = modsDirectoryPath;
        _modOrderService = modOrderService;
    }

    public async Task InstallModAsync(string modArchivePath, string modDisplayName, CancellationToken cancellationToken)
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            Directory.CreateDirectory(tempDirectory);
            await ExtractArchiveAsync(modArchivePath, tempDirectory, cancellationToken);

            var packageJsonPath = Directory.EnumerateFiles(tempDirectory, "package.json", SearchOption.AllDirectories)
                .FirstOrDefault() ?? throw new InvalidModException("Missing package.json");

            var sourceModDir = Path.GetDirectoryName(packageJsonPath);
            var modDirectoryName = Path.GetFileName(sourceModDir);
            var targetModDir = Path.Combine(_modsDirectoryPath, modDirectoryName);

            EnsureCleanTargetDirectory(targetModDir);
            CopyDirectory(sourceModDir, targetModDir);

            await UpdateModOrder(modDirectoryName);
        }
        finally
        {
            CleanupTempDirectory(tempDirectory);
        }
    }

    private async Task ExtractArchiveAsync(string archivePath, string extractPath, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            var extension = Path.GetExtension(archivePath).ToLower();

            if (extension is ".7z" or ".rar")
            {
                using var archive = new SevenZipExtractor.ArchiveFile(archivePath);
                archive.Extract(extractPath);
            }
            else if (extension == ".zip")
            {
                using var archive = ArchiveFactory.Open(archivePath);
                foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    entry.WriteToDirectory(extractPath, new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }
            else
            {
                throw new NotSupportedException($"Unsupported archive format: {extension}");
            }
        }, cancellationToken);
    }

    private void EnsureCleanTargetDirectory(string targetPath)
    {
        if (Directory.Exists(targetPath))
        {
            Directory.Delete(targetPath, true);
        }
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var newDestDir = Path.Combine(destDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, newDestDir);
        }
    }

    private async Task UpdateModOrder(string modDirectoryName)
    {
        var modOrder = (await _modOrderService.GetModOrderAsync()).ToList();
        if (!modOrder.Contains(modDirectoryName))
        {
            modOrder.Add(modDirectoryName);
            await _modOrderService.SaveModOrderAsync(modOrder);
        }
    }

    private void CleanupTempDirectory(string tempDirectory)
    {
        try
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
        catch
        {
            // Log cleanup errors if needed
        }
    }
}

public interface IModOrderService
{
    Task<IEnumerable<string>> GetModOrderAsync();
    Task SaveModOrderAsync(IEnumerable<string> modOrder);
}

public class InvalidModException : Exception
{
    public InvalidModException(string message) : base(message) { }
}
