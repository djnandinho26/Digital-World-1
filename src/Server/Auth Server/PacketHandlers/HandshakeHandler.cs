using Digital_World.Helpers;
using Digital_World.Packets;

namespace Digital_World.PacketHandlers
{
    /// <summary>
    /// Handler para pacotes de Handshake (Type: -1)
    /// </summary>
    public static class HandshakeHandler
    {
        public static void Process(Client client, PacketReader packet, byte[] buffer)
        {
            /*
            PacketWriter resp = new PacketWriter();
            resp.Type(-2);
            resp.WriteBytes(new byte[] { 0xcf, 0xa6, 0x8f, 0xd8, 0xb4, 0x4e });
             * */
            MultiLogger.LogServer("Conexão aceita: {0}", client.m_socket.RemoteEndPoint);

            packet.Skip(8);
            ushort u1 = (ushort)packet.ReadShort();
            ushort u2 = (ushort)packet.ReadShort();

            client.Send(new Packets.PacketFFEF((short)(client.handshake ^ 0x7e41)));
        }
    }
}
