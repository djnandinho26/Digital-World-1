using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Digital_World.Network
{
    /// <summary>
    /// Servidor FTP básico - SOMENTE UPLOAD (Write-Only)
    /// Não permite leitura, listagem ou exclusão de arquivos
    /// </summary>
    public class FtpServer
    {
        private TcpListener listener;
        private bool isRunning;
        private string uploadPath;
        private int port;
        private string username;
        private string password;

        public bool IsRunning => isRunning;

        public FtpServer(string uploadPath, int port = 21, string username = "upload", string password = "upload123")
        {
            this.uploadPath = Path.GetFullPath(uploadPath);
            this.port = port;
            this.username = username;
            this.password = password;
        }

        public void Start()
        {
            if (isRunning) return;

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
                Console.WriteLine($"[FTP] Criada pasta: {uploadPath}");
            }

            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                isRunning = true;
                Console.WriteLine($"[FTP] Servidor iniciado na porta {port}");
                Console.WriteLine($"[FTP] Modo: SOMENTE UPLOAD (Write-Only)");
                Console.WriteLine($"[FTP] Salvando arquivos em: {uploadPath}");
                Console.WriteLine($"[FTP] Usuário: {username}");

                Task.Run(() => Listen());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FTP] Erro ao iniciar servidor: {ex.Message}");
                Console.WriteLine($"[FTP] Execute como Administrador ou use outra porta");
            }
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            listener?.Stop();
            Console.WriteLine("[FTP] Servidor parado");
        }

        private async void Listen()
        {
            while (isRunning)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    Task.Run(() => HandleClient(client));
                }
                catch (Exception ex)
                {
                    if (isRunning)
                        Console.WriteLine($"[FTP] Erro ao aceitar conexão: {ex.Message}");
                }
            }
        }

        private void HandleClient(TcpClient controlClient)
        {
            string clientAddress = ((IPEndPoint)controlClient.Client.RemoteEndPoint).Address.ToString();
            Console.WriteLine($"[FTP] Cliente conectado: {clientAddress}");

            try
            {
                using (var stream = controlClient.GetStream())
                using (var reader = new StreamReader(stream, Encoding.ASCII))
                using (var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true })
                {
                    // Enviar mensagem de boas-vindas
                    SendResponse(writer, 220, "Digital World FTP Server - UPLOAD ONLY");

                    bool authenticated = false;
                    string currentUser = "";
                    TcpClient dataClient = null;
                    int dataPort = 0;

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split(' ', 2);
                        string command = parts[0].ToUpper();
                        string argument = parts.Length > 1 ? parts[1] : "";

                        Console.WriteLine($"[FTP] {clientAddress} > {command} {argument}");

                        switch (command)
                        {
                            case "USER":
                                currentUser = argument;
                                SendResponse(writer, 331, "Password required");
                                break;

                            case "PASS":
                                if (currentUser == username && argument == password)
                                {
                                    authenticated = true;
                                    SendResponse(writer, 230, "User logged in (Upload Only Mode)");
                                    Console.WriteLine($"[FTP] {clientAddress} autenticado como {currentUser}");
                                }
                                else
                                {
                                    SendResponse(writer, 530, "Login incorrect");
                                    Console.WriteLine($"[FTP] {clientAddress} falha na autenticação");
                                }
                                break;

                            case "SYST":
                                if (!authenticated)
                                {
                                    SendResponse(writer, 530, "Not logged in");
                                    break;
                                }
                                SendResponse(writer, 215, "UNIX Type: L8");
                                break;

                            case "TYPE":
                                if (!authenticated)
                                {
                                    SendResponse(writer, 530, "Not logged in");
                                    break;
                                }
                                SendResponse(writer, 200, $"Type set to {argument}");
                                break;

                            case "PWD":
                                if (!authenticated)
                                {
                                    SendResponse(writer, 530, "Not logged in");
                                    break;
                                }
                                SendResponse(writer, 257, "\"/\" is current directory");
                                break;

                            case "PORT":
                                if (!authenticated)
                                {
                                    SendResponse(writer, 530, "Not logged in");
                                    break;
                                }
                                // Parse PORT command: h1,h2,h3,h4,p1,p2
                                string[] portParts = argument.Split(',');
                                if (portParts.Length == 6)
                                {
                                    string dataIp = $"{portParts[0]}.{portParts[1]}.{portParts[2]}.{portParts[3]}";
                                    dataPort = int.Parse(portParts[4]) * 256 + int.Parse(portParts[5]);
                                    SendResponse(writer, 200, "PORT command successful");
                                }
                                else
                                {
                                    SendResponse(writer, 501, "Invalid PORT command");
                                }
                                break;

                            case "PASV":
                                if (!authenticated)
                                {
                                    SendResponse(writer, 530, "Not logged in");
                                    break;
                                }
                                // Modo passivo - criar listener temporário
                                var pasvListener = new TcpListener(IPAddress.Any, 0);
                                pasvListener.Start();
                                int pasvPort = ((IPEndPoint)pasvListener.LocalEndpoint).Port;
                                
                                // Obter IP local
                                string localIp = GetLocalIPAddress();
                                string[] ipParts = localIp.Split('.');
                                int p1 = pasvPort / 256;
                                int p2 = pasvPort % 256;
                                
                                SendResponse(writer, 227, $"Entering Passive Mode ({ipParts[0]},{ipParts[1]},{ipParts[2]},{ipParts[3]},{p1},{p2})");
                                
                                // Aceitar conexão de dados em background
                                Task.Run(async () =>
                                {
                                    dataClient = await pasvListener.AcceptTcpClientAsync();
                                    pasvListener.Stop();
                                });
                                break;

                            case "STOR":
                                if (!authenticated)
                                {
                                    SendResponse(writer, 530, "Not logged in");
                                    break;
                                }
                                // STOR - único comando permitido para upload
                                string filename = argument;
                                string filepath = Path.Combine(uploadPath, Path.GetFileName(filename));

                                SendResponse(writer, 150, "Opening data connection");

                                try
                                {
                                    // Aguardar conexão de dados
                                    int waitCount = 0;
                                    while (dataClient == null && waitCount < 50)
                                    {
                                        Thread.Sleep(100);
                                        waitCount++;
                                    }

                                    if (dataClient == null)
                                    {
                                        SendResponse(writer, 425, "Can't open data connection");
                                        break;
                                    }

                                    using (var dataStream = dataClient.GetStream())
                                    using (var fileStream = File.Create(filepath))
                                    {
                                        dataStream.CopyTo(fileStream);
                                    }

                                    dataClient.Close();
                                    dataClient = null;

                                    SendResponse(writer, 226, "Transfer complete");
                                    Console.WriteLine($"[FTP] Arquivo recebido: {filename} ({new FileInfo(filepath).Length} bytes)");
                                }
                                catch (Exception ex)
                                {
                                    SendResponse(writer, 550, $"Upload failed: {ex.Message}");
                                    Console.WriteLine($"[FTP] Erro no upload: {ex.Message}");
                                }
                                break;

                            // Comandos bloqueados (não permitidos em modo upload-only)
                            case "LIST":
                            case "NLST":
                            case "RETR":
                            case "DELE":
                            case "RMD":
                            case "MKD":
                            case "CWD":
                            case "CDUP":
                            case "RNFR":
                            case "RNTO":
                                if (!authenticated)
                                {
                                    SendResponse(writer, 530, "Not logged in");
                                    break;
                                }
                                SendResponse(writer, 502, "Command not implemented (Upload Only Mode)");
                                Console.WriteLine($"[FTP] {clientAddress} comando bloqueado: {command}");
                                break;

                            case "NOOP":
                                SendResponse(writer, 200, "OK");
                                break;

                            case "QUIT":
                                SendResponse(writer, 221, "Goodbye");
                                Console.WriteLine($"[FTP] {clientAddress} desconectado");
                                return;

                            default:
                                SendResponse(writer, 500, "Unknown command");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FTP] Erro na conexão com {clientAddress}: {ex.Message}");
            }
            finally
            {
                controlClient.Close();
            }
        }

        private void SendResponse(StreamWriter writer, int code, string message)
        {
            writer.WriteLine($"{code} {message}");
        }

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }
    }
}
