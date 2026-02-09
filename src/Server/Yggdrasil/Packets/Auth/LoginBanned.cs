using System;
using System.Collections.Generic;
using System.Text;

namespace Digital_World.Packets.Auth
{
    public class LoginBanned : Packet, IPacket
    {
        public LoginBanned(uint RemainingTimeInSeconds, string Reason)
        {
            packet.Type(3308);
            packet.WriteUInt(RemainingTimeInSeconds);
            packet.WriteString(Reason);
        }
    }
}
