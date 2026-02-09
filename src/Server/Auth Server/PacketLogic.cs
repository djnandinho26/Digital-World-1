#define CREATE
using Digital_World.Helpers;
using Digital_World.Packets;
using Digital_World.PacketHandlers;
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
                    HandshakeHandler.Process(client, packet, buffer);
                    break;

                case -3:
                    break; 

                case 3301:
                    LoginHandler.Process(client, packet, buffer);
                    break;

                case 1701:
                    ServerListHandler.Process(client, packet, buffer);
                    break;
                case 1702:
                    ServerRequestHandler.Process(client, packet, buffer);
                    break;
                    
                default:
                    MultiLogger.LogServer("ID de Pacote Desconhecido: {0}", packet.Type);
                    MultiLogger.LogServer(Packet.Visualize(buffer));
                    break;
            }
        }
    }
}
