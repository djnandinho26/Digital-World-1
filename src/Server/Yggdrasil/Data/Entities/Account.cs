using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_World.Data.Entities
{
    public class Account
    {
        [Key]
        public uint AccountId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? SecondPassword { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        public uint? UniId { get; set; }

        public int? Char1 { get; set; }

        public int? Char2 { get; set; }

        public int? Char3 { get; set; }

        public int? Char4 { get; set; }

        public int? LastChar { get; set; }

        public int Premium { get; set; }

        public long Cash { get; set; }

        public int Silk { get; set; }

        public int Level { get; set; } = 1;
    }
}
