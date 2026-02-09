using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Digital_World.Packets.Auth
{
    public class ServerList:Packet, IPacket
    {
        public ServerList(Dictionary<int, string> servers, string user, int characters)
        {
            packet.Type(1701);
            packet.WriteByte((byte)servers.Count);
            foreach(KeyValuePair<int, string> server in servers)
            {
                packet.WriteInt(server.Key);
                packet.WriteString(server.Value);
                packet.WriteByte(0); //Manutenance
                packet.WriteByte(0); //OverLoad
                packet.WriteByte((byte)characters); //Characters Quantidade
                packet.WriteByte(0); //Isso e Novo Mensagem de NEW
                packet.WriteByte(5); //Slots Disponiveis
                packet.WriteByte(5); //Slots Abertos
            }
            packet.WriteString(user);
        }
    }
}
