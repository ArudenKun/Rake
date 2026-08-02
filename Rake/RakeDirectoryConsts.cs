using System;
using System.IO;
using Rake.Core;
using Rake.Core.Extensions;

namespace Rake;

public static class RakeDirectoryConsts
{
    public static readonly string App = AppContext.BaseDirectory;

    public static readonly string Local = Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData
    );

    public static readonly string LocalLow = Local + "Low";

    public static readonly string Roaming = Environment.GetFolderPath(
        Environment.SpecialFolder.ApplicationData
    );

    public static readonly string Data;
    public static readonly string Logs;
    public static readonly string Tools;

    static RakeDirectoryConsts()
    {
        if (
            !File.Exists(App.CombinePath(".portable"))
            && !Directory.Exists(App.CombinePath("data"))
            && !RakeConsts.IsDebug
        )
        {
            Data = Roaming.CombinePath(RakeConsts.Name);
        }
        else
        {
            var dataDir = App.CombinePath("data");
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            Data = dataDir;
        }

        Logs = Data.CombinePath("Logs");
        Tools = Data.CombinePath("Tools");
    }
}
