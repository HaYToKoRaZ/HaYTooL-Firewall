using GuvenlikDuvarim.Core.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace GuvenlikDuvarim.UI.ViewModels
{
    /// <summary>
    /// Panel 2'deki TreeView'da her bir düğümü temsil eden ViewModel.
    /// Klasör düğümleri genişletilebilir; EXE düğümleri yaprak (leaf) düğümdür.
    /// </summary>
    public class ContentTreeNode : INotifyPropertyChanged
    {
        private string _displayName = string.Empty;

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

        /// <summary>Klasörün genişletilmiş/daraltılmış durumu</summary>
        public bool IsExpanded { get; set; } = true;

        /// <summary>Tooltip'te gösterilecek tam yol</summary>
        public string ToolTipText => FullPath;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
