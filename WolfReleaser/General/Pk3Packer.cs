using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using WolfReleaser.Objects;
using System.IO;

namespace WolfReleaser.General
{
    public static class Pk3Packer
    {
        public static void PackPk3(
            string path,
            MapFileCollection files,
            HashSet<Pk3File> stockFiles)
        {
            var folder = FileUtil.NewTempFolder;

            var stock = new HashSet<string>(stockFiles.Select(x => x.Path));

            foreach ((string source, string target) in files.GetFiles())
            {
                // If exact match found in stock files, skip
                if (stock.Contains(target))
                {
                    Log.Debug($"Skipping '{target}', exists in stock pk3s.");
                    continue;
                }

                string sourceFile = null;
                string targetFile = null;
                string ambiguous = null;

                // Exact match
                if (File.Exists(source))
                {
                    sourceFile = source;
                    targetFile = target;
                }
                else if (Path.GetExtension(source) == "")
                {
                    if (File.Exists(ambiguous = Path.ChangeExtension(source, "tga")))
                    {
                        Log.Debug($"Using found tga version for ambiguous file {target}");
                        sourceFile = ambiguous;
                        targetFile = Path.ChangeExtension(target, "tga");
                    }
                    else if (stock.Contains(ambiguous))
                    {
                        Log.Debug($"TGA for ambiguous file '{target}' found in stock pk3s");
                        continue;
                    }
                    else if (File.Exists(ambiguous = Path.ChangeExtension(source, "jpg")))
                    {
                        Log.Debug($"Using found jpg version for ambiguous file {target}");
                        sourceFile = ambiguous;
                        targetFile = Path.ChangeExtension(target, "jpg");
                    }
                    else if (stock.Contains(ambiguous))
                    {
                        Log.Debug($"JPG for ambiguous file '{target}' found in stock pk3s");
                        continue;
                    }
                }

                if (sourceFile == null || targetFile == null)
                {
                    Log.Error($"Cannot find '{target}' in filesystem or stock pk3s.");
                    continue;
                }

                var fullPathToTarget = Path.Combine(folder, targetFile);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPathToTarget));
                File.Copy(sourceFile, fullPathToTarget, false);

                Log.Debug($" -> {targetFile}");
            }

            ZipFile.CreateFromDirectory(folder, path);
        }
    }
}
