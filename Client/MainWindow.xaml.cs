using Microsoft.Win32;
using System.IO;
using System.Net.Http;
using System.Windows;

namespace CloudFG
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();
        private Window last;
        private readonly string username;
        public MainWindow(string username)
        {
            InitializeComponent();
            last = this;
            this.username = username;
            lblWelcome.Text = "Benvenuto, " + username;
        }

        private async void BtnSendClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                UploadDetailsWindow detailsWin = new UploadDetailsWindow();
                detailsWin.Owner = this;
                if (detailsWin.ShowDialog() == true)
                {
                    string title = detailsWin.DocumentTitle;
                    string author = username;
                    lblFileName.Text = Path.GetFileName(filePath);
                    await UploadFile(filePath, title, author);
                }
            }
        }

        private async Task UploadFile(string filePath, string title, string author)
        {
            btnSend.IsEnabled = false;
            uploadProgressBar.Visibility = Visibility.Visible;
            uploadProgressBar.IsIndeterminate = true;
            lblStatus.Text = "Caricamento in corso...";
            try
            {
                using var form = new MultipartFormDataContent();
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var streamContent = new StreamContent(fileStream);
                form.Add(new StringContent(title), "title");
                form.Add(new StringContent(author), "author");
                form.Add(streamContent, "file", Path.GetFileName(filePath));
                var response = await client.PostAsync("http://localhost:8080/upload", form);
                if (response.IsSuccessStatusCode)
                {
                    lblStatus.Text = "Successo! Il Server ha ricevuto il file.";
                    lblStatus.Foreground = System.Windows.Media.Brushes.Green;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    lblStatus.Text = "Errore: File già esistente sul server.";
                    lblStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    lblStatus.Text = $"Errore Server: {response.StatusCode}";
                    lblStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore: {ex.Message}", "Errore di Connessione");
                lblStatus.Text = "Caricamento fallito.";
            }
            finally
            {
                btnSend.IsEnabled = true;
                uploadProgressBar.Visibility = Visibility.Hidden;
            }
        }

        private void RemoveText(object sender, RoutedEventArgs e)
        {
            if (lblSearch.Text == "Inserisci un titolo")
            {
                lblSearch.Text = "";
                lblSearch.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void BtnSearchClick(object sender, RoutedEventArgs e)
        {
            string query = lblSearch.Text;
            SearchWindow searchWin = new SearchWindow(query, username);
            double offset = 40;
            double nextLeft = last.Left + offset;
            if (nextLeft + searchWin.Width > SystemParameters.VirtualScreenWidth)
            {
                nextLeft = 0;
            }
            searchWin.Left = nextLeft;
            double nextTop = last.Top + offset;
            if (nextTop + searchWin.Height > SystemParameters.VirtualScreenHeight)
            {
                nextTop = 0;
            }
            searchWin.Top = nextTop;
            last = searchWin;
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnLogoutClick(object sender, RoutedEventArgs e)
        {
            CloudFG.Properties.Settings.Default.UserToken = string.Empty;
            CloudFG.Properties.Settings.Default.Save();
            this.Closed -= WindowClosed;
            LoginWindow loginWin = new LoginWindow();
            loginWin.Show();
            this.Close();
        }

        private async void BtnDeleteAccountClick(object sender, RoutedEventArgs e)
        {
            var conferma = MessageBox.Show($"Sei sicuro di voler eliminare '{username}'?", "Conferma", MessageBoxButton.YesNo);
            if (conferma == MessageBoxResult.Yes)
            {
                try
                {
                    HttpClient client = new HttpClient();
                    var response = await client.DeleteAsync($"http://localhost:8080/delete_user?user={username}");
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Utente e relativi file eliminati correttamente!");
                        CloudFG.Properties.Settings.Default.UserToken = string.Empty;
                        CloudFG.Properties.Settings.Default.Save();
                        this.Closed -= WindowClosed;
                        LoginWindow loginWin = new LoginWindow();
                        loginWin.Show();
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Errore: {ex.Message}");
                }
            }
        }

        private void BtnAllClick(object sender, RoutedEventArgs e)
        {
            SearchWindow searchWin = new SearchWindow("", username);
            double offset = 40;
            double nextLeft = last.Left + offset;
            if (nextLeft + searchWin.Width > SystemParameters.VirtualScreenWidth)
            {
                nextLeft = 0;
            }
            searchWin.Left = nextLeft;
            double nextTop = last.Top + offset;
            if (nextTop + searchWin.Height > SystemParameters.VirtualScreenHeight)
            {
                nextTop = 0;
            }
            searchWin.Top = nextTop;
            last = searchWin;
        }
    }
}