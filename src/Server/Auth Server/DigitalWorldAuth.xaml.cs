using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using Digital_World.Network;
using Digital_World.Packets;
using Digital_World.Helpers;

namespace Digital_World
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class AuthMainWin : Window
    {
        SocketWrapper server;
        HttpServer httpServer;
        FtpServer ftpServer;
        List<Client> clients = new List<Client>();
        Settings Opt;

        public AuthMainWin()
        {
            InitializeComponent();

            server = new SocketWrapper();
            server.OnAccept += new SocketWrapper.dlgAccept(m_auth_OnAccept);
            server.OnRead += new SocketWrapper.dlgRead(m_auth_OnRead);
            server.OnClose += new SocketWrapper.dlgClose(server_OnClose);

            // Configurar logger com múltiplos TextBox
            MultiLogger _writer = new MultiLogger(tLogAuth, tLogWeb);

            Opt = Settings.Deserialize("Settings.json");
            
            // Inicializar Entity Framework Core e criar banco de dados
            try
            {
                SqlDB.InitializeEF(
                    Opt.Database.Host,
                    Opt.Database.Username,
                    Opt.Database.Password,
                    Opt.Database.Schema
                );
                MultiLogger.LogServer("[INFO] Database initialized successfully");
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[ERROR] Failed to initialize database: {0}", ex.Message);
                MessageBox.Show($"Falha ao inicializar banco de dados:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            // Iniciar servidor HTTP/HTTPS
            httpServer = new HttpServer(
                Opt.AuthServer.PatchPath, 
                Opt.AuthServer.HttpPort,
                Opt.AuthServer.HttpsPort,
                Opt.AuthServer.HttpsEnabled,
                Opt.AuthServer.CertificatePath,
                Opt.AuthServer.CertificatePassword,
                Opt.AuthServer.CertificateType
            );
            
            // Iniciar servidor FTP
            ftpServer = new FtpServer(
                Opt.AuthServer.FtpUploadPath,
                Opt.AuthServer.FtpPort,
                Opt.AuthServer.FtpUsername,
                Opt.AuthServer.FtpPassword
            );
            
            if (Opt.AuthServer.AutoStart)
            {
                ServerInfo info = new ServerInfo(Opt.AuthServer.Port, Opt.AuthServer.IP);
                server.Listen(info);
                
                if (Opt.AuthServer.HttpEnabled)
                    httpServer.Start();
                    
                if (Opt.AuthServer.FtpEnabled)
                    ftpServer.Start();
            }
        }

        void m_auth_OnRead(Client client, byte[] buffer, int length)
        {
            //TODO: Packet Response Logic
            PacketLogic.Process(client, buffer);
        }

        void m_auth_OnAccept(Client state)
        {
            state.Handshake();
            clients.Add(state);
        }

        void server_OnClose(Client client)
        {
            try
            {
                clients.Remove(client);
            }
            catch { }
        }


        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (server.Running) return;
            ServerInfo info = new ServerInfo(Opt.AuthServer.Port,
                 Opt.AuthServer.IP);
            server.Listen(info);
            
            if (Opt.AuthServer.HttpEnabled && !httpServer.IsRunning)
                httpServer.Start();
                
            if (Opt.AuthServer.FtpEnabled && !ftpServer.IsRunning)
                ftpServer.Start();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            server.Stop();
            
            if (httpServer != null && httpServer.IsRunning)
                httpServer.Stop();
                
            if (ftpServer != null && ftpServer.IsRunning)
                ftpServer.Stop();
            
            foreach(Client client in clients)
            {
                client.Send(new Packets.Auth.LoginMessage("Server is shutting down."));
                client.m_socket.Close();
            }
        }

        private void mi_opt_Click(object sender, RoutedEventArgs e)
        {
            Options winOpt = new Options();
            if (winOpt.ShowDialog().Value)
            {
                // Parar servidores ativos antes de recriar
                if (httpServer != null && httpServer.IsRunning)
                    httpServer.Stop();
                
                if (ftpServer != null && ftpServer.IsRunning)
                    ftpServer.Stop();
                
                Opt = Settings.Deserialize();
                
                // Atualizar servidores com novas configurações
                httpServer = new HttpServer(
                    Opt.AuthServer.PatchPath, 
                    Opt.AuthServer.HttpPort,
                    Opt.AuthServer.HttpsPort,
                    Opt.AuthServer.HttpsEnabled,
                    Opt.AuthServer.CertificatePath,
                    Opt.AuthServer.CertificatePassword,
                    Opt.AuthServer.CertificateType
                );
                
                ftpServer = new FtpServer(
                    Opt.AuthServer.FtpUploadPath,
                    Opt.AuthServer.FtpPort,
                    Opt.AuthServer.FtpUsername,
                    Opt.AuthServer.FtpPassword
                );
            }
        }

        private void chkEncryption_Changed(object sender, RoutedEventArgs e)
        {
            if (txtEncryptionStatus == null) return; // Evita erro durante inicialização XAML
            
            if (chkEncryption.IsChecked == true)
            {
                Tools.CryptoManager.EnableEncryption();
                txtEncryptionStatus.Text = "ATIVA";
                txtEncryptionStatus.Foreground = Brushes.LimeGreen;
            }
            else
            {
                Tools.CryptoManager.DisableEncryption();
                txtEncryptionStatus.Text = "DESATIVADA";
                txtEncryptionStatus.Foreground = Brushes.Red;
            }
        }
    }
}
