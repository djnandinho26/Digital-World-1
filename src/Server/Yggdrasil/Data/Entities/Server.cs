using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Digital_World.Data.Entities
{
    public class Server
    {
        [Key]
        public int ServerId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(45)]
        public string? Ip { get; set; }

        public int Port { get; set; }
    }
}
