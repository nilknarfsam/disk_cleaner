using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Limpador_USB.Models;
using Limpador_USB.Services;
using Limpador_USB.Views;

namespace Limpador_USB
{
    public partial class MainWindow : Window
    {
        private const int RefreshSeconds = 3;
        private const double Fat32MaxGb = 32.0;

        private readonly ObservableCollection<UsbDisk> _disks = new();
        private readonly DispatcherTimer _refreshTimer = new();
        
        private bool _isRefreshing;
        private bool _isExecuting;
        private string _lastSignature = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            GridDisks.ItemsSource = _disks;

            _refreshTimer.Interval = TimeSpan.FromSeconds(RefreshSeconds);
            _refreshTimer.Tick += async (s, e) => await RefreshDisksAsync(silent: true);

            Loaded += async (s, e) =>
            {
                await RefreshDisksAsync(silent: false);
                _refreshTimer.Start();
            };
        }

        private string GetDisksSignature(IEnumerable<UsbDisk> disks)
        {
            return string.Join("|", disks.Select(d => $"{d.Number}:{d.FriendlyName}:{d.SizeGB}:{d.UsedStr}:{d.DriveLetters}:{d.OperationalStatus}:{d.HealthStatus}:{d.IsReadOnly}"));
        }

        private async Task RefreshDisksAsync(bool silent)
        {
            if (_isRefreshing || _isExecuting) return;
            _isRefreshing = true;

            try
            {
                var newDisks = await DiskService.GetUsbDisksAsync();
                string signature = GetDisksSignature(newDisks);

                if (signature != _lastSignature || !silent)
                {
                    var selectedNumbers = _disks.Where(d => d.IsSelected).Select(d => d.Number).ToHashSet();

                    _disks.Clear();
                    foreach (var disk in newDisks)
                    {
                        if (selectedNumbers.Contains(disk.Number))
                        {
                            disk.IsSelected = true;
                        }
                        _disks.Add(disk);
                    }

                    _lastSignature = signature;
                    TxtStatus.Text = $"{_disks.Count} disco(s) USB detectado(s). Atualização automática a cada {RefreshSeconds}s.";
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Erro ao listar discos: {ex.Message}";
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDisksAsync(silent: false);
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var disk in _disks)
            {
                disk.IsSelected = true;
            }
        }

        private void BtnUnselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var disk in _disks)
            {
                disk.IsSelected = false;
            }
        }

        private void GridDisks_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (GridDisks.SelectedItem is UsbDisk disk)
            {
                // Alterna seleção ao clicar na linha
                disk.IsSelected = !disk.IsSelected;
            }
        }

        private async void BtnExecute_Click(object sender, RoutedEventArgs e)
        {
            var selectedDisks = _disks.Where(d => d.IsSelected).ToList();
            if (!selectedDisks.Any())
            {
                MessageBox.Show("Selecione pelo menos um disco USB.", "Nenhum disco selecionado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string mode = RadioCleanOnly.IsChecked == true ? "clean_only" : "format_quick";
            string fsType = (ComboFs.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "NTFS";
            string label = TxtLabel.Text.Trim();

            if (mode == "format_quick" && fsType == "FAT32")
            {
                var largeDisks = selectedDisks.Where(d => d.SizeGB > Fat32MaxGb).ToList();
                if (largeDisks.Any())
                {
                    string nums = string.Join(", ", largeDisks.Select(d => d.Number));
                    MessageBox.Show($"FAT32 suporta no máximo {Fat32MaxGb} GB por partição.\nDisco(s) {nums} excede(m) esse limite. Use NTFS ou exFAT.",
                        "FAT32 não suportado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            string modeLabel = mode == "clean_only"
                ? "LIMPAR E DEIXAR NÃO ALOCADO"
                : $"FORMATAR RAPIDAMENTE EM {fsType} (Rótulo: {(string.IsNullOrEmpty(label) ? "FORMATADO" : label)})";

            var dialog = new ConfirmWindow(modeLabel, selectedDisks)
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true || !dialog.IsConfirmed)
            {
                TxtStatus.Text = "Operação cancelada pelo usuário.";
                return;
            }

            _isExecuting = true;
            SetControlsEnabled(false);
            Progress.Visibility = Visibility.Visible;
            TxtStatus.Text = "Executando operação... não remova os discos.";

            try
            {
                var selectedNumbers = selectedDisks.Select(d => d.Number).ToList();
                var logs = new List<string>();

                // Revalidação prévia
                var latestDisks = await DiskService.GetUsbDisksAsync();
                var diskMap = latestDisks.ToDictionary(d => d.Number);

                foreach (int number in selectedNumbers)
                {
                    if (!diskMap.TryGetValue(number, out var disk))
                    {
                        throw new InvalidOperationException($"Disco {number} não está disponível ou deixou de ser USB.");
                    }

                    string blockReason = DiskService.DiskBlockReason(disk);
                    if (!string.IsNullOrEmpty(blockReason))
                    {
                        throw new InvalidOperationException(blockReason);
                    }

                    TxtStatus.Text = $"Processando Disco {number}...";

                    var commands = DiskpartService.BuildDiskpartCommands(number, mode, fsType, label);
                    var (success, output) = await DiskpartService.ExecuteDiskpartAsync(commands);

                    logs.Add($"===== Disco {number} =====\n{output}\n");

                    if (!success)
                    {
                        throw new InvalidOperationException($"Falha ao processar o Disco {number}.");
                    }
                }

                MessageBox.Show("Operação finalizada com sucesso nos discos selecionados.", "Concluído", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshDisksAsync(silent: false);
            }
            catch (Exception ex)
            {
                string logPath = DiskpartService.SaveErrorLog(ex.ToString());
                MessageBox.Show($"{ex.Message}\n\nLog completo salvo em:\n{logPath}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isExecuting = false;
                Progress.Visibility = Visibility.Collapsed;
                SetControlsEnabled(true);
                TxtStatus.Text = "Pronto.";
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            BtnExecute.IsEnabled = enabled;
            BtnRefresh.IsEnabled = enabled;
            BtnSelectAll.IsEnabled = enabled;
            BtnUnselectAll.IsEnabled = enabled;
            RadioCleanOnly.IsEnabled = enabled;
            RadioFormatQuick.IsEnabled = enabled;
            ComboFs.IsEnabled = enabled;
            TxtLabel.IsEnabled = enabled;
            GridDisks.IsEnabled = enabled;
        }
    }
}
