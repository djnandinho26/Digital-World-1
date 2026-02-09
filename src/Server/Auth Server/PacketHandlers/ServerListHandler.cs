using Digital_World.Helpers;
using Digital_World.Packets;

namespace Digital_World.PacketHandlers
{
    /// <summary>
    /// Handler para pacotes de requisição de lista de servidores (Type: 0x6A5 / 1701)
    /// </summary>
    public static class ServerListHandler
    {
        public static void Process(Client client, PacketReader packet, byte[] buffer)
        {
            client.Send(new Packets.Auth.ServerList(SqlDB.GetServersEF(), client.Username, client.Characters));
        }
    }
}
