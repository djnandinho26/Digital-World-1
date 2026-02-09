using System;
using System.Collections.Generic;
using System.Text;

namespace Digital_World.Packets.Auth;
public class LoginRequest : Packet, IPacket
{
    public LoginRequest(int Result, int Subtype)
    {
        packet.Type(3301);
        packet.WriteInt(Result);
        packet.WriteByte((byte)Subtype);
    }
}
