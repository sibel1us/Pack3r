using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfReleaser.General;
using WolfReleaser.Parsers;

namespace WolfReleaser.Objects
{
    public enum FileType
    {
        NONE = 0,
        BSP,
        Lightmap,
        Texture,
        Image = Texture,
        Shader,
        Model,
        Sound,
        MapSource,
        Mapscript,
        Soundscript,
        Speakerscript,
        Arena,
    }

    public class MapFileCollection
    {
        public bool IncludeSource { get; set; }

        public string MapPath { get; }
        public string MapName => Path.GetFileNameWithoutExtension(this.MapPath);

        public string ETMain { get; }
        public string ScriptsFolder => Path.Combine(this.ETMain, "scripts");
        public string TexturesFolder => Path.Combine(this.ETMain, "textures");
        public string MapsFolder => Path.Combine(this.ETMain, "maps");
        public string ModelsFolder => Path.Combine(this.ETMain, "models");
        public string SoundFolder => Path.Combine(this.ETMain, "sound");
        public string LevelshotsFolder => Path.Combine(this.ETMain, "levelshots");
        public string LightmapFolder => Path.Combine(this.MapsFolder, this.MapName);

        public HashSet<string> Shaders { get; } = new HashSet<string>();
        public HashSet<string> Textures { get; } = new HashSet<string>();
        public HashSet<string> Models { get; } = new HashSet<string>();
        public HashSet<string> Sounds { get; } = new HashSet<string>();
        public HashSet<string> MiscFiles { get; } = new HashSet<string>();

        public MapFileCollection(string mapPath)
        {
            var etmain = Directory.GetParent(
                Directory.GetParent(mapPath).FullName).FullName;

            if (!etmain.EndsWith($"{Path.DirectorySeparatorChar}"))
            {
                etmain += Path.DirectorySeparatorChar;
            }

            this.MapPath = mapPath;
            this.ETMain = etmain;

            this.Validate();

            this.Init();
        }

        public IEnumerable<(string source, string target)> GetFiles()
        {
            var allFiles = Enumerable.Empty<string>()
                .Concat(this.MiscFiles)
                .Concat(this.Shaders)
                .Concat(this.Textures)
                .Concat(this.Models)
                .Concat(this.Sounds);

            if (this.IncludeSource)
                allFiles.Concat(new[] { this.MapPath });

            var refUri = new Uri(this.ETMain);

            var bspPath = Path.ChangeExtension(this.MapPath, "bsp");

            if (File.Exists(bspPath))
            {
                this.MiscFiles.Add(bspPath);
            }
            else
            {
                Log.Error($"Compiled BSP not found '{bspPath}'");
            }

            foreach (var file in allFiles)
            {
                if (Path.IsPathRooted(file))
                {
                    yield return (
                        Path.GetFullPath(file),
                        refUri.MakeRelativeUri(new Uri(file)).ToString());
                }
                else
                {
                    yield return (
                        Path.GetFullPath(Path.Combine(ETMain, file)),
                        file);
                }
            }

            yield break;
        }

