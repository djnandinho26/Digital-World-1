using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Digital_World.Network
{
    public class HttpServer
    {
        private HttpListener listener;
        private bool isRunning;
        private string rootPath;
        private int httpPort;
        private int httpsPort;
        private bool httpsEnabled;
        private string certificatePath;
        private string certificatePassword;

        public bool IsRunning => isRunning;

        public HttpServer(string rootPath, int httpPort = 8080, int httpsPort = 8443, 
                         bool httpsEnabled = false, string certificatePath = "", string certificatePassword = "")
        {
            this.rootPath = Path.GetFullPath(rootPath);
            this.httpPort = httpPort;
            this.httpsPort = httpsPort;
            this.httpsEnabled = httpsEnabled;
            this.certificatePath = certificatePath;
            this.certificatePassword = certificatePassword;
            listener = new HttpListener();
        }

        public void Start()
        {
            if (isRunning) return;

            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
                Console.WriteLine($"[HTTP] Criada pasta: {rootPath}");
            }

            listener.Prefixes.Clear();
            listener.Prefixes.Add($"http://+:{httpPort}/");
            
            if (httpsEnabled)
            {
                if (BindCertificate())
                {
                    listener.Prefixes.Add($"https://+:{httpsPort}/");
                    Console.WriteLine($"[HTTPS] Certificado configurado para porta {httpsPort}");
                }
                else
                {
                    Console.WriteLine($"[HTTPS] Falha ao configurar certificado, apenas HTTP será usado");
                }
            }

            try
            {
                listener.Start();
                isRunning = true;
                Console.WriteLine($"[HTTP] Servidor iniciado na porta {httpPort}");
                if (httpsEnabled && listener.Prefixes.Contains($"https://+:{httpsPort}/"))
                    Console.WriteLine($"[HTTPS] Servidor seguro iniciado na porta {httpsPort}");
                Console.WriteLine($"[HTTP] Servindo arquivos de: {rootPath}");

                Task.Run(() => Listen());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HTTP] Erro ao iniciar servidor: {ex.Message}");
                Console.WriteLine($"[HTTP] Execute como Administrador ou use:");
                Console.WriteLine($"  netsh http add urlacl url=http://+:{httpPort}/ user=Everyone");
                if (httpsEnabled)
                    Console.WriteLine($"  netsh http add urlacl url=https://+:{httpsPort}/ user=Everyone");
            }
        }

        private bool BindCertificate()
        {
            // Verificar se certificado existe e é válido
            if (!string.IsNullOrEmpty(certificatePath) && File.Exists(certificatePath))
            {
                if (ValidateCertificate(certificatePath, certificatePassword))
                {
                    Console.WriteLine($"[HTTPS] Certificado válido encontrado: {certificatePath}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[HTTPS] Certificado inválido ou expirado, gerando novo...");
                }
            }
            else
            {
                Console.WriteLine($"[HTTPS] Certificado não encontrado, gerando automaticamente...");
            }

            // Gerar certificado automaticamente
            return GenerateSelfSignedCertificate();
        }

        private bool ValidateCertificate(string certPath, string password)
        {
            try
            {
                var cert = new X509Certificate2(certPath, password);
                
                // Verificar se está expirado
                if (DateTime.Now < cert.NotBefore || DateTime.Now > cert.NotAfter)
                {
                    Console.WriteLine($"[HTTPS] Certificado expirado (válido de {cert.NotBefore:dd/MM/yyyy} até {cert.NotAfter:dd/MM/yyyy})");
                    return false;
                }
                
                // Verificar se tem a chave privada
                if (!cert.HasPrivateKey)
                {
                    Console.WriteLine($"[HTTPS] Certificado não possui chave privada");
                    return false;
                }
                
                Console.WriteLine($"[HTTPS] Certificado válido até: {cert.NotAfter:dd/MM/yyyy HH:mm}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HTTPS] Erro ao validar certificado: {ex.Message}");
                return false;
            }
        }

        private bool GenerateSelfSignedCertificate()
        {
            try
            {
                Console.WriteLine($"[HTTPS] Gerando certificado auto-assinado...");
                
                // Criar diretório se não existir
                string certDir = Path.GetDirectoryName(certificatePath) ?? "";
                if (!string.IsNullOrEmpty(certDir) && !Directory.Exists(certDir))
                {
                    Directory.CreateDirectory(certDir);
                }
                
                // Comando PowerShell para gerar certificado
                string password = string.IsNullOrEmpty(certificatePassword) ? "DigitalWorld2026" : certificatePassword;
                string psScript = $@"
                    $cert = New-SelfSignedCertificate -DnsName 'localhost', '127.0.0.1', '*.local' `
                        -CertStoreLocation 'cert:\CurrentUser\My' `
                        -FriendlyName 'Digital World HTTPS' `
                        -NotAfter (Get-Date).AddYears(10) `
                        -KeyUsage DigitalSignature, KeyEncipherment `
                        -KeyAlgorithm RSA `
                        -KeyLength 2048;
                    $pwd = ConvertTo-SecureString -String '{password}' -Force -AsPlainText;
                    Export-PfxCertificate -Cert $cert -FilePath '{certificatePath.Replace("\\", "\\\\")}' -Password $pwd | Out-Null;
                    Remove-Item -Path ""cert:\CurrentUser\My\$($cert.Thumbprint)"" -DeleteKey;
                    Write-Host 'OK'
                ";
                
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var process = System.Diagnostics.Process.Start(psi);
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    if (process.ExitCode == 0 && File.Exists(certificatePath))
                    {
                        Console.WriteLine($"[HTTPS] ✓ Certificado gerado: {certificatePath}");
                        Console.WriteLine($"[HTTPS] ✓ Senha: {password}");
                        Console.WriteLine($"[HTTPS] ✓ Válido por 10 anos");
                        
                        // Atualizar senha se estava vazia
                        if (string.IsNullOrEmpty(certificatePassword))
                            this.certificatePassword = password;
                        
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"[HTTPS] ✗ Erro ao gerar certificado: {error}");
                        Console.WriteLine($"[HTTPS] Execute manualmente: C:\\DMOServer\\GenerateCertificate.ps1");
                        return false;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HTTPS] ✗ Erro ao gerar certificado: {ex.Message}");
                Console.WriteLine($"[HTTPS] Execute manualmente: C:\\DMOServer\\GenerateCertificate.ps1");
                return false;
            }
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            listener.Stop();
            Console.WriteLine("[HTTP] Servidor parado");
        }

        private async void Listen()
        {
            while (isRunning)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    Task.Run(() => ProcessRequest(context));
                }
                catch (Exception ex)
                {
                    if (isRunning)
                        Console.WriteLine($"[HTTP] Erro no listener: {ex.Message}");
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                string requestedPath = request.Url.LocalPath.TrimStart('/');
                string filePath = Path.Combine(rootPath, requestedPath);

                // Prevenir path traversal
                if (!Path.GetFullPath(filePath).StartsWith(rootPath))
                {
                    SendError(response, 403, "Forbidden");
                    return;
                }

                if (File.Exists(filePath))
                {
                    byte[] fileData = File.ReadAllBytes(filePath);
                    response.ContentType = GetContentType(filePath);
                    response.ContentLength64 = fileData.Length;
                    response.StatusCode = 200;
                    response.OutputStream.Write(fileData, 0, fileData.Length);

                    Console.WriteLine($"[HTTP] {request.HttpMethod} {requestedPath} - 200 OK ({fileData.Length} bytes)");
                }
                else
                {
                    SendError(response, 404, "File Not Found");
                    Console.WriteLine($"[HTTP] {request.HttpMethod} {requestedPath} - 404 Not Found");
                }

                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HTTP] Erro ao processar requisição: {ex.Message}");
            }
        }

        private void SendError(HttpListenerResponse response, int statusCode, string message)
        {
            response.StatusCode = statusCode;
            byte[] data = System.Text.Encoding.UTF8.GetBytes($"<html><body><h1>{statusCode} - {message}</h1></body></html>");
            response.ContentType = "text/html";
            response.ContentLength64 = data.Length;
            response.OutputStream.Write(data, 0, data.Length);
        }

        private string GetContentType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension switch
            {
                ".bin" => "application/octet-stream",
                ".dat" => "application/octet-stream",
                ".exe" => "application/octet-stream",
                ".zip" => "application/zip",
                ".html" => "text/html",
                ".txt" => "text/plain",
                ".json" => "application/json",
                _ => "application/octet-stream"
            };
        }
    }
}
