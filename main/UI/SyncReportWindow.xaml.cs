using System.Collections.Generic;
using System.Windows;

namespace GuvenlikDuvarim.UI
{
    public partial class SyncReportWindow : Window
    {
        public SyncReportWindow(int scannedFolders, List<string> addedExes, List<string> removedExes, string timeTaken)
        {
            InitializeComponent();
            txtScannedFolders.Text = scannedFolders.ToString();
            txtAddedExes.Text = addedExes.Count.ToString();
            txtRemovedExes.Text = removedExes.Count.ToString();
            txtTimeTaken.Text = $"İşlem süresi: {timeTaken}";

            lstAddedExes.ItemsSource = addedExes.Count > 0 ? addedExes : new List<string> { "Yeni eklenen dosya yok." };
            lstRemovedExes.ItemsSource = removedExes.Count > 0 ? removedExes : new List<string> { "Temizlenen hayalet kural yok." };
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