        private void Init()
        {
            var mapParser = new MapParser(this.MapPath);
            var map = mapParser.Parse();

            this.Sounds.AddRange(map.Sounds);
            this.Models.AddRange(map.Models);

            var allShaders = ShaderParser.ReadShaders(Path.Combine(this.ETMain, "scripts")).ToList();
            var (usedTextures, usedShaderFiles) = ShaderParser.GetRequiredFiles(map, allShaders);

            this.Shaders.AddRange(usedShaderFiles);
            this.Textures.AddRange(usedTextures);

            if (MapscriptParser.HasScript(map))
            {
                var msParser = new MapscriptParser(map);
                var mapscript = msParser.Parse();
                this.Sounds.AddRange(mapscript.Sounds);
                this.Textures.AddRange(mapscript.Remaps);
                this.MiscFiles.Add(mapscript.FullPath);
            }
            else
            {
                Log.Info($"Mapscript not found '{MapscriptParser.GetScriptPath(map.FullPath)}'");
            }

            if (SpeakerscriptParser.HasSpeakerscript(map))
            {
                var spsParser = new SpeakerscriptParser(map);
                var speakerscript = spsParser.Parse();
                this.Sounds.AddRange(speakerscript.Sounds);
                this.MiscFiles.Add(speakerscript.FullPath);
            }
            else
            {
                Log.Info($"Speaker script not found" +
                    $" '{SpeakerscriptParser.GetScriptPath(map.FullPath)}'");
            }

            if (SoundscriptParser.HasSoundscript(map))
            {
                var ssparser = new SoundscriptParser(map);
                var soundscript = ssparser.Parse();
                this.Sounds.AddRange(soundscript.Sounds);
                this.MiscFiles.Add(soundscript.FullPath);
            }
            else
            {
                Log.Info($"Soundscript not found" +
                    $" '{SoundscriptParser.GetScriptPath(map.FullPath)}'");
            }

            // Add lightmaps
            if (Directory.Exists(this.LightmapFolder))
            {
                var lightmaps = Directory.GetFiles(this.LightmapFolder, "lm_????.tga");

                if (!lightmaps.Any())
                {
                    Log.Warn($"Lightmap folder found but " +
                        $"contains no lightmaps ('{this.LightmapFolder}')");
                }

                this.MiscFiles.AddRange(lightmaps);
            }
            else
            {
                Log.Info($"Lightmap folder not found '{this.LightmapFolder}'");
            }

            // Add levelshots, .arena, .objdata, and tracemap
            this.GetMiscFiles();
        }

        private void GetMiscFiles()
        {
            // Get levelshots
            var lvshots = Path.Combine(this.LevelshotsFolder, this.MapName);
            var lvshotsTga = Path.ChangeExtension(lvshots, "tga");
            var lvshotsJpg = Path.ChangeExtension(lvshots, "jpg");

            if (File.Exists(lvshotsTga))
            {
                this.Textures.Add(lvshotsTga);
            }
            else if (File.Exists(lvshotsJpg))
            {
                this.Textures.Add(lvshotsJpg);
            }
            else
            {
                Log.Info($"Levelshots file (tga/jpg) not found '{lvshots}'");
            }

            // Get arena
            var arenaFile = Path.Combine(this.ScriptsFolder, $"{this.MapName}.arena");

            if (File.Exists(arenaFile))
            {
                this.MiscFiles.Add(arenaFile);
            }
            else
            {
                Log.Info($"No arena file found '{arenaFile}'");
            }

            // Get other random files
            var objData = Path.Combine(this.MapsFolder, $"{MapName}.objdata");

            if (File.Exists(objData))
            {
                this.MiscFiles.Add(objData);
            }
            else
            {
                Log.Info($"No objective data file found '{objData}'");
            }

            var tracemap = Path.Combine(this.MapsFolder, $"{MapName}_tracemap.tga");

            if (File.Exists(tracemap))
            {
                this.MiscFiles.Add(tracemap);
            }
            else
            {
                Log.Info($"No tracemap found '{tracemap}'");
            }
        }

        private void Validate()
        {
            if (new DirectoryInfo(this.ETMain).Name != "etmain")
            {
                Log.Error($"Map not in /etmain/maps/ -folder!");
            }

            this.FindDir(true, this.ETMain);
            this.FindDir(true, this.TexturesFolder);
            this.FindDir(true, this.ScriptsFolder);
            this.FindDir(true, this.MapsFolder);
            this.FindDir(false, this.ModelsFolder);
            this.FindDir(false, this.SoundFolder);
            this.FindDir(false, this.LevelshotsFolder);
            this.FindDir(false, Path.Combine(this.ETMain, "video"));
            this.FindFile(true, "pak0.pk3");
            this.FindFile(true, "pak1.pk3");
            this.FindFile(true, "pak2.pk3");
            this.FindFile(true, "pak2.pk3");
        }

        private void FindDir(bool fatal, string dir)
        {
            if (!Directory.Exists(dir))
            {
                var err = $"Directory '{dir}' not found.";

                if (fatal)
                { Log.Error(err); }
                else
                { Log.Warn(err); }
            }
        }

        private void FindFile(bool fatal, params string[] pathParts)
        {
            var arr = new[] { this.ETMain }.Concat(pathParts).ToArray();
            var target = Path.Combine(arr);

            if (!File.Exists(target))
            {
                var err = $"File '{target}' not found.";

                if (fatal)
                { Log.Error(err); }
                else
                { Log.Warn(err); }
            }
        }
    }
}
