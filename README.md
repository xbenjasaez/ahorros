# Ahorro — Presupuesto personal (WPF .NET 8)

Aplicación de escritorio para gestión de presupuesto, ahorro, movimientos, metas y pagos programados.

## Requisitos

- .NET 8 SDK
- Windows 10/11

## Compilar y ejecutar

```powershell
cd c:\Proyectos\Escritorio\Ahorro
dotnet restore Ahorro.sln
dotnet build Ahorro.sln -c Release
dotnet run --project src\Ahorro.App\Ahorro.App.csproj
```

## Arquitectura

| Proyecto | Rol |
|----------|-----|
| Ahorro.App | Host WPF, DI, ventana principal |
| Ahorro.Views / ViewModels | MVVM estricto |
| Ahorro.Services | Lógica de negocio |
| Ahorro.Repositories / Data | EF Core + SQLite |
| Ahorro.Models | Entidades |
| Ahorro.Themes | Tema oscuro premium |
| Ahorro.Exports | Excel (ClosedXML) + PDF (QuestPDF) |
| Ahorro.Configuration | Seed de datos demo |

Base de datos: `%LocalAppData%\Ahorro\ahorro.db`

## Pantallas

Dashboard, Presupuesto, Movimientos, Metas, Pagos, Reportes, Configuración.

Datos de demostración se cargan en el primer arranque (perfil local, 3 periodos, transacciones, metas, pagos).
