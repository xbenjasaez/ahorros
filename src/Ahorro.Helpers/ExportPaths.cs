using System.IO;

namespace Ahorro.Helpers;

public static class ExportPaths
{
    public static string DefaultFolder
    {
        get
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Ahorro");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }
}
