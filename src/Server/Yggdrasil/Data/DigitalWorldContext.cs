using Microsoft.EntityFrameworkCore;
using Digital_World.Data.Entities;

namespace Digital_World.Data
{
    /// <summary>
    /// Entity Framework DbContext para Digital World
    /// </summary>
    public class DigitalWorldContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<DigimonEntity> Digimons { get; set; }
        public DbSet<Server> Servers { get; set; }

        public DigitalWorldContext(DbContextOptions<DigitalWorldContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Account Configuration
            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("acct");
                entity.HasKey(e => e.AccountId);
                
                entity.Property(e => e.AccountId).HasColumnName("accountId");
                entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Password).HasColumnName("password").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(100);
                entity.Property(e => e.UniId).HasColumnName("uniId");
                entity.Property(e => e.Char1).HasColumnName("char1");
                entity.Property(e => e.Char2).HasColumnName("char2");
                entity.Property(e => e.Char3).HasColumnName("char3");
                entity.Property(e => e.Char4).HasColumnName("char4");
                entity.Property(e => e.LastChar).HasColumnName("lastChar");
                entity.Property(e => e.Premium).HasColumnName("premium");
                entity.Property(e => e.Cash).HasColumnName("cash");
                entity.Property(e => e.Silk).HasColumnName("silk");

                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Character Configuration
            modelBuilder.Entity<Character>(entity =>
            {
                entity.ToTable("chars");
                entity.HasKey(e => e.CharacterId);
                
                entity.Property(e => e.CharacterId).HasColumnName("characterId");
                entity.Property(e => e.AccountId).HasColumnName("accountId");
                entity.Property(e => e.CharName).HasColumnName("charName").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Model).HasColumnName("model");
                entity.Property(e => e.Level).HasColumnName("level");
                entity.Property(e => e.Partner).HasColumnName("partner");
                entity.Property(e => e.Map).HasColumnName("map");
                entity.Property(e => e.X).HasColumnName("x");
                entity.Property(e => e.Y).HasColumnName("y");
                entity.Property(e => e.Hp).HasColumnName("hp");
                entity.Property(e => e.Ds).HasColumnName("ds");
                entity.Property(e => e.Money).HasColumnName("money");
                entity.Property(e => e.Inventory).HasColumnName("inventory").HasColumnType("blob");
                entity.Property(e => e.Warehouse).HasColumnName("warehouse").HasColumnType("blob");
                entity.Property(e => e.Archive).HasColumnName("archive").HasColumnType("blob");

                entity.HasOne<Account>()
                    .WithMany()
                    .HasForeignKey(e => e.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.CharName).IsUnique();
            });

            // Digimon Configuration
            modelBuilder.Entity<DigimonEntity>(entity =>
            {
                entity.ToTable("digimon");
                entity.HasKey(e => e.DigimonId);
                
                entity.Property(e => e.DigimonId).HasColumnName("digimonId");
                entity.Property(e => e.CharacterId).HasColumnName("characterId");
                entity.Property(e => e.DigiName).HasColumnName("digiName").HasMaxLength(50);
                entity.Property(e => e.DigiModel).HasColumnName("digiModel");
                entity.Property(e => e.Level).HasColumnName("level");
                entity.Property(e => e.Size).HasColumnName("size");
                entity.Property(e => e.Hp).HasColumnName("hp");
                entity.Property(e => e.Ds).HasColumnName("ds");
                entity.Property(e => e.Exp).HasColumnName("exp");
                entity.Property(e => e.DigiSlot).HasColumnName("digiSlot");
                entity.Property(e => e.Evolutions).HasColumnName("evolutions").HasColumnType("blob");

                entity.HasOne<Character>()
                    .WithMany()
                    .HasForeignKey(e => e.CharacterId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Server Configuration
            modelBuilder.Entity<Server>(entity =>
            {
                entity.ToTable("servers");
                entity.HasKey(e => e.ServerId);
                
                entity.Property(e => e.ServerId).HasColumnName("serverId");
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.Ip).HasColumnName("ip").HasMaxLength(45);
                entity.Property(e => e.Port).HasColumnName("port");
            });
        }
    }
}
