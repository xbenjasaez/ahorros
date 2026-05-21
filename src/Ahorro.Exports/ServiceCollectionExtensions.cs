using Ahorro.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Ahorro.Exports;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAhorroExports(this IServiceCollection services)
    {
        services.AddScoped<IExcelExportService, ExcelExportService>();
        services.AddScoped<IPdfExportService, PdfExportService>();
        return services;
    }
}
