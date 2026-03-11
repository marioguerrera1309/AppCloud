using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
namespace CloudFG
{
    public partial class SearchWindow : Window
    {
        private readonly string username;
        private string query;
        public SearchWindow(string query, string username)
        {
            InitializeComponent();
            this.username = username;
            this.query = query;
            this.Title = "Risultati per: " + query;
            LoadResult(query);
        }
        private async void LoadResult(string query)
        {
            try {
                HttpClient client = new HttpClient();
                // Chiamata al server Go passando la query nell'URL
                HttpResponseMessage response;
                if(string.IsNullOrWhiteSpace(query)) {
                    this.Title = "I tuoi documenti";
                    response = await client.GetAsync($"http://localhost:8080/search_all?user={username}");
                } else {
                    response = await client.GetAsync($"http://localhost:8080/search?query={query}&user={username}");
                }
                if (response.IsSuccessStatusCode) {
                    string json = await response.Content.ReadAsStringAsync();
                    // Trasforma il JSON ricevuto dal server in una lista di oggetti Document
                    var risultati = JsonSerializer.Deserialize<List<Document>>(json);
                    if (risultati != null)
                    {
                        // ItemSource prende un IEnumerable (come List) e lo usa per creare la lista di risultati
                        // usando il DataTemplate definito in XAML per visualizzare le proprietà di ogni oggetto.
                        lstResults.ItemsSource = risultati;
                        this.Show();
                    } else {
                        MessageBox.Show("Nessun risultato trovato.");
                        lstResults.ItemsSource = null;
                        this.Close();
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show("Errore durante la ricerca: " + ex.Message);
            }
        }
        // Metodo per aprire il file selezionato quando si clicca sul pulsante "Visualizza"
        private async void BtnResultClick(object sender, RoutedEventArgs e)
        {
            // Ottiene il documento associato al pulsante cliccato usando DataContext
            var button = sender as Button; // Il pulsante è il sender dell'evento e lo convertiamo a Button
            if(button == null) return;
            var libro = button.DataContext as Document; 
            // Il button non ha un DataContext ma lo eredita
            if(libro == null) return;
            MessageBox.Show($"Hai scelto: {libro.Title}. Avvio download...");
            try {
                HttpClient client = new HttpClient();
                // Chiediamo il file al server usando l'hash
                var response = await client.GetAsync($"http://localhost:8080/download?hash={libro.Hash}&user={username}");
                if (response.IsSuccessStatusCode) {
                    // Definiamo dove salvare il file temporaneamente (es: nella cartella Download dell'utente)
                    string tempFolder = Path.Combine(Path.GetTempPath(), "CloudFGDownloads");
                    Directory.CreateDirectory(tempFolder);
                    // Se Title è null, usa l'Hash o una stringa generica come nome file
                    string fileName = libro.Title ?? libro.Hash ?? "documento_senza_nome";
                    string fullSavePath = Path.Combine(tempFolder, fileName);
                    // Salviamo il file su disco
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(fullSavePath, fileBytes);
                    //apriamo il file con il programma predefinito (Word, PDF Reader, ecc.)
                    var psi = new ProcessStartInfo {
                        FileName = fullSavePath,
                        UseShellExecute = true // Fondamentale per aprire il file e non un esegbile
                    };
                    Process.Start(psi);
                }
            } catch (Exception ex) {
                MessageBox.Show($"Errore nel download: {ex.Message}");
            }
        }
        // Metodo per eliminare il file selezionato quando si clicca sul pulsante "Elimina"
        private async void BtnDeleteResultClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var libro = button?.DataContext as Document;
            if (libro == null) return;
            var conferma = MessageBox.Show($"Sei sicuro di voler eliminare '{libro.Title}'?", "Conferma", MessageBoxButton.YesNo);
            if (conferma == MessageBoxResult.Yes)
            {
                try {
                    HttpClient client = new HttpClient();
                    var response = await client.DeleteAsync($"http://localhost:8080/delete?hash={libro.Hash}&user={username}");
                    if (response.IsSuccessStatusCode) {
                    MessageBox.Show("Eliminato!");
                    //ricarica la ricerca per aggiornare la lista
                    LoadResult(query); 
                }
                } catch (Exception ex) {
                    MessageBox.Show($"Errore: {ex.Message}");
                }
            }
        }
        // Metodo per visualizzare le analisi del documento selezionato quando si clicca sul pulsante "Visualizza analisi"
        private async void BtnViewAnaliticsClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var libro = button?.DataContext as Document;
            if (libro == null) return;
            try {
                HttpClient client = new HttpClient();
                var response = await client.GetAsync($"http://localhost:8080/download_analitics?hash={libro.Hash}");
                if (response.IsSuccessStatusCode) {
                    var content = await response.Content.ReadAsStringAsync();
                    //MessageBox.Show($"Contenuto ricevuto: {content}");
                    var analitics = JsonSerializer.Deserialize<Analitics>(content);
                    MessageBox.Show($"Analisi per '{libro.Title}':\nGulpease Index: {analitics.GulpeaseIndex}\nLettere: {analitics.Letters}\nParole: {analitics.Words}\nFrasi: {analitics.Sentences}\nTempo di lettura(in minuti): {analitics.ReadTime}\nTempo di analisi(in secondi): {analitics.TimeAnalysis}\nParole uniche: {analitics.UniqueWords}");
                } else {
                    MessageBox.Show("Nessun dato analitics disponibile per questo documento.");
                }
            } catch (Exception ex) {
                MessageBox.Show($"Errore durante il recupero delle analisi: {ex.Message}");
            }
        }
    }
}
