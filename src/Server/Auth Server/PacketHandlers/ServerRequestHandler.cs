using Digital_World.Helpers;
using Digital_World.Packets;
using System;
using System.Collections.Generic;

namespace Digital_World.PacketHandlers
{
    /// <summary>
    /// Handler para pacotes de requisição de IP do servidor (Type: 1702)
    /// </summary>
    public static class ServerRequestHandler
    {
        public static void Process(Client client, PacketReader packet, byte[] buffer)
        {
            //Requesting IP of Server
            int serverID = BitConverter.ToInt32(buffer, 4);
            KeyValuePair<int, string> server = SqlDB.GetServer(serverID);
            SqlDB.LoadUserEF(client);
            client.Send(new Packets.Auth.ServerIP(server.Value, server.Key, client.AccountID, client.UniqueID));
        }
    }
}
