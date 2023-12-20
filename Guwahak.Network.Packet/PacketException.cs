using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace Guwahak.Network.Packet
{
    public class PacketException : Packet
    {
        [Key(0)] public Exception Exception { get; set; }
        
        public PacketException(Exception e)
        {
            Exception = e;
        }
    }
}
