#define CREATE
using Digital_World.Helpers;
using Digital_World.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Digital_World
{
    public class PacketLogic
    {
        public static void Process(Client client, byte[] buffer)
        {
            PacketReader packet = null;
            try
            {
                packet = new PacketReader(buffer);
            }
            catch
            {
                return;
            }
            switch (packet.Type)
            {
                case -1:
                    {
                        /*
                        PacketWriter resp = new PacketWriter();
                        resp.Type(-2);
                        resp.WriteBytes(new byte[] { 0xcf, 0xa6, 0x8f, 0xd8, 0xb4, 0x4e });
                         * */
                        MultiLogger.LogServer("Accepted connection: {0}", client.m_socket.RemoteEndPoint);

                        packet.Skip(8);
                        ushort u1 = (ushort)packet.ReadShort();
                        ushort u2 = (ushort)packet.ReadShort();

                        client.Send(new Packets.PacketFFEF((short)(client.handshake ^ 0x7e41)));
                        break;
                    }
                case 3301:
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

                        MultiLogger.LogServer("Receiving login request: {0}", username);
#if CREATE
                        SqlDB.CreateUserEF(username, password);
                        MultiLogger.LogServer("Creating user {0}...", username);
#else
                        int success = SqlDB.ValidateEF(client, user, pass);
                        switch (success)
                        {
                            case -1:
                                //Banned or non-existent
                                MultiLogger.LogServer("Banned or nonexistent login: {0}", user);
                                client.Send(new Packets.Auth.LoginMessage(string.Format("This username has been banned.")));
                                break;
                            case -2:
                                //Wrong Pass;
                                Console.Write("Incorrect password: {0}", user);
                                client.Send(new Packets.Auth.LoginMessage("The password provided does not match."));
                                break;
                            case -3:
                                client.Send(new Packets.Auth.LoginMessage("This username does not exist."));
                                break;
                            default:
                                //Normal Login
                                MultiLogger.LogServer("Successful login: {0}\n Sending Server List", user);
                                client.Send(new Packets.Auth.ServerList(SqlDB.GetServersEF(), user, client.Characters));
                                break;
                        }
#endif
                        break;
                    }
                case 1702:
                    {
                        //Requesting IP of Server
                        int serverID = BitConverter.ToInt32(buffer, 4);
                        KeyValuePair<int, string> server = SqlDB.GetServer(serverID);
                        SqlDB.LoadUserEF(client);
                        client.Send(new Packets.Auth.ServerIP(server.Value, server.Key, client.AccountID, client.UniqueID));
                        break;
                    }
                case 0x6A5:
                    {
                        client.Send(new Packets.Auth.ServerList(SqlDB.GetServersEF(), client.Username, client.Characters));
                        break;
                    }
                case -3:
                    break;
                default:
                    {
                        MultiLogger.LogServer("Unknown Packet ID: {0}", packet.Type);
                        MultiLogger.LogServer(Packet.Visualize(buffer));
                        break;
                    }
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
