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
using System.Windows.Shapes;
using Digital_World.Helpers;

namespace Digital_World
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {
        private Settings mySettings;

        public Options()
        {
            InitializeComponent();

            mySettings = Settings.Deserialize("Settings.json");

            tHost.Text = mySettings.AuthServer.Host;
            tPort.Text = mySettings.AuthServer.Port.ToString();
            chkStart.IsChecked = new bool?(mySettings.AuthServer.AutoStart);
            
            // Carregar configurações do database
            tDBHost.Text = mySettings.Database.Host;
            tDBUser.Text = mySettings.Database.Username;
            tDBPass.Password = mySettings.Database.Password;
            tDBSchema.Text = mySettings.Database.Schema;
            
            // Carregar configurações HTTP
            chkHttpEnabled.IsChecked = new bool?(mySettings.AuthServer.HttpEnabled);
            tHttpPort.Text = mySettings.AuthServer.HttpPort.ToString();
            chkHttpsEnabled.IsChecked = new bool?(mySettings.AuthServer.HttpsEnabled);
            tHttpsPort.Text = mySettings.AuthServer.HttpsPort.ToString();
            tPatchPath.Text = mySettings.AuthServer.PatchPath;
            
            // Carregar tipo de certificado
            cbCertType.SelectedIndex = (mySettings.AuthServer.CertificateType == "ZeroSSL") ? 1 : 0;
            
            // Carregar configurações FTP
            chkFtpEnabled.IsChecked = new bool?(mySettings.AuthServer.FtpEnabled);
            tFtpPort.Text = mySettings.AuthServer.FtpPort.ToString();
            tFtpUploadPath.Text = mySettings.AuthServer.FtpUploadPath;
            tFtpUsername.Text = mySettings.AuthServer.FtpUsername;
            tFtpPassword.Password = mySettings.AuthServer.FtpPassword;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            mySettings.AuthServer.Host = tHost.Text;
            mySettings.AuthServer.AutoStart = chkStart.IsChecked.Value;
            try
            {
                mySettings.AuthServer.Port = int.Parse(tPort.Text);
            }
            catch (FormatException)
            {
                mySettings.AuthServer.Port = 7030;
            }
            
            // Salvar configurações do database
            mySettings.Database.Host = tDBHost.Text;
            mySettings.Database.Username = tDBUser.Text;
            mySettings.Database.Password = tDBPass.Password;
            mySettings.Database.Schema = tDBSchema.Text;
            
            // Salvar configurações HTTP
            mySettings.AuthServer.HttpEnabled = chkHttpEnabled.IsChecked.Value;
            mySettings.AuthServer.HttpsEnabled = chkHttpsEnabled.IsChecked.Value;
            mySettings.AuthServer.PatchPath = tPatchPath.Text;
            
            // Salvar tipo de certificado
            mySettings.AuthServer.CertificateType = (cbCertType.SelectedIndex == 1) ? "ZeroSSL" : "Auto";
            
            try
            {
                mySettings.AuthServer.HttpPort = int.Parse(tHttpPort.Text);
            }
            catch (FormatException)
            {
                mySettings.AuthServer.HttpPort = 8080;
            }
            try
            {
                mySettings.AuthServer.HttpsPort = int.Parse(tHttpsPort.Text);
            }
            catch (FormatException)
            {
                mySettings.AuthServer.HttpsPort = 8443;
            }
            
            // Salvar configurações FTP
            mySettings.AuthServer.FtpEnabled = chkFtpEnabled.IsChecked.Value;
            mySettings.AuthServer.FtpUploadPath = tFtpUploadPath.Text;
            mySettings.AuthServer.FtpUsername = tFtpUsername.Text;
            mySettings.AuthServer.FtpPassword = tFtpPassword.Password;
            
            try
            {
                mySettings.AuthServer.FtpPort = int.Parse(tFtpPort.Text);
            }
            catch (FormatException)
            {
                mySettings.AuthServer.FtpPort = 21;
            }
            
            mySettings.Serialize("Settings.json");

            this.DialogResult = new bool?(true);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = new bool?(false);
        }
    }
}
