using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_World.Data.Entities
{
    public class DigimonEntity
    {
        [Key]
        public int DigimonId { get; set; }

        public int? CharacterId { get; set; }

        [MaxLength(50)]
        public string? DigiName { get; set; }

        public uint DigiModel { get; set; }

        public byte Level { get; set; }

        public ushort Size { get; set; }

        public int Hp { get; set; }

        public int Ds { get; set; }

        public long Exp { get; set; }

        public byte DigiSlot { get; set; }

        public byte[]? Evolutions { get; set; }
    }
}
