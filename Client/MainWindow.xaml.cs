using Microsoft.Win32;// Libreria per aprire la finestra per selezionare il file
using System.IO; // Libreria per gestire i file
using System.Net.Http; // Libreria per inviare richieste HTTP al server
using System.Windows;// Libreria per gestire le finestre
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
        // Metodo per gestire il click del pulsante "Invia"
        private async void BtnSendClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true) {
                string filePath = openFileDialog.FileName;
                UploadDetailsWindow detailsWin = new UploadDetailsWindow();
                detailsWin.Owner = this;
                if (detailsWin.ShowDialog() == true) // Resta bloccato finché non preme Conferma
                {
                    string title = detailsWin.DocumentTitle;
                    string author = username;
                    //Assegna al label il nome del file selezionato
                    lblFileName.Text = Path.GetFileName(filePath);
                    await UploadFile(filePath, title, author);
                }
            }
        }
        // Metodo per caricare il file selezionato sul server
        private async Task UploadFile(string filePath, string title, string author)
        {
            btnSend.IsEnabled = false;
            uploadProgressBar.Visibility = Visibility.Visible;
            uploadProgressBar.IsIndeterminate = true;
            lblStatus.Text = "Caricamento in corso...";
            try {
                // Crea un form http per inviare il file e i metadati al server
                using var form = new MultipartFormDataContent();
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var streamContent = new StreamContent(fileStream);
                // Aggiunge il file e i metadati al form
                form.Add(new StringContent(title), "title");
                form.Add(new StringContent(author), "author");
                form.Add(streamContent, "file", Path.GetFileName(filePath));
                var response = await client.PostAsync("http://localhost:8080/upload", form);
                if (response.IsSuccessStatusCode) {
                    lblStatus.Text = "Successo! Il Server ha ricevuto il file.";
                    lblStatus.Foreground = System.Windows.Media.Brushes.Green;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Conflict) {
                    lblStatus.Text = "Errore: File già esistente sul server.";
                    lblStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
                else {
                    lblStatus.Text = $"Errore Server: {response.StatusCode}";
                    lblStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex) {
                MessageBox.Show($"Errore: {ex.Message}", "Errore di Connessione");
                lblStatus.Text = "Caricamento fallito.";
            }
            finally {
                btnSend.IsEnabled = true;
                uploadProgressBar.Visibility = Visibility.Hidden;
            }
        }
        // Questo metodo viene chiamato quando il TextBox per la ricerca "lblSearch" viene cliccato, se il testo è quello di default lo svuota e cambia il colore del testo da grigio a nero
        private void RemoveText(object sender, RoutedEventArgs e)
        {
            if (lblSearch.Text == "Inserisci un titolo") {
                lblSearch.Text = "";
                lblSearch.Foreground = System.Windows.Media.Brushes.Black;
            }
        }
        // Metodo per gestire il click del pulsante "Cerca"
        // Apre una nuova SearchWindow con i risultati della ricerca posizionandola in modo da non sovrapporsi completamente alla finestra precedente
        private void BtnSearchClick(object sender, RoutedEventArgs e)
        {
            string query = lblSearch.Text;
            SearchWindow searchWin = new SearchWindow(query, username);
            double offset = 40;//spostamento della finestra rispetto alla finestra principale
            double nextLeft = last.Left + offset;
            // Se superiamo la larghezza dello schermo ricominciamo da sinistra
            if (nextLeft + searchWin.Width > SystemParameters.VirtualScreenWidth) {
                nextLeft = 0;
            }
            searchWin.Left = nextLeft;
            double nextTop = last.Top + offset;
            // Se superiamo la larghezza dello schermo, ricominciamo da sinistra
            if (nextTop + searchWin.Height > SystemParameters.VirtualScreenHeight) {
                nextTop = 0;
            }
            searchWin.Top = nextTop;
            last = searchWin;
            //La finestra verrà mostrata dopo aver ricevuto la risposta se non è vuota
            //searchWin.Show();
        }
        // Il metodo viene chiamato quando la finestra principale viene chiusa
        // Definendo il metodo WindowClosed quando si apre la LoginWindow dopo il logout possiamo rimuoverlo e chiudendo la MainWindow non si chiude l'intera applicazione
        private void WindowClosed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
        // Metodo per gestire il click del pulsante "Logout"
        private void BtnLogoutClick(object sender, RoutedEventArgs e)
        {
            // Rimuove il token salvato
            CloudFG.Properties.Settings.Default.UserToken = string.Empty;
            CloudFG.Properties.Settings.Default.Save();
            // Rimuove l'evento di chiusura alla MainWindow per evitare che chiuda l'intera applicazione quando chiudiamo la MainWindow per aprire la LoginWindow
            this.Closed -= WindowClosed;
            // Apre la LoginWindow
            LoginWindow loginWin = new LoginWindow();
            loginWin.Show();
            // Chiude la MainWindow
            this.Close();
        }
        // Metodo per gestire il click del pulsante "Elimina Account"
        private async void BtnDeleteAccountClick(object sender, RoutedEventArgs e)
        {   
            var conferma = MessageBox.Show($"Sei sicuro di voler eliminare '{username}'?", "Conferma", MessageBoxButton.YesNo);
            if (conferma == MessageBoxResult.Yes){
                try {
                    HttpClient client = new HttpClient();
                    var response = await client.DeleteAsync($"http://localhost:8080/delete_user?user={username}");
                    if (response.IsSuccessStatusCode){
                        MessageBox.Show("Utente e relativi file eliminati correttamente!");
                        // Rimuove il token salvato
                        CloudFG.Properties.Settings.Default.UserToken = string.Empty;
                        CloudFG.Properties.Settings.Default.Save();
                        // Rimuove l'evento di chiusura alla MainWindow per evitare che chiuda l'intera applicazione quando chiudiamo la MainWindow per aprire la LoginWindow
                        this.Closed -= WindowClosed;
                        // Apre la LoginWindow
                        LoginWindow loginWin = new LoginWindow();
                        loginWin.Show();
                        // Chiude la MainWindow
                        this.Close();
                    }
                } catch (Exception ex) {
                    MessageBox.Show($"Errore: {ex.Message}");
                }
            }   
        }
        // Metodo per gestire il click del pulsante "Visualizza tutti"
        private void BtnAllClick(object sender, RoutedEventArgs e)
        {
            SearchWindow searchWin = new SearchWindow("", username); // Passa una stringa vuota per visualizzare tutti i documenti
            double offset = 40;//spostamento della finestra rispetto alla finestra principale
            double nextLeft = last.Left + offset;
            // Se superiamo la larghezza dello schermo ricominciamo da sinistra
            if (nextLeft + searchWin.Width > SystemParameters.VirtualScreenWidth) {
                nextLeft = 0;
            }
            searchWin.Left = nextLeft;
            double nextTop = last.Top + offset;
            // Se superiamo la larghezza dello schermo, ricominciamo da sinistra
            if (nextTop + searchWin.Height > SystemParameters.VirtualScreenHeight) {
                nextTop = 0;
            }
            searchWin.Top = nextTop;
            last = searchWin;
        }
    }
}