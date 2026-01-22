using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Digital_World.Helpers;
using static Digital_World.Helpers.Settings;

namespace Digital_World.Data
{
    /// <summary>
    /// Factory para criar DbContext em design-time (usado por dotnet ef commands)
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DigitalWorldContext>
    {
        public DigitalWorldContext CreateDbContext(string[] args)
        {
            // Carregar configurações
            Settings settings;
            
            // Procurar Settings.json na pasta raiz do projeto
            var settingsPath = System.IO.Path.Combine("..", "Settings.json");
            if (!System.IO.File.Exists(settingsPath))
            {
                settingsPath = "Settings.json";
            }
            
            if (System.IO.File.Exists(settingsPath))
            {
                settings = Settings.Deserialize(settingsPath);
            }
            else
            {
                // Valores padrão se Settings.json não existir
                settings = new Settings();
                settings.Database = new DatabaseSettings
                {
                    Host = "192.168.0.100",
                    Username = "dmo",
                    Password = "Tb6!kV7-yM!z7FS#BevB",
                    Schema = "digitalworld"
                };
            }

            // Construir connection string
            var connectionString = $"Server={settings.Database.Host};" +
                                 $"Database={settings.Database.Schema};" +
                                 $"User={settings.Database.Username};" +
                                 $"Password={settings.Database.Password};" +
                                 $"AllowPublicKeyRetrieval=True;" +
                                 $"SslMode=None;";

            var optionsBuilder = new DbContextOptionsBuilder<DigitalWorldContext>();
            optionsBuilder.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString)
            );

            return new DigitalWorldContext(optionsBuilder.Options);
        }
    }
}
