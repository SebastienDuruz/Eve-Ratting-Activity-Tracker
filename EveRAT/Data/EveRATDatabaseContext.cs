/// Author : Sébastien Duruz
/// Date : 03.05.2023

using EveRAT.Models;
using Microsoft.EntityFrameworkCore;

namespace EveRAT.Data;

public partial class EveRATDatabaseContext : DbContext
{
    public EveRATDatabaseContext()
    {
    }

    public EveRATDatabaseContext(DbContextOptions<EveRATDatabaseContext> options)
        : base(options)
    {
    }

    public virtual DbSet<History> Histories { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite("Data Source=db/EveRAT.db;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<History>(entity =>
        {
            entity.ToTable("History");

            entity.HasIndex(e => e.HistoryId, "IX_History_HistoryId").IsUnique();
            entity.Property(e => e.HistoryId).ValueGeneratedOnAdd();

            entity.Property(e => e.HistoryAdm).HasColumnName("HistoryADM");
            entity.Property(e => e.HistoryDateTime)
                .HasDefaultValueSql("datetime('now')")
                .HasColumnType("datetime");
            entity.Property(e => e.HistoryNpckills).HasColumnName("HistoryNPCKills");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}