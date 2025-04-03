using System;
using System.Diagnostics;
using System.IO;

public class N2NManager
{
    private Process _edgeNodeProcess;

    public void StartEdgeNode(string roomName, string supernodeIp, string edgeIp, string password)
    {
        string edgePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "edge.exe");

        if (!File.Exists(edgePath))
        {
            throw new FileNotFoundException("edge.exe not found!");
        }

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = edgePath,
                Arguments = $"-c {roomName} -a {edgeIp} -l {supernodeIp} -k {password}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _edgeNodeProcess = new Process
            {
                StartInfo = startInfo
            };

            _edgeNodeProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Log("Output", e.Data);
                }
            };

            _edgeNodeProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Log("Error", e.Data);
                }
            };

            _edgeNodeProcess.Start();
            _edgeNodeProcess.BeginOutputReadLine();
            _edgeNodeProcess.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            Log("Exception", ex.Message);
        }
    }

    private void Log(string type, string message)
    {
        string logMessage = $"[{DateTime.Now}] [{type}] {message}";
        File.AppendAllText("edge_log.txt", logMessage + Environment.NewLine);
    }

    public void StopEdgeNode()
    {
        if (_edgeNodeProcess != null && !_edgeNodeProcess.HasExited)
        {
            _edgeNodeProcess.Kill();
            Log("Info", "Edge node has been stopped.");
        }
    }
}
