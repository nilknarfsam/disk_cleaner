using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Limpador_USB.Models;

namespace Limpador_USB.Services
{
    public class DiskService
    {
        public static bool IsDiskEligible(UsbDisk disk)
        {
            if (!string.Equals(disk.BusType, "USB", StringComparison.OrdinalIgnoreCase)) return false;
            if (disk.IsBoot || disk.IsSystem) return false;
            if (disk.IsReadOnly || disk.IsOffline) return false;
            return true;
        }

        public static string DiskBlockReason(UsbDisk disk)
        {
            if (!string.Equals(disk.BusType, "USB", StringComparison.OrdinalIgnoreCase))
                return $"Disco {disk.Number} não é USB.";
            if (disk.IsBoot)
                return $"Disco {disk.Number} é disco de boot.";
            if (disk.IsSystem)
                return $"Disco {disk.Number} é disco de sistema.";
            if (disk.IsReadOnly)
                return $"Disco {disk.Number} está em somente leitura.";
            if (disk.IsOffline)
                return $"Disco {disk.Number} está offline.";
            return string.Empty;
        }

        public static List<string> GetAvailableDriveLetters()
        {
            var usedLetters = DriveInfo.GetDrives()
                .Select(d => d.Name.Substring(0, 1).ToUpper())
                .ToHashSet();

            var available = new List<string> { "Automática" };
            for (char c = 'D'; c <= 'Z'; c++)
            {
                string letter = c.ToString();
                if (!usedLetters.Contains(letter))
                {
                    available.Add($"{letter}:");
                }
            }
            return available;
        }

        public static async Task<(bool success, string message)> EjectUsbDiskAsync(UsbDisk disk)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string letters = disk.DriveLetters;
                    if (string.IsNullOrWhiteSpace(letters) || letters == "-")
                    {
                        return (false, $"O Disco {disk.Number} não possui letra de unidade atribuída para ejetar.");
                    }

                    var firstLetter = letters.Split(',').Select(l => l.Trim().Replace(":", "")).FirstOrDefault(l => !string.IsNullOrEmpty(l));
                    if (string.IsNullOrEmpty(firstLetter))
                    {
                        return (false, "Letra de unidade inválida.");
                    }

                    string psScript = $"$drive = '{firstLetter}:'; (New-Object -comObject Shell.Application).NameSpace(17).ParseName($drive).InvokeVerb('Eject')";
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var proc = Process.Start(psi);
                    proc?.WaitForExit(5000);

