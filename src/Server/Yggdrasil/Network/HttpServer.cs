using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Digital_World.Helpers;

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
        private string certificateType;

        public bool IsRunning => isRunning;

        public HttpServer(string rootPath, int httpPort = 8080, int httpsPort = 8443, 
                         bool httpsEnabled = false, string certificatePath = "", string certificatePassword = "", string certificateType = "Auto")
        {
            this.rootPath = Path.GetFullPath(rootPath);
            this.httpPort = httpPort;
            this.httpsPort = httpsPort;
            this.httpsEnabled = httpsEnabled;
            this.certificatePath = certificatePath;
            this.certificatePassword = certificatePassword;
            this.certificateType = certificateType;
            listener = new HttpListener();
        }

        public void Start()
        {
            if (isRunning) return;

            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
                MultiLogger.LogWeb($"[HTTP] Criada pasta: {rootPath}");
            }

            listener.Prefixes.Clear();
            listener.Prefixes.Add($"http://+:{httpPort}/");
            
            if (httpsEnabled)
            {
                if (BindCertificate())
                {
                    listener.Prefixes.Add($"https://+:{httpsPort}/");
                    MultiLogger.LogWeb($"[HTTPS] Certificado configurado para porta {httpsPort}");
                }
                else
                {
                    MultiLogger.LogWeb($"[HTTPS] Falha ao configurar certificado, apenas HTTP será usado");
                }
            }

            try
            {
                listener.Start();
                isRunning = true;
                MultiLogger.LogWeb($"[HTTP] Servidor iniciado na porta {httpPort}");
                if (httpsEnabled && listener.Prefixes.Contains($"https://+:{httpsPort}/"))
                    MultiLogger.LogWeb($"[HTTPS] Servidor seguro iniciado na porta {httpsPort}");
                MultiLogger.LogWeb($"[HTTP] Servindo arquivos de: {rootPath}");

                Task.Run(() => Listen());
            }
            catch (Exception ex)
            {
                MultiLogger.LogWeb($"[HTTP] Erro ao iniciar servidor: {ex.Message}");
                MultiLogger.LogWeb($"[HTTP] Execute como Administrador ou use:");
                MultiLogger.LogWeb($"  netsh http add urlacl url=http://+:{httpPort}/ user=Everyone");
                if (httpsEnabled)
                    MultiLogger.LogWeb($"  netsh http add urlacl url=https://+:{httpsPort}/ user=Everyone");
            }
        }

        private bool BindCertificate()
        {
            string certDir = Path.GetDirectoryName(certificatePath) ?? "";
            string zeroSslCert = Path.Combine(certDir, "certificate.crt");
            string zeroSslCaBundle = Path.Combine(certDir, "ca_bundle.crt");
            string zeroSslPrivateKey = Path.Combine(certDir, "private.key");

            // Obter tipo de certificado da configuração
            MultiLogger.LogWeb($"[HTTPS] Tipo de certificado configurado: {certificateType}");

            // Se escolheu ZeroSSL, processar certificados ZeroSSL
            if (certificateType == "ZeroSSL")
            {
                if (File.Exists(zeroSslCert) && File.Exists(zeroSslPrivateKey))
                {
                    MultiLogger.LogWeb($"[HTTPS] Certificados ZeroSSL detectados, processando...");
                    if (ConvertZeroSSLToPfx(zeroSslCert, zeroSslCaBundle, zeroSslPrivateKey))
                    {
                        MultiLogger.LogWeb($"[HTTPS] ✓ Certificado ZeroSSL convertido para PFX");
                    }
                }
                else
                {
                    MultiLogger.LogWeb($"[HTTPS] ============================================");
                    MultiLogger.LogWeb($"[HTTPS] CERTIFICADO ZEROSSSL NÃO ENCONTRADO!");
                    MultiLogger.LogWeb($"[HTTPS] ============================================");
                    MultiLogger.LogWeb($"[HTTPS] Por favor, copie os arquivos do ZeroSSL para:");
                    MultiLogger.LogWeb($"[HTTPS]   {certDir}");
                    MultiLogger.LogWeb($"[HTTPS] ");
                    MultiLogger.LogWeb($"[HTTPS] Arquivos necessários:");
                    MultiLogger.LogWeb($"[HTTPS]   - certificate.crt (certificado do domínio)");
                    MultiLogger.LogWeb($"[HTTPS]   - ca_bundle.crt (cadeia CA)");
                    MultiLogger.LogWeb($"[HTTPS]   - private.key (chave privada)");
                    MultiLogger.LogWeb($"[HTTPS] ");
                    MultiLogger.LogWeb($"[HTTPS] Como obter certificado gratuito ZeroSSL:");
                    MultiLogger.LogWeb($"[HTTPS]   1. Acesse https://zerossl.com");
                    MultiLogger.LogWeb($"[HTTPS]   2. Crie uma conta gratuita");
                    MultiLogger.LogWeb($"[HTTPS]   3. Gere certificado para seu domínio (90 dias)");
                    MultiLogger.LogWeb($"[HTTPS]   4. Baixe os 3 arquivos");
                    MultiLogger.LogWeb($"[HTTPS]   5. Copie para a pasta cert\\");
                    MultiLogger.LogWeb($"[HTTPS]   6. Reinicie o servidor");
                    MultiLogger.LogWeb($"[HTTPS] ============================================");
                    return false;
                }
            }

            // Verificar se certificado PFX existe
            if (!string.IsNullOrEmpty(certificatePath) && File.Exists(certificatePath))
            {
                var validationResult = ValidateCertificate(certificatePath, certificatePassword);
                
                if (validationResult == CertificateValidation.Valid)
                {
                    MultiLogger.LogWeb($"[HTTPS] ✓ Certificado válido encontrado: {certificatePath}");
                    return BindCertificateToPort();
                }
                else if (validationResult == CertificateValidation.Expired)
                {
                    MultiLogger.LogWeb($"[HTTPS] ============================================");
                    MultiLogger.LogWeb($"[HTTPS] CERTIFICADO VENCIDO!");
                    MultiLogger.LogWeb($"[HTTPS] ============================================");
                    MultiLogger.LogWeb($"[HTTPS] Deletando certificado vencido: {certificatePath}");
                    
                    try
                    {
                        File.Delete(certificatePath);
                        MultiLogger.LogWeb($"[HTTPS] ✓ Certificado vencido deletado");
                    }
                    catch (Exception ex)
                    {
                        MultiLogger.LogWeb($"[HTTPS] ✗ Erro ao deletar certificado: {ex.Message}");
                    }

                    if (certificateType == "ZeroSSL")
                    {
                        MultiLogger.LogWeb($"[HTTPS] ");
                        MultiLogger.LogWeb($"[HTTPS] AÇÃO NECESSÁRIA:");
                        MultiLogger.LogWeb($"[HTTPS] Por favor, obtenha um NOVO certificado ZeroSSL:");
                        MultiLogger.LogWeb($"[HTTPS]   1. Acesse https://zerossl.com");
                        MultiLogger.LogWeb($"[HTTPS]   2. Renove ou crie novo certificado (90 dias)");
                        MultiLogger.LogWeb($"[HTTPS]   3. Baixe os 3 arquivos (certificate.crt, ca_bundle.crt, private.key)");
                        MultiLogger.LogWeb($"[HTTPS]   4. Copie para: {certDir}");
                        MultiLogger.LogWeb($"[HTTPS]   5. Reinicie o servidor");
                        MultiLogger.LogWeb($"[HTTPS] ============================================");
                        return false;
                    }
                    else
                    {
                        MultiLogger.LogWeb($"[HTTPS] Gerando novo certificado auto-assinado...");
                    }
                }
                else
                {
                    MultiLogger.LogWeb($"[HTTPS] ✗ Certificado inválido: {certificatePath}");
                }
            }

            // Gerar certificado auto-assinado se configurado ou se não houver certificado válido
            if (certificateType == "Auto" || certificateType != "ZeroSSL")
            {
                MultiLogger.LogWeb($"[HTTPS] Gerando certificado auto-assinado...");
                if (GenerateSelfSignedCertificate())
                {
                    return BindCertificateToPort();
                }
            }
            
            return false;
        }

        private enum CertificateValidation
        {
            Valid,
            Expired,
            Invalid
        }

        private CertificateValidation ValidateCertificate(string certPath, string password)
        {
            try
            {
                var cert = new X509Certificate2(certPath, password);
                
                // Verificar se está expirado
                if (DateTime.Now < cert.NotBefore || DateTime.Now > cert.NotAfter)
                {
                    MultiLogger.LogWeb($"[HTTPS] ✗ Certificado expirado!");
                    MultiLogger.LogWeb($"[HTTPS]   Válido de: {cert.NotBefore:dd/MM/yyyy HH:mm:ss}");
                    MultiLogger.LogWeb($"[HTTPS]   Válido até: {cert.NotAfter:dd/MM/yyyy HH:mm:ss}");
                    MultiLogger.LogWeb($"[HTTPS]   Data atual: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                    return CertificateValidation.Expired;
                }
                
                // Verificar se tem a chave privada
                if (!cert.HasPrivateKey)
                {
                    MultiLogger.LogWeb($"[HTTPS] Certificado não possui chave privada");
                    return CertificateValidation.Invalid;
                }
                
                // Mostrar informações do certificado
                MultiLogger.LogWeb($"[HTTPS] Certificado: {cert.Subject}");
                MultiLogger.LogWeb($"[HTTPS] Emissor: {cert.Issuer}");
                MultiLogger.LogWeb($"[HTTPS] Válido de {cert.NotBefore:dd/MM/yyyy} até {cert.NotAfter:dd/MM/yyyy}");
                
                // Calcular dias restantes
                int daysRemaining = (cert.NotAfter - DateTime.Now).Days;
                if (daysRemaining <= 30)
                {
                    MultiLogger.LogWeb($"[HTTPS] ⚠ AVISO: Certificado expira em {daysRemaining} dias!");
                    if (cert.Issuer.Contains("ZeroSSL") || cert.Issuer.Contains("Let's Encrypt"))
                    {
                        MultiLogger.LogWeb($"[HTTPS]   Renove em: https://zerossl.com");
                    }
                }
                else
                {
                    MultiLogger.LogWeb($"[HTTPS] ✓ Certificado válido por mais {daysRemaining} dias");
                }
                
                // Identificar tipo de certificado
                string issuer = cert.Issuer;
                string subject = cert.Subject;
                bool isSelfSigned = issuer.Equals(subject, StringComparison.OrdinalIgnoreCase);
                
                if (isSelfSigned)
                {
                    MultiLogger.LogWeb($"[HTTPS] Tipo: Certificado auto-assinado");
                }
                else
                {
                    MultiLogger.LogWeb($"[HTTPS] Tipo: Certificado CA (confiável)");
                    MultiLogger.LogWeb($"[HTTPS] Emissor: {GetCertificateIssuerName(issuer)}");
                }
                
                return CertificateValidation.Valid;
            }
            catch (Exception ex)
            {
                MultiLogger.LogWeb($"[HTTPS] Erro ao validar certificado: {ex.Message}");
                return CertificateValidation.Invalid;
            }
        }

        private string GetCertificateIssuerName(string issuer)
        {
            // Extrair o CN (Common Name) do emissor
            var parts = issuer.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed.Substring(3);
                }
            }
            return issuer;
        }

        private string GetCertificateSubjectName(string subject)
        {
            // Extrair o CN (Common Name) do subject
            var parts = subject.Split(',');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed.Substring(3);
                }
            }
            return subject;
        }
        private bool BindCertificateToPort()
        {
            try
            {
                // Carregar certificado do arquivo
                string password = string.IsNullOrEmpty(certificatePassword) ? "DigitalWorld2026" : certificatePassword;
                var cert = new X509Certificate2(certificatePath, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                
                // Importar certificado para o Windows Certificate Store
                MultiLogger.LogWeb($"[HTTPS] Importando certificado para o Windows Certificate Store...");
                using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.ReadWrite);
                    
                    // Remover certificado antigo com mesmo thumbprint se existir
                    var oldCerts = store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, false);
                    foreach (var oldCert in oldCerts)
                    {
                        store.Remove(oldCert);
                    }
                    
                    // Adicionar novo certificado
                    store.Add(cert);
                    store.Close();
                    MultiLogger.LogWeb($"[HTTPS] ✓ Certificado importado com sucesso");
                }
                
                // Obter thumbprint do certificado
                string thumbprint = cert.Thumbprint;
                string appId = "{00112233-4455-6677-8899-AABBCCDDEEFF}"; // GUID fixo para a aplicação
                
                MultiLogger.LogWeb($"[HTTPS] Fazendo binding do certificado (Thumbprint: {thumbprint})...");
                
                // Remover binding anterior se existir
                var deleteCmd = $"netsh http delete sslcert ipport=0.0.0.0:{httpsPort}";
                RunCommand(deleteCmd, ignoreErrors: true);
                
                // Adicionar novo binding
                var addCmd = $"netsh http add sslcert ipport=0.0.0.0:{httpsPort} certhash={thumbprint} appid={appId}";
                if (RunCommand(addCmd))
                {
                    MultiLogger.LogWeb($"[HTTPS] ✓ Certificado associado à porta {httpsPort}");
                    return true;
                }
                else
                {
                    MultiLogger.LogWeb($"[HTTPS] ✗ Falha ao associar certificado");
                    return false;
                }
            }
            catch (Exception ex)
            {
                MultiLogger.LogWeb($"[HTTPS] Erro ao fazer binding: {ex.Message}");
                return false;
            }
        }

        private bool ConvertZeroSSLToPfx(string certFile, string caBundle, string privateKeyFile)
        {
            try
            {
                MultiLogger.LogWeb($"[HTTPS] Convertendo certificados ZeroSSL para formato PFX...");
                
                // Ler certificado
                string certPem = File.ReadAllText(certFile);
                
                // Ler CA bundle se existir
                string caBundlePem = "";
                if (File.Exists(caBundle))
                {
                    caBundlePem = File.ReadAllText(caBundle);
                    MultiLogger.LogWeb($"[HTTPS] CA Bundle encontrado: {Path.GetFileName(caBundle)}");
                }
                
                // Ler chave privada
                string privateKeyPem = File.ReadAllText(privateKeyFile);
                
                // Combinar certificado + CA bundle + chave privada em um único PEM
                string combinedPem = certPem + "\n" + caBundlePem + "\n" + privateKeyPem;
                
                // Salvar PEM combinado temporariamente
                string tempPemFile = Path.Combine(Path.GetDirectoryName(certificatePath) ?? "", "temp_combined.pem");
                File.WriteAllText(tempPemFile, combinedPem);
                
                // Converter PEM para PFX usando OpenSSL via PowerShell
                string password = string.IsNullOrEmpty(certificatePassword) ? "DigitalWorld2026" : certificatePassword;
                string psScript = $@"
                    # Tentar usar OpenSSL se disponível
                    $opensslPath = (Get-Command openssl -ErrorAction SilentlyContinue).Source
                    if ($opensslPath) {{
                        openssl pkcs12 -export -out '{certificatePath}' -in '{tempPemFile}' -password pass:{password}
                        exit $LASTEXITCODE
                    }}
                    
                    # Fallback: usar .NET para converter
                    $cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::CreateFromPemFile('{certFile}', '{privateKeyFile}')
                    $certBytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, '{password}')
                    [System.IO.File]::WriteAllBytes('{certificatePath}', $certBytes)
                    exit 0
                ";
                
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var process = Process.Start(psi);
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    // Limpar arquivo temporário
                    if (File.Exists(tempPemFile))
                        File.Delete(tempPemFile);
                    
                    if (process.ExitCode == 0 && File.Exists(certificatePath))
                    {
                        MultiLogger.LogWeb($"[HTTPS] ✓ Certificado ZeroSSL convertido com sucesso");
                        
                        // Atualizar senha se estava vazia
                        if (string.IsNullOrEmpty(certificatePassword))
                            this.certificatePassword = password;
                        
                        return true;
                    }
                    else
                    {
                        MultiLogger.LogWeb($"[HTTPS] ✗ Erro ao converter certificado ZeroSSL");
                        if (!string.IsNullOrWhiteSpace(error))
                            MultiLogger.LogWeb($"[HTTPS] {error}");
                        return false;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                MultiLogger.LogWeb($"[HTTPS] Erro ao converter certificados ZeroSSL: {ex.Message}");
                return false;
            }
        }

        private bool RunCommand(string command, bool ignoreErrors = false)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var process = Process.Start(psi);
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    if (process.ExitCode != 0 && !ignoreErrors)
                    {
                        if (!string.IsNullOrWhiteSpace(error))
                            MultiLogger.LogWeb($"[HTTPS] Erro no comando: {error.Trim()}");
                        if (!string.IsNullOrWhiteSpace(output))
                            MultiLogger.LogWeb($"[HTTPS] Output: {output.Trim()}");
                        return false;
                    }
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                if (!ignoreErrors)
                    MultiLogger.LogWeb($"[HTTPS] Erro ao executar comando: {ex.Message}");
                return false;
            }
        }
        private bool GenerateSelfSignedCertificate()
        {
            try
            {
                MultiLogger.LogWeb($"[HTTPS] Gerando certificado auto-assinado...");
                
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
                        -NotAfter (Get-Date).AddDays(1) `
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
                        MultiLogger.LogWeb($"[HTTPS] ✓ Certificado gerado: {certificatePath}");
                        MultiLogger.LogWeb($"[HTTPS] ✓ Senha: {password}");
                        MultiLogger.LogWeb($"[HTTPS] ✓ Válido por 1 dia (apenas para testes)");
                        
                        // Atualizar senha se estava vazia
                        if (string.IsNullOrEmpty(certificatePassword))
                            this.certificatePassword = password;
                        
                        return true;
                    }
                    else
                    {
                        MultiLogger.LogWeb($"[HTTPS] ✗ Erro ao gerar certificado: {error}");
                        MultiLogger.LogWeb($"[HTTPS] Execute manualmente: C:\\DMOServer\\GenerateCertificate.ps1");
                        return false;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                MultiLogger.LogWeb($"[HTTPS] ✗ Erro ao gerar certificado: {ex.Message}");
                MultiLogger.LogWeb($"[HTTPS] Execute manualmente: C:\\DMOServer\\GenerateCertificate.ps1");
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
                        MultiLogger.LogWeb($"[HTTP] Erro no listener: {ex.Message}");
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

                    MultiLogger.LogWeb($"[HTTP] {request.HttpMethod} {requestedPath} - 200 OK ({fileData.Length} bytes)");
                }
                else
                {
                    SendError(response, 404, "File Not Found");
                    MultiLogger.LogWeb($"[HTTP] {request.HttpMethod} {requestedPath} - 404 Not Found");
                }

                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                MultiLogger.LogWeb($"[HTTP] Erro ao processar requisição: {ex.Message}");
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

