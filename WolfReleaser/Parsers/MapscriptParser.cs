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
    public class MapscriptRemapShader : BaseParserMatch<Mapscript>
    {
        public override bool IsMatch(string line)
        {
            return line.StartsWith("remapshader ", CMP);
        }

        public override object Process(string line, Mapscript target)
        {
            if (this.IsMatch(line))
            {
                target.Remaps.Add(line.GetSplitPart(2).TrimQuotes());
                return this.pass;
            }

            return null;
        }
    }

    public class MapscriptPlaysound : BaseParserMatch<Mapscript>
    {
        public override bool IsMatch(string line)
        {
            return line.StartsWith("playsound ", CMP);
        }

        public override object Process(string line, Mapscript target)
        {
            if (this.IsEnabled && this.IsMatch(line))
            {
                target.Sounds.Add(line.GetSplitPart(1).TrimQuotes());
                return this.pass;
            }

            return null;
        }
    }

    public class MapscriptParser : BaseParser<Mapscript>
    {
        public static bool HasScript(Map map)
        {
            return File.Exists(GetScriptPath(map.FullPath));
        }

        public static string GetScriptPath(string mapPath)
        {
            return Path.ChangeExtension(mapPath, "script");
        }

        public MapscriptRemapShader RemapShader { get; } = new MapscriptRemapShader();
        public MapscriptPlaysound PlaySound { get; } = new MapscriptPlaysound();

        public MapscriptParser(string path)
        {
            this.filepath = path;
            this.lines = File.ReadAllLines(path);
        }

        public MapscriptParser(Map map)
            : this(Path.ChangeExtension(map.FullPath, "script")) { }

        public override Mapscript Parse()
        {
            var script = new Mapscript
            {
                MapName = Path.GetFileNameWithoutExtension(this.FullPath),
                FullPath = this.FullPath,
                Remaps = new HashSet<string>(),
                Sounds = new HashSet<string>()
            };

            Log.Debug($"Reading {this.lines.Length} lines in " +
                $"mapscript for {script.MapName}");

            foreach ((var line, var index) in this.Lines.Clean().RemoveComments())
            {
                _ = this.RemapShader.Process(line, script) ??
                    this.PlaySound.Process(line, script);
            }

            Log.Debug(string.Format(
                "Found {0} remapshaders and {1} playsounds in mapscript for {2}",
                script.Remaps.Count,
                script.Sounds.Count,
                script.MapName));

            return script;
        }
    }
}
