using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HeladeriaJackFrost;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        
        // Configurar DbContext 
        builder.Services.AddDbContext<HeladeriaDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
        
        // Configurar Autenticación por Cookies
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Acceso/Login";     // A dónde redirige si no está logueado
                options.AccessDeniedPath = "/Acceso/AccesoDenegado"; // Si no tiene permisos
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
            });
        
        var app = builder.Build();
        
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
        }

        app.UseRouting();

        app.UseAuthentication(); 
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Acceso}/{action=Login}/{id?}")
            .WithStaticAssets();

        /* --- para dar almenos dos cuentas de prueba ---
            analizar si esto es una buena practica XD...
        */
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<HeladeriaDbContext>();

            try
            {
                await context.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE producto
                        ADD COLUMN IF NOT EXISTS estado integer NOT NULL DEFAULT 1;
                    """);
            }
            catch (PostgresException ex)
            {
                throw new InvalidOperationException(
                    "No se pudo preparar la columna producto.estado. Verifique que exista la tabla producto y que el usuario de la base de datos tenga permisos para modificar su esquema.",
                    ex);
            }

            // si la tabla cuenta está vacía, la poblamos
            if (!await context.Cuenta.AnyAsync())
            {
                var passwordHasher = new PasswordHasher<string>();
                // genera un hash compatible con ASP.NET Core para "123456"
                string hashedPassword = passwordHasher.HashPassword(null, "123456");

                var cuentasIniciales = new List<Cuenta>
                {
                    new Cuenta { IdPersona = 2, Email = "camila.flores@jackfrost.com", Usuario = "admin_camila", Contra = hashedPassword },
                    new Cuenta { IdPersona = 5, Email = "mostrador@jackfrost.com", Usuario = "mostrador", Contra = hashedPassword }
                };

                context.Cuenta.AddRange(cuentasIniciales);
                await context.SaveChangesAsync();
            }
        }
        /* -------------------------------------------------------- */
        
        await app.RunAsync();
    }
}