                    return (true, $"Disco {disk.Number} ({firstLetter}:) foi ejetado com segurança.");
                }
                catch (Exception ex)
                {
                    return (false, $"Erro ao ejetar disco: {ex.Message}");
                }
            });
        }

        public static async Task<List<UsbDisk>> GetUsbDisksAsync()
        {
            return await Task.Run(() =>
            {
                string psScript = @"
$disks = Get-Disk | Select-Object Number,FriendlyName,SerialNumber,BusType,Size,PartitionStyle,OperationalStatus,HealthStatus,IsBoot,IsSystem,IsOffline,IsReadOnly
$result = foreach ($d in $disks) {
    if ($d.BusType -eq 'USB' -and -not $d.IsBoot -and -not $d.IsSystem) {
        $parts = Get-Partition -DiskNumber $d.Number -ErrorAction SilentlyContinue
        $vols = $parts | Get-Volume -ErrorAction SilentlyContinue
        
        $driveLetters = ($vols | Where-Object { $_.DriveLetter } | ForEach-Object { $_.DriveLetter + ':' }) -join ', '
        
        $totalSize = [int64]($d.Size)
        $remSize = 0
        $hasVols = $false
        if ($vols) {
            $hasVols = $true
            foreach ($v in $vols) {
                if ($null -ne $v.SizeRemaining) {
                    $remSize += [int64]$v.SizeRemaining
                }
            }
        }

        [PSCustomObject]@{
            Number            = $d.Number
            FriendlyName      = $d.FriendlyName
            SerialNumber      = $d.SerialNumber
            BusType           = $d.BusType
            Size              = $d.Size
            SizeRemaining     = if ($hasVols) { $remSize } else { $null }
            DriveLetters      = $driveLetters
            PartitionStyle    = $d.PartitionStyle
            OperationalStatus = $d.OperationalStatus
            HealthStatus      = $d.HealthStatus
            IsBoot            = $d.IsBoot
            IsSystem          = $d.IsSystem
            IsOffline         = $d.IsOffline
            IsReadOnly        = $d.IsReadOnly
        }
    }
}
$result | ConvertTo-Json -Depth 4
";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using var process = Process.Start(psi);
                if (process == null) return new List<UsbDisk>();

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(15000);

                if (string.IsNullOrWhiteSpace(output)) return new List<UsbDisk>();

                try
                {
                    using var doc = JsonDocument.Parse(output);
                    var root = doc.RootElement;
                    var disks = new List<UsbDisk>();

                    IEnumerable<JsonElement> elements = root.ValueKind == JsonValueKind.Array
                        ? root.EnumerateArray()
                        : new[] { root };

                    foreach (var elem in elements)
                    {
                        string name = elem.TryGetProperty("FriendlyName", out var fn) ? fn.GetString() ?? "" : "";
                        var disk = new UsbDisk
                        {
                            Number = elem.GetProperty("Number").GetInt32(),
                            FriendlyName = name,
                            SerialNumber = elem.TryGetProperty("SerialNumber", out var sn) ? sn.GetString() ?? "" : "",
                            BusType = elem.TryGetProperty("BusType", out var bt) ? bt.GetString() ?? "" : "",
                            PartitionStyle = elem.TryGetProperty("PartitionStyle", out var ps) ? ps.GetString() ?? "" : "",
                            OperationalStatus = elem.TryGetProperty("OperationalStatus", out var os) ? os.GetString() ?? "" : "",
                            HealthStatus = elem.TryGetProperty("HealthStatus", out var hs) ? hs.GetString() ?? "" : "",
                            IsBoot = elem.TryGetProperty("IsBoot", out var ib) && ib.GetBoolean(),
                            IsSystem = elem.TryGetProperty("IsSystem", out var isSys) && isSys.GetBoolean(),
                            IsOffline = elem.TryGetProperty("IsOffline", out var isOff) && isOff.GetBoolean(),
                            IsReadOnly = elem.TryGetProperty("IsReadOnly", out var isRo) && isRo.GetBoolean(),
                            DriveLetters = elem.TryGetProperty("DriveLetters", out var dl) ? dl.GetString() ?? "-" : "-"
                        };

                        if (string.IsNullOrWhiteSpace(disk.DriveLetters)) disk.DriveLetters = "-";

                        if (!IsDiskEligible(disk)) continue;

                        long sizeBytes = elem.TryGetProperty("Size", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt64() : 0;
                        disk.SizeGB = sizeBytes > 0 ? Math.Round((double)sizeBytes / (1024 * 1024 * 1024), 2) : 0;

                        if (disk.SizeGB > 120 || name.ToUpper().Contains("SSD") || name.ToUpper().Contains("EXTERNAL") || name.ToUpper().Contains("PASSPORT"))
                        {
                            disk.DiskIcon = "💽";
                        }
                        else
                        {
                            disk.DiskIcon = "🔌";
                        }

                        if (elem.TryGetProperty("SizeRemaining", out var sr) && sr.ValueKind == JsonValueKind.Number && sizeBytes > 0)
                        {
                            long remBytes = sr.GetInt64();
                            long usedBytes = Math.Max(0, sizeBytes - remBytes);
                            disk.UsedGB = Math.Round((double)usedBytes / (1024 * 1024 * 1024), 2);
                            disk.FreeGB = Math.Round((double)remBytes / (1024 * 1024 * 1024), 2);
                            double pct = Math.Round(((double)usedBytes / sizeBytes) * 100, 1);
                            disk.UsedPercentage = pct;
                            disk.UsedStr = $"{disk.UsedGB} GB ({pct}%)";
                            disk.FreeStr = $"{disk.FreeGB} GB";
                        }
                        else
                        {
                            disk.UsedPercentage = 0;
                            disk.UsedStr = "Não alocado";
                            disk.FreeStr = "0 GB";
                        }

                        disks.Add(disk);
                    }

                    return disks.OrderBy(d => d.Number).ToList();
                }
                catch
                {
                    return new List<UsbDisk>();
                }
            });
        }
    }
}
