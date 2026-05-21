namespace Ahorro.Data;

public static class DatabasePaths
{
    public static string GetDatabasePath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Ahorro");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "ahorro.db");
    }

    public static string GetConnectionString() =>
        $"Data Source={GetDatabasePath()}";
}
