using MessagePack;

namespace Guwahak.Network.Packet
{
    [MessagePackObject]
    [Serializable]
    public class Packet
    {
        [Key(100)] public Ulid Index { get; set; }
        [Key(101)] public Ulid AnsTo { get; set; }
        [Key(102)] public string Order { get; set; }

        public Packet()
        {
            Index = Ulid.NewUlid();
            AnsTo = Ulid.Empty;
            Order = GetType().ToString();
        }
    }
}
