using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WolfReleaser.General
{
    public static class FileUtil
    {
        public static string AppDataFolder
        {
            get
            {
                var appdata = Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData);
                var path = Path.Combine(appdata, "Pack3r");

                if (!Directory.Exists(path))
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                        Log.Debug($"Created working directory '{path}'");
                    }
                    catch (Exception e)
                    {
                        Log.Fatal($"Failed to create working directory " +
                            $"to '{path}': {e.Message}");
                        Log.Debug(e.ToString());
                        return null;
                    }
                }

                return path;
            }
        }

        public static string NewTempFolder
        {
            get
            {
                var path = Path.Combine(AppDataFolder, Guid.NewGuid().ToString());

                try
                {
                    Directory.CreateDirectory(path);
                    Log.Debug($"Created temporary directory '{path}'");
                }
                catch (Exception e)
                {
                    Log.Fatal($"Failed to create temporary directory " +
                        $"to '{path}': {e.Message}");
                    Log.Debug(e.ToString());
                    return null;
                }

                return path;
            }
        }

        public static void DeleteTempData()
        {
            foreach (var path in Directory.GetDirectories(AppDataFolder))
            {
                try
                {
                    Directory.Delete(path);
                    Log.Debug($"Deleted temporary directory '{path}'");
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to delete temporary directory " +
                        $"to '{path}': {e.Message}");
                    Log.Debug(e.ToString());
                }
            }
        }
    }
}
