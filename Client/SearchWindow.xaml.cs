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
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response;
                if (string.IsNullOrWhiteSpace(query))
                {
                    this.Title = "I tuoi documenti";
                    response = await client.GetAsync($"http://localhost:8080/search_all?user={username}");
                }
                else
                {
                    response = await client.GetAsync($"http://localhost:8080/search?query={query}&user={username}");
                }
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var risultati = JsonSerializer.Deserialize<List<Document>>(json);
                    if (risultati != null)
                    {
                        lstResults.ItemsSource = risultati;
                        this.Show();
                    }
                    else
                    {
                        MessageBox.Show("Nessun risultato trovato.");
                        lstResults.ItemsSource = null;
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore durante la ricerca: " + ex.Message);
            }
        }

        private async void BtnResultClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            var libro = button.DataContext as Document;
            if (libro == null) return;
            MessageBox.Show($"Hai scelto: {libro.Title}. Avvio download...");
            try
            {
                HttpClient client = new HttpClient();
                var response = await client.GetAsync($"http://localhost:8080/download?hash={libro.Hash}&user={username}");
                if (response.IsSuccessStatusCode)
                {
                    string tempFolder = Path.Combine(Path.GetTempPath(), "CloudFGDownloads");
                    Directory.CreateDirectory(tempFolder);
                    string fileName = libro.Title ?? libro.Hash ?? "documento_senza_nome";
                    string fullSavePath = Path.Combine(tempFolder, fileName);
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(fullSavePath, fileBytes);
                    var psi = new ProcessStartInfo
                    {
                        FileName = fullSavePath,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore nel download: {ex.Message}");
            }
        }

        private async void BtnDeleteResultClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var libro = button?.DataContext as Document;
            if (libro == null) return;
            var conferma = MessageBox.Show($"Sei sicuro di voler eliminare '{libro.Title}'?", "Conferma", MessageBoxButton.YesNo);
            if (conferma == MessageBoxResult.Yes)
            {
                try
                {
                    HttpClient client = new HttpClient();
                    var response = await client.DeleteAsync($"http://localhost:8080/delete?hash={libro.Hash}&user={username}");
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Eliminato!");
                        LoadResult(query);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Errore: {ex.Message}");
                }
            }
        }

        private async void BtnViewAnaliticsClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var libro = button?.DataContext as Document;
            if (libro == null) return;
            try
            {
                HttpClient client = new HttpClient();
                var response = await client.GetAsync($"http://localhost:8080/download_analitics?hash={libro.Hash}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var analitics = JsonSerializer.Deserialize<Analitics>(content);
                    if (analitics == null)
                    {
                        MessageBox.Show("Dati analitics non validi ricevuti dal server.");
                        return;
                    }
                    MessageBox.Show($"Analisi per '{libro.Title}':\nGulpease Index: {analitics.GulpeaseIndex}\nLettere: {analitics.Letters}\nParole: {analitics.Words}\nFrasi: {analitics.Sentences}\nTempo di lettura(in minuti): {analitics.ReadTime}\nTempo di analisi(in secondi): {analitics.TimeAnalysis}\nParole uniche: {analitics.UniqueWords}");
                }
                else
                {
                    MessageBox.Show("Nessun dato analitics disponibile per questo documento.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante il recupero delle analisi: {ex.Message}");
            }
        }
    }
}
