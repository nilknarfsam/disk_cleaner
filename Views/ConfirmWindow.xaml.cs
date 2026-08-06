using System.Collections.Generic;
using System.Text;
using System.Windows;
using Limpador_USB.Models;

namespace Limpador_USB.Views
{
    public partial class ConfirmWindow : Window
    {
        public bool IsConfirmed { get; private set; }

        public ConfirmWindow(string modeLabel, List<UsbDisk> disks)
        {
            InitializeComponent();
            TxtModeLabel.Text = modeLabel;

            var sb = new StringBuilder();
            foreach (var d in disks)
            {
                sb.AppendLine($"• Disco {d.Number}: {d.FriendlyName}");
                sb.AppendLine($"  Tamanho: {d.SizeGB} GB | Letra: {d.DriveLetters} | Espaço Usado: {d.UsedStr}");
                sb.AppendLine();
            }

            TxtDisksInfo.Text = sb.ToString().TrimEnd();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            DialogResult = false;
            Close();
        }
    }
}
