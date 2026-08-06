using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Limpador_USB.Services
{
    public class DiskpartService
    {
        public static List<string> BuildDiskpartCommands(int diskNumber, string mode, string fsType = "NTFS", string label = "FORMATADO")
        {
            string safeLabel = label.Replace("\"", "").Trim();
            if (safeLabel.Length > 32) safeLabel = safeLabel.Substring(0, 32);
            if (string.IsNullOrEmpty(safeLabel)) safeLabel = "FORMATADO";

            var commands = new List<string>
            {
                $"select disk {diskNumber}",
                "clean"
            };

            if (mode == "clean_only") return commands;

            commands.Add("convert gpt");
            commands.Add("create partition primary");
            commands.Add($"format fs={fsType.ToLower()} quick label=\"{safeLabel}\"");
            commands.Add("assign");

            return commands;
        }

        public static async Task<(bool success, string output)> ExecuteDiskpartAsync(List<string> scriptLines)
        {
            return await Task.Run(() =>
            {
                string tempFile = Path.Combine(Path.GetTempPath(), $"diskpart_script_{Guid.NewGuid():N}.txt");
                try
                {
                    File.WriteAllLines(tempFile, scriptLines, Encoding.UTF8);

                    var psi = new ProcessStartInfo
                    {
                        FileName = "diskpart",
                        Arguments = $"/s \"{tempFile}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8
                    };

                    using var process = Process.Start(psi);
                    if (process == null) return (false, "Falha ao iniciar diskpart.");

                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    string fullOutput = (stdout + "\n" + stderr).Trim();
                    return (process.ExitCode == 0, fullOutput);
                }
                finally
                {
                    if (File.Exists(tempFile))
                    {
                        try { File.Delete(tempFile); } catch { }
                    }
                }
            });
        }

        public static string SaveErrorLog(string content)
        {
            string logDir = Path.Combine(Environment.GetEnvironmentVariable("TEMP") ?? ".", "Limpador_USB_logs");
            Directory.CreateDirectory(logDir);
            string logPath = Path.Combine(logDir, $"diskpart_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            File.WriteAllText(logPath, content, Encoding.UTF8);
            return logPath;
        }
    }
}
