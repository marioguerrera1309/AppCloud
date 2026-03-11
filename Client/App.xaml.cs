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
            base.OnStartup(e);
            string token = CloudFG.Properties.Settings.Default.UserToken;
            if (!string.IsNullOrEmpty(token))
            {
                tokenpresente = true;
                try
                {
                    string[] partialToken = token.Split('-');
                    username = partialToken[0];
                    string timestampStr = partialToken[1];
                    long tokenTime = long.Parse(timestampStr);
                    long oraAttuale = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    if (oraAttuale - tokenTime <= 300)
                    {
                        sessioneValida = true;
                    }
                }
                catch
                {
                    sessioneValida = false;
                }
            }
            if (sessioneValida)
            {
                new MainWindow(username).Show();
            }
            else
            {
                CloudFG.Properties.Settings.Default.UserToken = string.Empty;
                CloudFG.Properties.Settings.Default.Save();
                if (tokenpresente)
                {
                    MessageBox.Show("La sessione è scaduta. Effettua nuovamente il login.", "Sessione scaduta", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                new LoginWindow().Show();
            }
        }
    }
}

