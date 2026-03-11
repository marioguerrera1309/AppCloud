using System.Windows;
namespace CloudFG
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            string username = string.Empty;
            bool sessioneValida = false;
            bool tokenpresente = false;
            base.OnStartup(e);// Esegue prima il codice di inizializzazione di Application
            string token = CloudFG.Properties.Settings.Default.UserToken;
            // Se c'è un token salvato non scaduto vai direttamente alla MainWindow
            if (!string.IsNullOrEmpty(token))
            {
                tokenpresente = true;
                try
                {
                    string[] partialToken = token.Split('-');
                    username = partialToken[0];
                    string timestampStr = partialToken[1];
                    //facciamo la conversione del timestamp in long
                    long tokenTime = long.Parse(timestampStr);
                    long oraAttuale = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    //300 secondi = 5 minuti
                    if (oraAttuale - tokenTime <= 300) {
                        sessioneValida = true;
                    }
                }
                catch
                {
                    sessioneValida = false;
                }
            }
            if (sessioneValida) {
                new MainWindow(username).Show();
            }
            else {
                CloudFG.Properties.Settings.Default.UserToken = string.Empty;
                CloudFG.Properties.Settings.Default.Save();
                if(tokenpresente) {
                    MessageBox.Show("La sessione è scaduta. Effettua nuovamente il login.", "Sessione scaduta", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                new LoginWindow().Show();
            }
        }
    }
}
    
