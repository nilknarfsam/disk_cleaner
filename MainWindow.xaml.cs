using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        private readonly ObservableCollection<UsbDisk> _allDisks = new();
        private readonly ObservableCollection<UsbDisk> _displayDisks = new();
        private readonly DispatcherTimer _refreshTimer = new();
        
        private bool _isRefreshing;
        private bool _isExecuting;
        private bool _isDarkMode;
        private string _lastSignature = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            GridDisks.ItemsSource = _displayDisks;

            PopulateDriveLetters();
            LoadThemePreference();

            _refreshTimer.Interval = TimeSpan.FromSeconds(RefreshSeconds);
            _refreshTimer.Tick += async (s, e) => await RefreshDisksAsync(silent: true);

            Loaded += async (s, e) =>
            {
                await RefreshDisksAsync(silent: false);
                _refreshTimer.Start();
            };
        }

        private void PopulateDriveLetters()
        {
            var letters = DiskService.GetAvailableDriveLetters();
            ComboDriveLetter.ItemsSource = letters;
            ComboDriveLetter.SelectedIndex = 0;
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
                    var selectedNumbers = _allDisks.Where(d => d.IsSelected).Select(d => d.Number).ToHashSet();

                    _allDisks.Clear();
                    foreach (var disk in newDisks)
                    {
                        if (selectedNumbers.Contains(disk.Number))
                        {
                            disk.IsSelected = true;
                        }
                        _allDisks.Add(disk);
                    }

                    _lastSignature = signature;
                    ApplySearchFilter();
                    TxtStatus.Text = $"{_allDisks.Count} disco(s) USB detectado(s). Atualização automática a cada {RefreshSeconds}s.";
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

        private void ApplySearchFilter()
        {
            string query = TxtSearch.Text.Trim().ToLower();
            _displayDisks.Clear();

            foreach (var disk in _allDisks)
            {
                if (string.IsNullOrEmpty(query) ||
                    disk.FriendlyName.ToLower().Contains(query) ||
                    disk.Number.ToString().Contains(query) ||
                    disk.DriveLetters.ToLower().Contains(query))
                {
                    _displayDisks.Add(disk);
                }
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        private void LoadThemePreference()
        {
            try
            {
                string cfgPath = Path.Combine(Path.GetTempPath(), "Limpador_USB_theme.cfg");
                if (File.Exists(cfgPath))
                {
                    string theme = File.ReadAllText(cfgPath).Trim();
                    if (theme == "dark")
                    {
                        SetDarkMode(true);
                    }
                }
            }
            catch { }
        }

        private void SaveThemePreference()
        {
            try
            {
                string cfgPath = Path.Combine(Path.GetTempPath(), "Limpador_USB_theme.cfg");
                File.WriteAllText(cfgPath, _isDarkMode ? "dark" : "light");
            }
            catch { }
        }

        private void SetDarkMode(bool enableDark)
        {
            _isDarkMode = enableDark;

            if (_isDarkMode)
            {
                Background = new SolidColorBrush(Color.FromRgb(17, 24, 39));
                MainRootGrid.Background = new SolidColorBrush(Color.FromRgb(17, 24, 39));
                TxtTitle.Foreground = Brushes.White;
                GridDisks.Background = new SolidColorBrush(Color.FromRgb(31, 41, 55));
                GridDisks.Foreground = Brushes.White;
                BtnTheme.Content = "☀️ Modo Claro";
                BtnTheme.Background = new SolidColorBrush(Color.FromRgb(55, 65, 81));
                BtnTheme.Foreground = Brushes.White;
            }
            else
            {
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 252));
                MainRootGrid.Background = new SolidColorBrush(Color.FromRgb(250, 250, 252));
                TxtTitle.Foreground = new SolidColorBrush(Color.FromRgb(17, 24, 39));
                GridDisks.Background = Brushes.White;
                GridDisks.Foreground = Brushes.Black;
                BtnTheme.Content = "🌙 Modo Escuro";
                BtnTheme.Background = new SolidColorBrush(Color.FromRgb(229, 231, 235));
                BtnTheme.Foreground = Brushes.Black;
            }

            SaveThemePreference();
        }

        private void BtnTheme_Click(object sender, RoutedEventArgs e)
        {
            SetDarkMode(!_isDarkMode);
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDisksAsync(silent: false);
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var disk in _displayDisks)
            {
                disk.IsSelected = true;
            }
        }

        private void BtnUnselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var disk in _allDisks)
            {
                disk.IsSelected = false;
            }
        }

        private void GridDisks_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (GridDisks.SelectedItem is UsbDisk disk)
            {
                disk.IsSelected = !disk.IsSelected;
            }
        }

        private async void BtnEject_Click(object sender, RoutedEventArgs e)
        {
            var selectedDisks = _allDisks.Where(d => d.IsSelected).ToList();
            if (!selectedDisks.Any())
            {
                MessageBox.Show("Selecione pelo menos um disco USB para ejetar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var disk in selectedDisks)
            {
                var (success, msg) = await DiskService.EjectUsbDiskAsync(disk);
                if (success)
                {
                    MessageBox.Show(msg, "Ejeção Concluída", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(msg, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            await RefreshDisksAsync(silent: false);
        }

        private async void BtnExecute_Click(object sender, RoutedEventArgs e)
        {
            var selectedDisks = _allDisks.Where(d => d.IsSelected).ToList();
            if (!selectedDisks.Any())
            {
                MessageBox.Show("Selecione pelo menos um disco USB.", "Nenhum disco selecionado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string mode = RadioCleanOnly.IsChecked == true ? "clean_only" : "format_quick";
            string fsType = (ComboFs.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "NTFS";
            string label = TxtLabel.Text.Trim();
            string customLetter = ComboDriveLetter.SelectedItem?.ToString() ?? "Automática";

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
                : $"FORMATAR RAPIDAMENTE EM {fsType} (Rótulo: {(string.IsNullOrEmpty(label) ? "FORMATADO" : label)} | Letra: {customLetter})";

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

                    TxtStatus.Text = $"Desmontando volumes do Disco {number}...";
                    await DiskService.ForceDismountVolumesAsync(number);

                    TxtStatus.Text = $"Processando Disco {number}...";

                    var commands = DiskpartService.BuildDiskpartCommands(number, mode, fsType, label, customLetter);
                    var (success, output) = await DiskpartService.ExecuteDiskpartAsync(commands);

                    logs.Add($"===== Disco {number} =====\n{output}\n");

                    if (!success)
                    {
                        throw new InvalidOperationException($"Falha ao processar o Disco {number}.");
                    }
                }

                // Notificação sonora de conclusão
                try { SystemSounds.Asterisk.Play(); } catch { }

                MessageBox.Show("Operação finalizada com sucesso nos discos selecionados.", "Concluído", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshDisksAsync(silent: false);
            }
            catch (Exception ex)
            {
                string logPath = DiskpartService.SaveErrorLog(ex.ToString());
                
                var result = MessageBox.Show(
                    $"{ex.Message}\n\nLog salvo em:\n{logPath}\n\nDeseja copiar o caminho do log para a área de transferência?",
                    "Erro na Execução",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error
                );

                if (result == MessageBoxResult.Yes)
                {
                    try { Clipboard.SetText(logPath); } catch { }
                }
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
            BtnEject.IsEnabled = enabled;
            RadioCleanOnly.IsEnabled = enabled;
            RadioFormatQuick.IsEnabled = enabled;
            ComboFs.IsEnabled = enabled;
            TxtLabel.IsEnabled = enabled;
            ComboDriveLetter.IsEnabled = enabled;
            GridDisks.IsEnabled = enabled;
            TxtSearch.IsEnabled = enabled;
        }
    }
}
