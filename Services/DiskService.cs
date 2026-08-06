using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                        var disk = new UsbDisk
                        {
                            Number = elem.GetProperty("Number").GetInt32(),
                            FriendlyName = elem.TryGetProperty("FriendlyName", out var fn) ? fn.GetString() ?? "" : "",
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

                        if (elem.TryGetProperty("SizeRemaining", out var sr) && sr.ValueKind == JsonValueKind.Number && sizeBytes > 0)
                        {
                            long remBytes = sr.GetInt64();
                            long usedBytes = Math.Max(0, sizeBytes - remBytes);
                            disk.UsedGB = Math.Round((double)usedBytes / (1024 * 1024 * 1024), 2);
                            disk.FreeGB = Math.Round((double)remBytes / (1024 * 1024 * 1024), 2);
                            double pct = Math.Round(((double)usedBytes / sizeBytes) * 100, 1);
                            disk.UsedStr = $"{disk.UsedGB} GB ({pct}%)";
                            disk.FreeStr = $"{disk.FreeGB} GB";
                        }
                        else
                        {
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
