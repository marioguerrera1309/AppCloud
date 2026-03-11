using System.Windows;

namespace CloudFG
{
    public partial class UploadDetailsWindow : Window
    {
        public string DocumentTitle { get; private set; } = string.Empty;
        public UploadDetailsWindow()
        {
            InitializeComponent();
        }
        private void BtnConfirmClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(title.Text))
            {
                MessageBox.Show("Inserisci un titolo valido!", "Attenzione");
                return;
            }
            DocumentTitle = title.Text;
            this.DialogResult = true;
        }
    }
}
