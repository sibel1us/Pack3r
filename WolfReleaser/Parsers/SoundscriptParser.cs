using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfReleaser.General;
using WolfReleaser.Objects;

namespace WolfReleaser.Parsers
{
    public class SoundscriptNoise : BaseParserMatch<Soundscript>
    {
        public override bool IsMatch(string line)
        {
            return line.StartsWith("sound ", CMP);
        }

        public override object Process(string line, Soundscript target)
        {
            if (this.IsEnabled && this.IsMatch(line))
            {
                target.Sounds.Add(line.GetSplitPart(1));
                return this.pass;
            }

            return null;
        }
    }

    public class SoundscriptParser : BaseParser<Soundscript>
    {
        public SoundscriptNoise ScriptNoise { get; } = new SoundscriptNoise();

        public static bool HasSoundscript(Map map)
        {
            return File.Exists(GetScriptPath(map.FullPath));
        }

        public SoundscriptParser(string path)
        {
            this.filepath = path;
            this.lines = File.ReadAllLines(path);
        }

        public static SoundscriptParser TryInitialize(Map map)
        {
            string path = GetScriptPath(map.FullPath);

            if (File.Exists(path))
            {
                return new SoundscriptParser(path);
            }
            else
            {
                Log.Debug($"Soundscript not found in '{path}'");
                return null;
            }
        }

        public SoundscriptParser(Map map)
            : this(GetScriptPath(map.FullPath)) { }

        public static string GetScriptPath(string mapPath)
        {
            var etmain = Directory.GetParent(Path.GetDirectoryName(mapPath)).FullName;
            var sounds = Path.ChangeExtension(Path.GetFileNameWithoutExtension(mapPath), "sounds");
            var full = Path.Combine(etmain, "sound", "scripts", sounds);
            return full;
        }

        public override Soundscript Parse()
        {
            var script = new Soundscript
            {
                FullPath = this.FullPath,
                Sounds = new HashSet<string>()
            };

            Log.Debug($"Reading {this.lines.Length} lines in soundscript " +
                $"for {script.MapName}");

            foreach ((string line, int lineNumber) in lines.Clean().SkipComments())
            {
                this.ScriptNoise.Process(line, script);
            }

            Log.Debug($"Found {script.Sounds.Count} sounds in soundscript for {script.MapName}");

            return script;
        }
    }
}
