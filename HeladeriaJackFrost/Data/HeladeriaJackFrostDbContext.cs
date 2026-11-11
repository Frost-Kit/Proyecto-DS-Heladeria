using HeladeriaJackFrost.Models;
using Microsoft.EntityFrameworkCore;

namespace HeladeriaJackFrost.Data;

public class HeladeriaJackFrostDbContext : DbContext
{
    public HeladeriaJackFrostDbContext(DbContextOptions<HeladeriaJackFrostDbContext> options) : base(options) { }

    public DbSet<Proveedor> Proveedores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Para la tabla proveedor
        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.ToTable("Proveedor");
            entity.HasKey(p => p.IdProveedor);
            entity.Property(p => p.IdProveedor).HasColumnName("ID_Proveedor").UseIdentityAlwaysColumn();
            entity.Property(p => p.Nombre).HasColumnName("Nombre").HasMaxLength(100).IsRequired();
            entity.Property(p => p.Empresa).HasColumnName("Empresa").HasMaxLength(100).IsRequired();
            entity.Property(p => p.Telefono).HasColumnName("Telefono").HasMaxLength(20);
            entity.Property(p => p.Correo).HasColumnName("Correo").HasMaxLength(100);
        });
    }
}