using System.Windows;
using Ahorro.Configuration;
using Ahorro.Data;
using Ahorro.Exports;
using Ahorro.Services;
using Ahorro.Services.Abstractions;
using Ahorro.ViewModels.Shell;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ahorro.App;

public partial class App : Application
{
    public static IHost HostApp { get; private set; } = null!;

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        HostApp = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        await InitializeDatabaseAsync();

        var main = HostApp.Services.GetRequiredService<MainWindow>();
        main.Show();

        var shell = HostApp.Services.GetRequiredService<MainShellViewModel>();
        if (shell.CurrentViewModel is ViewModels.ILoadable loadable)
            await loadable.LoadAsync();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseSqlite(DatabasePaths.GetConnectionString()));

        services.AddAhorroServices();
        services.AddAhorroViewModels();
        services.AddAhorroExports();

        services.AddSingleton<MainWindow>();
    }

    private static async Task InitializeDatabaseAsync()
    {
        using var scope = HostApp.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = scope.ServiceProvider.GetRequiredService<ICurrentUserContext>();
        await DataSeeder.SeedAsync(db, user);
        await scope.ServiceProvider.GetRequiredService<IBudgetPeriodService>().EnsureActivePeriodAsync();
    }
}
