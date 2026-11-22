using HeladeriaJackFrost.Data;
using HeladeriaJackFrost.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;

namespace HeladeriaJackFrost;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Debug()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Iniciando HeladeriaJackFrost...");

            var builder = WebApplication.CreateBuilder(args);

            // Conecta Serilog con appsettings.json (donde configuramos el archivo .txt)
            builder.Services.AddSerilog((services, configuration) => configuration
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services));

            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<HeladeriaDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Acceso/Login";
                    options.AccessDeniedPath = "/Acceso/AccesoDenegado";
                });

            var app = builder.Build();

            // Registra peticiones HTTP (opcional)
            app.UseSerilogRequestLogging();

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

            // Seed de datos iniciales: aislado para que un fallo de DB no tumbe el arranque
            if (app.Environment.IsDevelopment())
            {
                SeedDatosIniciales(app);
            }
            
            app.UseSerilogRequestLogging(options =>
            {
                options.GetLevel = (httpContext, elapsed, ex) =>
                {
                    if (ex != null || httpContext.Response.StatusCode >= 500)
                        return Serilog.Events.LogEventLevel.Error;

                    var path = httpContext.Request.Path.Value ?? string.Empty;
                    if (path.StartsWith("/css") ||
                        path.StartsWith("/js") ||
                        path.StartsWith("/lib") ||
                        path.EndsWith(".css") ||
                        path.EndsWith(".js") ||
                        path.EndsWith("/favicon.ico"))
                    {
                        return Serilog.Events.LogEventLevel.Debug;
                    }

                    return Serilog.Events.LogEventLevel.Debug; // o Information si quieres ver rutas HTTP importantes
                };
            });

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "La aplicación falló al iniciar.");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void SeedDatosIniciales(WebApplication app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HeladeriaDbContext>();

            if (!context.Database.CanConnect())
            {
                Log.Warning("No se pudo conectar a la base de datos durante el seed inicial. Se omite el seed.");
                return;
            }

            if (context.Cuenta.Any()) return;
            
            var passwordHasher = new PasswordHasher<string>();
            string hashedPassword = passwordHasher.HashPassword(null!, "123456");

            var cuentasIniciales = new List<Cuenta>
            {
                new()
                {
                    IdPersona = 2,
                    Email = "camila.flores@jackfrost.com",
                    Usuario = "admin_camila",
                    Contra = hashedPassword
                },
                new()
                {
                    IdPersona = 5,
                    Email = "mostrador@jackfrost.com",
                    Usuario = "mostrador",
                    Contra = hashedPassword
                }
            };

            context.Cuenta.AddRange(cuentasIniciales);
            context.SaveChanges();
            Log.Information("Cuentas iniciales creadas correctamente.");
        }
        catch (PostgresException ex)
        {
            Log.Error(ex.MessageText, "Error de PostgreSQL durante el seed inicial.");
        }
        catch (NpgsqlException ex)
        {
            Log.Error(ex.Message, "Error de conexión/driver Npgsql durante el seed inicial.");
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message, "Error inesperado durante el seed inicial.");
        }
    }
}