using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.IO;

namespace Digital_World.Helpers
{
    /// <summary>
    /// Logger que direciona mensagens para diferentes TextBox baseado em prefixos
    /// </summary>
    public class MultiLogger : TextWriter
    {
        private static MultiLogger? instance;
        private TextBox authOutput;
        private TextBox webOutput;
        private StringBuilder currentLine = new StringBuilder();
        
        public MultiLogger(TextBox authTextBox, TextBox webTextBox)
        {
            authOutput = authTextBox;
            webOutput = webTextBox;
            instance = this;
            Console.SetOut(this);
        }

        /// <summary>
        /// Registra mensagens de logs dos servidores (Auth, Lobby, Game) - sempre vai para o painel esquerdo
        /// </summary>
        public static void LogServer(string format, params object[] args)
        {
            string message = args.Length > 0 ? string.Format(format, args) : format;
            instance?.WriteToAuth(message);
        }

        /// <summary>
        /// Registra mensagens de HTTP/HTTPS/FTP (sempre vai para o painel direito)
        /// </summary>
        public static void LogWeb(string format, params object[] args)
        {
            string message = args.Length > 0 ? string.Format(format, args) : format;
            instance?.WriteToWeb(message);
        }

        public override void Write(char value)
        {
            if (value == '\n')
            {
                string line = currentLine.ToString();
                RouteMessage(line);
                currentLine.Clear();
            }
            else if (value != '\r')
            {
                currentLine.Append(value);
            }
        }

        public override void Write(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            
            // Se contém quebra de linha, processar linha por linha
            if (value.Contains('\n'))
            {
                string[] lines = value.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (i > 0)
                        currentLine.Clear();
                    
                    currentLine.Append(lines[i].TrimEnd('\r'));
                    
                    if (i < lines.Length - 1)
                    {
                        RouteMessage(currentLine.ToString());
                        currentLine.Clear();
                    }
                }
            }
            else
            {
                currentLine.Append(value);
            }
        }

        public override void WriteLine(string value)
        {
            currentLine.Append(value);
            RouteMessage(currentLine.ToString());
            currentLine.Clear();
        }

        private void RouteMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            TextBox targetTextBox;
            
            // Determinar para qual TextBox enviar baseado no conteúdo
            if (IsWebServerMessage(message))
            {
                targetTextBox = webOutput;
            }
            else
            {
                targetTextBox = authOutput;
            }

            // Adicionar timestamp se a mensagem ainda não tiver
            string formattedMessage = message;
            if (!message.StartsWith("[") && !message.Contains(DateTime.Now.ToString("HH:mm:ss")))
            {
                formattedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            }

            // Enviar para o TextBox apropriado
            targetTextBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                targetTextBox.AppendText(formattedMessage + Environment.NewLine);
                targetTextBox.ScrollToEnd();
            }));
        }

        private bool IsWebServerMessage(string message)
        {
            string upperMessage = message.ToUpper();
            
            // Prioridade 1: Prefixos específicos (maior prioridade)
            if (upperMessage.Contains("[HTTP]") || 
                upperMessage.Contains("[HTTPS]") || 
                upperMessage.Contains("[FTP]"))
            {
                return true;
            }
            
            // Prioridade 2: Palavras-chave específicas de web/ftp
            string[] webKeywords = new string[]
            {
                "SSL",
                "TLS",
                "CERTIFICATE",
                "CERTIFICADO",
                "THUMBPRINT",
                "BINDING",
                "PFX",
                "GET ",
                "POST ",
                "PUT ",
                "DELETE ",
                "HEAD ",
                "OPTIONS ",
                "RETR",
                "STOR",
                "WWW",
                "PATCH",
                "DOWNLOAD",
                "UPLOAD"
            };
            
            return webKeywords.Any(keyword => 
                upperMessage.Contains(keyword));
        }

        private void WriteToAuth(string message)
        {
            if (authOutput.Dispatcher.CheckAccess())
            {
                authOutput.AppendText(message + Environment.NewLine);
                authOutput.ScrollToEnd();
            }
            else
            {
                authOutput.Dispatcher.Invoke(() =>
                {
                    authOutput.AppendText(message + Environment.NewLine);
                    authOutput.ScrollToEnd();
                });
            }
        }

        private void WriteToWeb(string message)
        {
            if (webOutput.Dispatcher.CheckAccess())
            {
                webOutput.AppendText(message + Environment.NewLine);
                webOutput.ScrollToEnd();
            }
            else
            {
                webOutput.Dispatcher.Invoke(() =>
                {
                    webOutput.AppendText(message + Environment.NewLine);
                    webOutput.ScrollToEnd();
                });
            }
        }

        public override Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }
    }
}
