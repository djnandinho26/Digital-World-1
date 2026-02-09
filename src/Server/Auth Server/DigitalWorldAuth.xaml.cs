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
            }
            catch (Exception ex)
            {
                MultiLogger.LogServer("[ERRO] Falha ao inicializar banco de dados: {0}", ex.Message);
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
                {
                    httpServer.Start();
                }
                    
                if (Opt.AuthServer.FtpEnabled)
                {
                    ftpServer.Start();
                }
                
                // Aguardar um pouco para os servidores realmente iniciarem
                System.Threading.Tasks.Task.Delay(200).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => UpdateServerStatus());
                });
            }
            else
            {
                // Atualizar status inicial
                UpdateServerStatus();
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
                
            // Aguardar um pouco para o servidor TCP realmente iniciar
            System.Threading.Tasks.Task.Delay(200).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() => UpdateServerStatus());
            });
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
            
            UpdateServerStatus();
        }

        private void mi_opt_Click(object sender, RoutedEventArgs e)
        {
            Options winOpt = new Options();
            if (winOpt.ShowDialog().Value)
            {
                // Salvar estado dos servidores antes de parar
                bool httpWasRunning = httpServer != null && httpServer.IsRunning;
                bool ftpWasRunning = ftpServer != null && ftpServer.IsRunning;
                
                // Parar servidores ativos antes de recriar
                if (httpWasRunning)
                    httpServer.Stop();
                
                if (ftpWasRunning)
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
                
                // Reiniciar servidores se estavam rodando
                if (httpWasRunning && Opt.AuthServer.HttpEnabled)
                    httpServer.Start();
                    
                if (ftpWasRunning && Opt.AuthServer.FtpEnabled)
                    ftpServer.Start();
                
                // Aguardar um pouco para os servidores realmente iniciarem
                System.Threading.Tasks.Task.Delay(200).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => UpdateServerStatus());
                });
            }
            else
            {
                UpdateServerStatus();
            }
        }

        private void UpdateServerStatus()
        {
            // Atualizar status TCP
            statusTcp.Fill = server.Running ? Brushes.LimeGreen : Brushes.Red;
            
            // Atualizar status HTTP - fica verde se o servidor HTTP está rodando
            bool httpRunning = httpServer != null && httpServer.IsRunning && Opt.AuthServer.HttpEnabled;
            statusHttp.Fill = httpRunning ? Brushes.LimeGreen : Brushes.Red;
            
            // Atualizar status HTTPS - fica verde se HTTPS está habilitado E o servidor está rodando
            bool httpsRunning = httpServer != null && httpServer.IsRunning && Opt.AuthServer.HttpsEnabled;
            statusHttps.Fill = httpsRunning ? Brushes.LimeGreen : Brushes.Red;
            
            // Atualizar status FTP
            statusFtp.Fill = (ftpServer != null && ftpServer.IsRunning) ? Brushes.LimeGreen : Brushes.Red;
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
