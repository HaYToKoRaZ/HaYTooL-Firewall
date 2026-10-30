using System.Windows;
using System.Windows.Input;

namespace GuvenlikDuvarim.UI
{
    public partial class InputDialog : Window
    {
        public string ResponseText { get; private set; } = string.Empty;

        public InputDialog(string prompt, string title, string defaultValue = "")
        {
            InitializeComponent();
            Title = title;
            lblPrompt.Text = prompt;
            txtInput.Text = defaultValue;
            txtInput.Focus();
            txtInput.SelectAll();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = txtInput.Text;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnOk_Click(sender, e);
            }
        }

        public static string? Show(Window owner, string prompt, string title, string defaultValue = "")
        {
            var dlg = new InputDialog(prompt, title, defaultValue)
            {
                Owner = owner
            };
            return dlg.ShowDialog() == true ? dlg.ResponseText : null;
        }
    }
}
