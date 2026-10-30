using GuvenlikDuvarim.Core.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace GuvenlikDuvarim.UI.ViewModels
{
    /// <summary>
    /// Panel 2'deki tablo (DataGrid) ve düğümleri temsil eden ViewModel.
    /// Simge, Adı, Konum, Gelen ve Giden sütun durumlarını barındırır.
    /// </summary>
    public class ContentTreeNode : INotifyPropertyChanged
    {
        private string _displayName = string.Empty;
        private string _inboundStatus = "-";
        private string _inboundStatusColor = "#6B7280";
        private string _inboundBadgeBackground = "Transparent";

        private string _outboundStatus = "-";
        private string _outboundStatusColor = "#6B7280";
        private string _outboundBadgeBackground = "Transparent";

        private string _syncStatus = "-";
        private string _syncStatusColor = "#6B7280";
        private string _syncBadgeBackground = "Transparent";

        /// <summary>Kullanıcıya gösterilen kısa ad (dosya/klasör adı)</summary>
        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>Tam dosya veya klasör yolu</summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>True ise klasör düğümü, False ise EXE düğümü</summary>
        public bool IsFolder { get; set; }

        /// <summary>Windows kabuğundan çekilen native simge</summary>
        public ImageSource? Icon => IconExtractor.GetIcon(FullPath, IsFolder);

        /// <summary>Altındaki çocuk düğümler (klasör içindeki EXE'ler)</summary>
        public ObservableCollection<ContentTreeNode> Children { get; set; } = new();

        private bool _isExpanded = false;

        /// <summary>Klasörün genişletilmiş durumu (Varsayılan: false - Daraltılmış)</summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ExpanderIcon));
                }
            }
        }

        /// <summary>Daraltma/Genişletme ok simgesi (▼ Genişletilmiş / ▶ Daraltılmış)</summary>
        public string ExpanderIcon => IsFolder ? (IsExpanded ? "▼ " : "▶ ") : string.Empty;

        /// <summary>Ok butonunun sadece klasörlerde görünmesini sağlar</summary>
        public Visibility ExpanderVisibility => IsFolder ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>Tooltip'te gösterilecek tam yol</summary>
        public string ToolTipText => FullPath;

        /// <summary>Tablodaki hiyerarşik girinti miktarı</summary>
        public Thickness IndentMargin { get; set; } = new Thickness(0);

        /// <summary>Klasör satırları kalın (Bold), dosyalar normal yazı tipi</summary>
        public FontWeight NameFontWeight => IsFolder ? FontWeights.Bold : FontWeights.Normal;

        /// <summary>Gelen Bağlantı Kural Durumu (Engellendi / İzin Verildi)</summary>
        public string InboundStatus
        {
            get => _inboundStatus;
            set { if (_inboundStatus != value) { _inboundStatus = value; OnPropertyChanged(); } }
        }

        public string InboundStatusColor
        {
            get => _inboundStatusColor;
            set { if (_inboundStatusColor != value) { _inboundStatusColor = value; OnPropertyChanged(); } }
        }

        public string InboundBadgeBackground
        {
            get => _inboundBadgeBackground;
            set { if (_inboundBadgeBackground != value) { _inboundBadgeBackground = value; OnPropertyChanged(); } }
        }

        /// <summary>Giden Bağlantı Kural Durumu (Engellendi / İzin Verildi)</summary>
        public string OutboundStatus
        {
            get => _outboundStatus;
            set { if (_outboundStatus != value) { _outboundStatus = value; OnPropertyChanged(); } }
        }

        public string OutboundStatusColor
        {
            get => _outboundStatusColor;
            set { if (_outboundStatusColor != value) { _outboundStatusColor = value; OnPropertyChanged(); } }
        }

        public string OutboundBadgeBackground
        {
            get => _outboundBadgeBackground;
            set { if (_outboundBadgeBackground != value) { _outboundBadgeBackground = value; OnPropertyChanged(); } }
        }

        /// <summary>Kuralın güvenlik duvarına uygulanıp uygulanmadığı (Senkron/Uygulandı)</summary>
        public string SyncStatus
        {
            get => _syncStatus;
            set { if (_syncStatus != value) { _syncStatus = value; OnPropertyChanged(); } }
        }

        public string SyncStatusColor
        {
            get => _syncStatusColor;
            set { if (_syncStatusColor != value) { _syncStatusColor = value; OnPropertyChanged(); } }
        }

        public string SyncBadgeBackground
        {
            get => _syncBadgeBackground;
            set { if (_syncBadgeBackground != value) { _syncBadgeBackground = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
