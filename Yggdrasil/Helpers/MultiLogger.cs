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
        private TextBox authOutput;
        private TextBox webOutput;
        private StringBuilder currentLine = new StringBuilder();
        
        public MultiLogger(TextBox authTextBox, TextBox webTextBox)
        {
            authOutput = authTextBox;
            webOutput = webTextBox;
            Console.SetOut(this);
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
            // Identificar mensagens do servidor HTTP/HTTPS/FTP
            string[] webKeywords = new string[]
            {
                "HTTP",
                "HTTPS",
                "FTP",
                "SSL",
                "TLS",
                "Certificate",
                "Certificado",
                "[WEB]",
                "[FTP]",
                "patch",
                "download",
                "upload",
                "GET ",
                "POST ",
                "PUT ",
                "DELETE ",
                "HEAD ",
                "OPTIONS ",
                "RETR",
                "STOR",
                "LIST",
                "USER",
                "PASS",
                "PWD",
                "CWD",
                "QUIT",
                "TYPE",
                "PORT",
                "PASV",
                "Server is now listening",
                "servidor HTTP",
                "servidor HTTPS",
                "servidor FTP",
                "Servidor escutando",
                "Client connected",
                "Cliente conectado",
                "Request from",
                "Requisição de"
            };

            string upperMessage = message.ToUpper();
            
            return webKeywords.Any(keyword => 
                upperMessage.Contains(keyword.ToUpper()));
        }

        public override Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }
    }
}
