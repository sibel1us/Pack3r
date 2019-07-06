using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfReleaser.General;

namespace WolfReleaser.General
{
    public static class Pk3Reader
    {
        public static HashSet<Pk3File> GetFiles(params string[] pk3paths)
        {
            var files = new HashSet<Pk3File>();

            foreach (var pk3path in pk3paths)
            {
                if (!File.Exists(pk3path))
                {
                    Log.Fatal($"File not found '{pk3path}'");
                    continue;
                }

                try
                {
                    using (var pk3 = ZipFile.OpenRead(pk3path))
                    {
                        files.UnionWith(pk3.Entries.Select(x => new Pk3File
                        {
                            Path = x.FullName,
                            FileSize = x.Length,
                            LastWrite = x.LastWriteTime
                        }));
                        Log.Debug($"Found {pk3.Entries.Count} files " +
                            $"in {Path.GetFileName(pk3path)}");
                    }
                }
                catch (Exception e)
                {
                    Log.Fatal($"Failed to read pk3 '{pk3path}': {e.Message}");
                    Log.Debug(e.ToString());
                }
            }

            return files;
        }
    }
}
