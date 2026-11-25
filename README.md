# Heladería Jack Frost — Sistema de Gestión

Aplicación web MVC para administrar una heladería: inventario, proveedores, clientes, ventas, reportes y un dashboard de indicadores.

- **Backend:** ASP.NET Core MVC (.NET 10), Entity Framework Core 10 + Npgsql
- **Base de datos:** PostgreSQL 16+
- **Frontend:** Razor Views, Bootstrap 5 y Bootstrap Icons (locales), Chart.js 4 (CDN)
- **Autenticación:** cookies con roles derivados de `personal.cargo`
- **Tests:** xUnit (`HeladeriaJackFrost.Tests`)

## Estructura

```
HeladeriaJackFrost/                Aplicación MVC
├── Program.cs                     Entrypoint, DI, cookie auth, seed de cuentas
├── Controllers/                   Acceso, Home, Producto, Proveedor, Cliente, Venta, Reporte
├── Data/HeladeriaDbContext.cs     DbContext scaffolded desde el esquema SQL
├── Data/ReporteDAO.cs             Consultas SQL del reporte de ventas
├── Models/                        Entidades EF (Persona, Cliente, Personal, Venta...)
├── ViewModels/                    Modelos de vista y DTOs del dashboard/reportes
├── Views/                         Razor Views por módulo + _Layout compartido
├── sql/                           Esquema + datos semilla (10 tablas), Consultas de prueba
└── wwwroot/                       CSS/JS propios y librerías locales (Bootstrap)

HeladeriaJackFrost.Tests/          Tests xUnit
HeladeriaJackFrost.slnx            Solución con ambos proyectos
```

## Módulos

| Ruta | Módulo | Acceso |
| --- | --- | --- |
| `/Acceso/Login` | Inicio de sesión (ruta por defecto) | Público |
| `/Home` | Dashboard: KPIs, gráficas, selector de fecha, top productos | Administrador |
| `/Producto` | CRUD de productos, filtro por categoría | Administrador |
| `/Proveedor` | CRUD en modales, búsqueda, baja lógica (estado 5) | Administrador |
| `/Cliente` | Lista/alta/edición de clientes | Administrador |
| `/Cliente/PanelCliente` | Panel con datos y puntos del cliente | Administrador / Cliente |
| `/Venta` | Venta maestro-detalle con múltiples productos, control de stock | Administrador |
| `/Reporte/Ventas` | Reporte de ventas y cobranzas por rango de fechas (diario/mensual) | Administrador |
| `/Home/GuiaEstilos` | Vista de referencia de componentes y estilos | Administrador |

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/) (el proyecto apunta a `net10.0`)
- Podman o Docker para ejecutar PostgreSQL 16+ en local (o instale postgres nativamente)

## Base de datos local en contenedor (recomendado)

Para desarrollo local, levanta PostgreSQL 16+ en un contenedor y ejecuta la aplicación con `dotnet run` en el host. No necesitas instalar PostgreSQL en el host.

```bash
podman run -d --name jackfrost-db \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=tu_password \
  -e POSTGRES_DB=jackfrost_db \
  -p 5432:5432 \
  -v jackfrost-pgdata:/var/lib/postgresql \
  docker.io/library/postgres:18-alpine
```

Si usas Docker, reemplaza `podman` por `docker` y `docker.io/library/postgres:18-alpine` por `postgres:18-alpine`, cambie el 18 si gusta usar otra version de postgres que sea mayor o igual a la 16.

El puerto `5432` del host queda conectado al `5432` del contenedor. En PostgreSQL 18 el volumen debe montarse en `/var/lib/postgresql` (no en `/var/lib/postgresql/data`).

Después crea el esquema y carga los datos semilla:

```bash
podman exec -i jackfrost-db psql -U postgres -d jackfrost_db \
  < HeladeriaJackFrost/sql/PostgreDB.sql
```

- El script comienza con `CREATE database jackfrost_db;`, que fallará porque la base ya existe; es inofensivo y el resto del script continúa.
- Carga el script solo en una base vacía. Si vuelves a ejecutarlo sobre una base con datos, aparecerán errores de duplicados.
- No es necesario montar el script en `/docker-entrypoint-initdb.d`; cárgalo manualmente con el comando anterior.

### Reiniciar la base de datos

En caso de que se actualice el PostgresDB.sql:

```bash
podman rm -f jackfrost-db
podman volume rm jackfrost-pgdata
```

Después repite el `podman run` y la carga de `PostgreDB.sql`.

O puede hacer esta acción con un administrador de base de datos gráfico (como dbeaver, plugins para us IDE o editor de código favorito)

## Configurar la aplicación

La cadena de conexión real va en `HeladeriaJackFrost/appsettings.Development.json`, que está ignorado por git (cada desarrollador crea el suyo). Puedes partir de `appsettings.json` y ajustar la conexión:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=jackfrost_db;Username=postgres;Password=tu_password"
  }
}
```

- No edites la cadena de `appsettings.json`; es solo una plantilla/placeholder.
- `launchSettings.json` ya define `ASPNETCORE_ENVIRONMENT=Development`, así que este archivo se carga automáticamente al ejecutar con `dotnet run`.
- Si usas otro puerto para PostgreSQL, ajusta `Port` en la cadena de conexión.

## Ejecutar

```bash
dotnet run --project HeladeriaJackFrost/HeladeriaJackFrost.csproj
```

Abre <http://localhost:5062>.

> En `Development`, al iniciar, la aplicación intenta sembrar las cuentas de prueba si la tabla `cuenta` está vacía. Si la base no existe, no está inicializada o la cadena de conexión es incorrecta, el error se registra pero el arranque continúa; el login y las vistas que consultan datos fallarán hasta que la base esté disponible.

## Tests

```bash
dotnet test HeladeriaJackFrost.slnx
```

Para ejecutar solo los tests de una clase o archivo:

```bash
dotnet test HeladeriaJackFrost.slnx \
  --filter "FullyQualifiedName~VentaDescuentoTests"
```

Los tests actuales validan la regla de descuento de `Venta.AplicarDescuento` y no necesitan base de datos.

## Usuarios de prueba

En `Development`, al iniciar con la tabla `cuenta` vacía, `Program.cs` crea estas cuentas (contraseña `123456`):

| Usuario | Correo | Rol |
| --- | --- | --- |
| `admin_camila` | `camila.flores@jackfrost.com` | Administrador |
| `mostrador` | `mostrador@jackfrost.com` | Cliente |

El login acepta usuario o correo. Los roles se asignan desde `personal.cargo`, así que solo un cargo exactamente `Administrador` obtiene acceso al dashboard, a los CRUD y a los reportes.

## Roles y autorización

- **Administrador:** cargo `Administrador` en `personal` (estado activo).
- **Cliente:** tener registro en `cliente`.
- El resto de los cargos (`Cajero Principal`, etc.) puede iniciar sesión si su registro en `personal` está activo, pero no accede a las vistas de administración.

## Notas de implementación

- `sql/PostgreDB.sql` es la fuente del esquema y la semilla. No hay migraciones de EF: si cambia el esquema, actualiza el SQL y regenera/edita `Data/HeladeriaDbContext.cs`.
- La baja lógica usa `estado = 5` en productos y proveedores; el reporte excluye las ventas en estados 4 y 5.
- El descuento de ventas está limitado a 0–20% en `Venta.AplicarDescuento`.
- `ReporteDAO` ejecuta SQL crudo con Npgsql en lugar de EF Core.

## Compilar

```bash
dotnet build HeladeriaJackFrost.slnx
```
