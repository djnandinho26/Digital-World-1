#define CREATE
using Digital_World.Helpers;
using Digital_World.Packets;
using System;
using System.IO;
using System.Text;

namespace Digital_World.PacketHandlers
{
    /// <summary>
    /// Handler para pacotes de Login (Type: 3301)
    /// </summary>
    public static class LoginHandler
    {
        public static void Process(Client client, PacketReader packet, byte[] buffer)
        {
            // Lê diretamente os dados do payload sem criar AuthenticationPacketReader
            var g_nNetVersion = BitConverter.ToUInt32(packet.ReadBytes(4), 0);
            var GetUserType = ExtractString(packet);
            var username = ExtractString(packet);
            var password = ExtractString(packet);
            var szCpuName = ExtractString(packet);
            var szGpuName = ExtractString(packet);
            var nPhyMemory = BitConverter.ToInt32(packet.ReadBytes(4), 0) / 1024;
            var szOS = ExtractString(packet);
            var szDxVersion = ExtractString(packet);

            MultiLogger.LogServer($"Recebendo requisição de login: {username}");
#if CREATE
            SqlDB.CreateUserEF(username, password);
            MultiLogger.LogServer($"Criando usuário {username}...");
#endif
            int success = SqlDB.ValidateEF(client, username, password);
            switch (success)
            {
                case -1:
                    //Banned or non-existent
                    MultiLogger.LogServer($"Login banido ou inexistente: {username}");
                    client.Send(new Packets.Auth.LoginBanned(uint.MaxValue,string.Format("This username has been banned.")));
                    break;
                case -2:
                    //Wrong Pass;
                    MultiLogger.LogServer($"Senha incorreta: {username}");
                    client.Send(new Packets.Auth.LoginRequest(10057,0));
                    break;
                case -3:
                    client.Send(new Packets.Auth.LoginRequest(10035,0));
                    break;
                default:
                    //Normal Login
                    MultiLogger.LogServer($"Login bem-sucedido: {username}\n Enviando Lista de Servidores");
                    client.Send(new Packets.Auth.LoginRequest(0,3));
                    break;
            }

        }

        public static string ExtractString(PacketReader packet) => ExtractData(packet);

        /// <summary>
        /// Extrai uma string do pacote de autenticação, lidando com o formato específico dos dados.
        /// </summary>
        /// <param name="packet">O pacote de autenticação a ser processado</param>
        /// <returns>A string extraída do pacote</returns>
        /// <exception cref="InvalidDataException">Lançada quando o tamanho da string é inválido</exception>
        /// <exception cref="InvalidOperationException">Lançada quando ocorre um erro ao extrair os dados</exception>
        private static string ExtractData(PacketReader packet)
        {
            ArgumentNullException.ThrowIfNull(packet, nameof(packet));

            try
            {
                // Lê o tamanho da string (o primeiro byte indica o tamanho)
                int size = packet.ReadByte();

                // Verifica se o tamanho é válido
                if (size < 0)
                    throw new InvalidDataException("Valor do tamanho inválido: não pode ser negativo");

                // Caso o tamanho seja zero, retorna string vazia
                if (size == 0)
                    return string.Empty;

                // Lê os bytes da string (tamanho + 1 para incluir o byte nulo de terminação)
                byte[] stringBytes = packet.ReadBytes(size + 1);

                // Converte os bytes para string, removendo caracteres nulos no final
                return Encoding.ASCII.GetString(stringBytes).TrimEnd('\0');
            }
            catch (Exception ex) when (ex is not InvalidDataException && ex is not ArgumentNullException)
            {
                // Captura e relança exceções com informações de contexto adicionais
                throw new InvalidOperationException("Falha ao extrair dados do pacote de autenticação", ex);
            }
        }
    }
}
