using System;
using System.Linq;
using System.Windows;
using Digital_World.Data;
using Digital_World.Helpers;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Digital_World.Forms
{
    public partial class DatabaseConfigForm : Window
    {
        public bool ConnectionSuccessful { get; private set; }
        public string Host { get; private set; } = string.Empty;
        public string Username { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;
        public string Database { get; private set; } = string.Empty;

        public DatabaseConfigForm()
        {
            InitializeComponent();
            
            // Tentar carregar configurações existentes se houver
            LoadExistingSettings();
        }

        public DatabaseConfigForm(string errorMessage) : this()
        {
            txtStatus.Text = $"❌ Erro: {errorMessage}";
            txtStatus.Foreground = System.Windows.Media.Brushes.Red;
        }

        private void LoadExistingSettings()
        {
            try
            {
                // Tentar carregar do Settings se existir
                if (System.IO.File.Exists("Settings.json"))
                {
                    var settings = Settings.Deserialize();
                    if (settings?.Database != null)
                    {
                        txtHost.Text = settings.Database.Host ?? "localhost";
                        txtUsername.Text = settings.Database.Username ?? "root";
                        txtDatabase.Text = settings.Database.Schema ?? "digitalworld";
                    }
                }
            }
            catch
            {
                // Se falhar, usar valores padrão já definidos no XAML
            }
        }

        private async void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            btnTest.IsEnabled = false;
            btnSave.IsEnabled = false;
            txtStatus.Text = "🔄 Testando conexão...";
            txtStatus.Foreground = System.Windows.Media.Brushes.Blue;

            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    // Criar string de conexão temporária
                    string testConnectionString = $"Server={txtHost.Text};User ID={txtUsername.Text};Password={txtPassword.Password};Database={txtDatabase.Text};";
                    
                    // Tentar conectar
                    var optionsBuilder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<DigitalWorldContext>();
                    optionsBuilder.UseMySql(
                        testConnectionString,
                        Microsoft.EntityFrameworkCore.ServerVersion.AutoDetect(testConnectionString),
                        options => options.CommandTimeout(5) // Timeout de 5 segundos para teste
                    );

                    using (var context = new DigitalWorldContext(optionsBuilder.Options))
                    {
                        // Tentar abrir conexão e executar query simples
                        context.Database.CanConnect();
                        var count = context.Accounts.Count(a => true);
                        
                        Dispatcher.Invoke(() =>
                        {
                            txtStatus.Text = $"✅ Conexão bem-sucedida! Encontradas {count} contas no banco.";
                            txtStatus.Foreground = System.Windows.Media.Brushes.Green;
                            btnSave.IsEnabled = true;
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"❌ Erro na conexão: {ex.Message}";
                txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                btnSave.IsEnabled = false;
                
                MultiLogger.LogServer("[DB Config] Teste de conexão falhou: {0}", ex.Message);
            }
            finally
            {
                btnTest.IsEnabled = true;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar campos
                if (string.IsNullOrWhiteSpace(txtHost.Text))
                {
                    MessageBox.Show("Por favor, informe o host do banco de dados.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtHost.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    MessageBox.Show("Por favor, informe o usuário do banco de dados.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtUsername.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtDatabase.Text))
                {
                    MessageBox.Show("Por favor, informe o nome do banco de dados.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtDatabase.Focus();
                    return;
                }

                // Salvar valores
                Host = txtHost.Text.Trim();
                Username = txtUsername.Text.Trim();
                Password = txtPassword.Password;
                Database = txtDatabase.Text.Trim();

                // Tentar salvar nas configurações
                try
                {
                    Settings settings;
                    if (System.IO.File.Exists("Settings.json"))
                    {
                        settings = Settings.Deserialize();
                    }
                    else
                    {
                        settings = new Settings();
                    }
                    
                    if (settings.Database != null)
                    {
                        settings.Database.Host = Host;
                        settings.Database.Username = Username;
                        settings.Database.Password = Password;
                        settings.Database.Schema = Database;
                        settings.Serialize();
                        
                        MultiLogger.LogServer("[DB Config] Configurações salvas com sucesso");
                    }
                }
                catch (Exception ex)
                {
                    MultiLogger.LogServer("[DB Config] Aviso: Não foi possível salvar configurações: {0}", ex.Message);
                }

                ConnectionSuccessful = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar configurações: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                MultiLogger.LogServer("[DB Config] Erro ao salvar: {0}", ex.Message);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ConnectionSuccessful = false;
            DialogResult = false;
            Close();
        }
    }
}
