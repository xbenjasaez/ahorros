# Ahorro

Aplicación de escritorio para gestión de presupuesto, transacciones, metas de ahorro y reportes financieros.

## Requisitos

- [.NET SDK](https://dotnet.microsoft.com/download) compatible con el proyecto
- Windows (WPF)

## Ejecución

```bash
dotnet build Ahorro.sln
dotnet run --project src/Ahorro.App/Ahorro.App.csproj
```

## Estructura

- `src/Ahorro.App` — aplicación principal
- `src/Ahorro.Views` / `src/Ahorro.ViewModels` — interfaz MVVM
- `src/Ahorro.Services` / `src/Ahorro.Repositories` / `src/Ahorro.Data` — lógica y persistencia
- `src/Ahorro.Models` — entidades y DTOs
