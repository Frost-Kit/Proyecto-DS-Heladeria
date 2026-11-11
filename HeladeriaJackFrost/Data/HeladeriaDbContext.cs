using System;
using System.Collections.Generic;
using HeladeriaJackFrost.Models;
using Microsoft.EntityFrameworkCore;

namespace HeladeriaJackFrost.Data;

public partial class HeladeriaDbContext : DbContext
{
    public HeladeriaDbContext(DbContextOptions<HeladeriaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Categoria> Categoria { get; set; }

    public virtual DbSet<Cliente> Cliente { get; set; }

    public virtual DbSet<Compra> Compra { get; set; }

    public virtual DbSet<Detalle> Detalle { get; set; }

    public virtual DbSet<Persona> Persona { get; set; }

    public virtual DbSet<Personal> Personal { get; set; }

    public virtual DbSet<Producto> Producto { get; set; }

    public virtual DbSet<Proveedor> Proveedor { get; set; }

    public virtual DbSet<Venta> Venta { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.IdCategoria).HasName("categoria_pkey");

            entity.ToTable("categoria");

            entity.HasIndex(e => e.Nombre, "categoria_nombre_key").IsUnique();

            entity.Property(e => e.IdCategoria).HasColumnName("id_categoria");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.IdCliente).HasName("cliente_pkey");

            entity.ToTable("cliente");

            entity.HasIndex(e => e.IdPersona, "cliente_id_persona_key").IsUnique();

            entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
            entity.Property(e => e.IdPersona).HasColumnName("id_persona");
            entity.Property(e => e.PuntosDeCliente).HasColumnName("puntos_de_cliente");

            entity.HasOne(d => d.IdPersonaNavigation).WithOne(p => p.Cliente)
                .HasForeignKey<Cliente>(d => d.IdPersona)
                .HasConstraintName("fk_cliente_persona");
        });

        modelBuilder.Entity<Compra>(entity =>
        {
            entity.HasKey(e => e.IdCompra).HasName("compra_pkey");

            entity.ToTable("compra");

            entity.Property(e => e.IdCompra).HasColumnName("id_compra");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.CostoUnitario)
                .HasPrecision(10, 2)
                .HasColumnName("costo_unitario");
            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha");
            entity.Property(e => e.IdProducto).HasColumnName("id_producto");
            entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
            entity.Property(e => e.Total)
                .HasPrecision(10, 2)
                .HasColumnName("total");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.Compra)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_compra_producto");

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.Compra)
                .HasForeignKey(d => d.IdProveedor)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_compra_proveedor");
        });

        modelBuilder.Entity<Detalle>(entity =>
        {
            entity.HasKey(e => e.IdDetalle).HasName("detalle_pkey");

            entity.ToTable("detalle");

            entity.Property(e => e.IdDetalle).HasColumnName("id_detalle");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.Descuento)
                .HasPrecision(10, 2)
                .HasColumnName("descuento");
            entity.Property(e => e.IdProducto).HasColumnName("id_producto");
            entity.Property(e => e.IdVenta).HasColumnName("id_venta");
            entity.Property(e => e.PrecioUnitario)
                .HasPrecision(10, 2)
                .HasColumnName("precio_unitario");
            entity.Property(e => e.Subtotal)
                .HasPrecision(10, 2)
                .HasColumnName("subtotal");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.Detalle)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_detalle_producto");

            entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.Detalle)
                .HasForeignKey(d => d.IdVenta)
                .HasConstraintName("fk_detalle_venta");
        });

        modelBuilder.Entity<Persona>(entity =>
        {
            entity.HasKey(e => e.IdPersona).HasName("persona_pkey");

            entity.ToTable("persona");

            entity.Property(e => e.IdPersona).HasColumnName("id_persona");
            entity.Property(e => e.ApellidoMat)
                .HasMaxLength(80)
                .HasColumnName("apellido_mat");
            entity.Property(e => e.ApellidoPat)
                .HasMaxLength(80)
                .HasColumnName("apellido_pat");
            entity.Property(e => e.Nombre)
                .HasMaxLength(80)
                .HasColumnName("nombre");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<Personal>(entity =>
        {
            entity.HasKey(e => e.IdPersonal).HasName("personal_pkey");

            entity.ToTable("personal");

            entity.HasIndex(e => e.IdPersona, "personal_id_persona_key").IsUnique();

            entity.Property(e => e.IdPersonal).HasColumnName("id_personal");
            entity.Property(e => e.Cargo)
                .HasMaxLength(50)
                .HasColumnName("cargo");
            entity.Property(e => e.Estado)
                .HasDefaultValue(1)
                .HasColumnName("estado");
            entity.Property(e => e.IdPersona).HasColumnName("id_persona");

            entity.HasOne(d => d.IdPersonaNavigation).WithOne(p => p.Personal)
                .HasForeignKey<Personal>(d => d.IdPersona)
                .HasConstraintName("fk_personal_persona");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.IdProducto).HasName("producto_pkey");

            entity.ToTable("producto");

            entity.HasIndex(e => e.Nombre, "producto_nombre_key").IsUnique();

            entity.Property(e => e.IdProducto).HasColumnName("id_producto");
            entity.Property(e => e.IdCategoria).HasColumnName("id_categoria");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.Precio)
                .HasPrecision(10, 2)
                .HasColumnName("precio");
            entity.Property(e => e.Stock).HasColumnName("stock");

            entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Producto)
                .HasForeignKey(d => d.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_producto_categoria");
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(e => e.IdProveedor).HasName("proveedor_pkey");

            entity.ToTable("proveedor");

            entity.HasIndex(e => e.Nombre, "proveedor_nombre_key").IsUnique();

            entity.Property(e => e.IdProveedor).HasColumnName("id_proveedor");
            entity.Property(e => e.Direccion)
                .HasMaxLength(150)
                .HasColumnName("direccion");
            entity.Property(e => e.Estado)
                .HasDefaultValue(1)
                .HasColumnName("estado");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
            entity.Property(e => e.NombreContacto)
                .HasMaxLength(100)
                .HasColumnName("nombre_contacto");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.HasKey(e => e.IdVenta).HasName("venta_pkey");

            entity.ToTable("venta");

            entity.Property(e => e.IdVenta).HasColumnName("id_venta");
            entity.Property(e => e.Estado)
                .HasDefaultValue(1)
                .HasColumnName("estado");
            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("fecha");
            entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
            entity.Property(e => e.IdPersonal).HasColumnName("id_personal");
            entity.Property(e => e.Total)
                .HasPrecision(10, 2)
                .HasColumnName("total");

            entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.Venta)
                .HasForeignKey(d => d.IdCliente)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_venta_cliente");

            entity.HasOne(d => d.IdPersonalNavigation).WithMany(p => p.Venta)
                .HasForeignKey(d => d.IdPersonal)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_venta_personal");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
