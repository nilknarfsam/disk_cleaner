using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Limpador_USB.Models
{
    public class UsbDisk : INotifyPropertyChanged
    {
        private bool _isSelected;

        public int Number { get; set; }
        public string FriendlyName { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string BusType { get; set; } = string.Empty;
        public double SizeGB { get; set; }
        public double? UsedGB { get; set; }
        public double? FreeGB { get; set; }
        public string UsedStr { get; set; } = "-";
        public string FreeStr { get; set; } = "-";
        public string DriveLetters { get; set; } = "-";
        public string PartitionStyle { get; set; } = string.Empty;
        public string OperationalStatus { get; set; } = string.Empty;
        public string HealthStatus { get; set; } = string.Empty;
        public bool IsBoot { get; set; }
        public bool IsSystem { get; set; }
        public bool IsOffline { get; set; }
        public bool IsReadOnly { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ReadOnlyStr => IsReadOnly ? "Sim" : "Não";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
