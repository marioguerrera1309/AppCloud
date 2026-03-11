using System.Net.Http;
using System.Text; // Libreria per codificare i dati in JSON da inviare al server
using System.Text.Json; // Libreria per serializzare e deserializzare oggetti in JSON
using System.Windows;
namespace CloudFG
{
    public partial class LoginWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();
        public LoginWindow()
        {
            InitializeComponent();
        }
        private async void BtnLoginClick(object sender, RoutedEventArgs e)
        {
            var data = new { Username = txtUserLogin.Text, Password = txtPassLogin.Password };
            await InviaRichiestaAuth("http://localhost:8080/login", data);
        }
        private async void BtnRegisterClick(object sender, RoutedEventArgs e)
        {
            if (txtPassReg.Password != txtPassConfirm.Password) {
                MessageBox.Show("Le password non coincidono!", "Erorre di validazione!",MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var data = new { Username = txtUserReg.Text, Password = txtPassReg.Password };
            await InviaRichiestaAuth("http://localhost:8080/register", data);
        }
        // Metodo per inviare le richieste di autenticazione (login/registrazione)
        private async Task InviaRichiestaAuth(string url, object data)
        {
            string username = "";
            try 
            {
                string json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content);
                if (response.IsSuccessStatusCode) {
                    // Il server restitsce il cookie di sessione come contenuto della risposta
                    string token = await response.Content.ReadAsStringAsync();
                    // Salvataggio in locale
                    CloudFG.Properties.Settings.Default.UserToken = token;
                    CloudFG.Properties.Settings.Default.Save();
                    // Apre MainWindow e chiude LoginWindow
                    string[] partialToken = token.Split('-');
                    username = partialToken[0];
                    string timestampStr = partialToken[1];
                    //MessageBox.Show("Autenticazione avvenuta con successo!", "Successo", MessageBoxButton.OK, MessageBoxImage.Information);
                    new MainWindow(username).Show();
                    this.Close();
                }
                else {
                    string errorMessage = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(errorMessage)) errorMessage = "Errore sconosciuto";
                    MessageBox.Show(errorMessage, "Errore Login", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Server non raggiungibile: " + ex.Message);
            }
        }
    }
}
