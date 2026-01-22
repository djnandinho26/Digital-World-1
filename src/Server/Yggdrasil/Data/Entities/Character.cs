using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_World.Data.Entities
{
    public class Character
    {
        [Key]
        public int CharacterId { get; set; }

        public uint AccountId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CharName { get; set; } = string.Empty;

        public ushort Model { get; set; }

        public byte Level { get; set; }

        public int? Partner { get; set; }

        public uint Map { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int Hp { get; set; }

        public int Ds { get; set; }

        public long Money { get; set; }

        public byte[]? Inventory { get; set; }

        public byte[]? Warehouse { get; set; }

        public byte[]? Archive { get; set; }
    }
}
