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

            foreach ((var source, var target) in files.GetFiles())
            {
                var sourceFile = source;
                var targetFile = target;

                if (!File.Exists(source))
                {
                    var tga = Path.ChangeExtension(source, "tga");
                    var jpg = Path.ChangeExtension(source, "jpg");

                    if (File.Exists(tga))
                    {
                        Log.Debug($"Using found tga version for ambiguous file {target}");
                        sourceFile = tga;
                    }
                    else if (File.Exists(jpg))
                    {
                        Log.Debug($"Using found jpg version for ambiguous file {target}");
                        sourceFile = jpg;
                    }
                    else
                    {
                        throw new FileNotFoundException(
                            $"Can't find ambiguous file '{source}'");
                    }
                }

                var stockFile = stockFiles.FirstOrDefault(x => x.Path == targetFile);

                if (stockFile != null)
                {
                    var fi = new FileInfo(sourceFile);
                    if (fi.Length > stockFile.FileSize)
                    {
                        Log.Debug($"Using version in folder of '{targetFile}', file bigger than" +
                            $"file in pk3 ({stockFile.FileSize} vs {stockFile.FileSize}).");
                    }
                    else
                    {
                        Log.Debug($"Skipping required file '{targetFile}', " +
                            "already exists in stock pk3s.");
                        continue;
                    }
                }

                var fullPathToTarget = Path.Combine(folder, targetFile);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPathToTarget));
                File.Copy(sourceFile, fullPathToTarget, false);
            }

            ZipFile.CreateFromDirectory(folder, path);
        }
    }
}
