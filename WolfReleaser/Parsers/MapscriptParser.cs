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
        public override event PropertyChangedEventHandler PropertyChanged;

        public static bool HasScript(Map map)
        {
            return File.Exists(Path.ChangeExtension(map.FullPath, "script"));
        }

        public MapscriptRemapShader RemapShader { get; }
        public MapscriptPlaysound PlaySound { get; }

        public override bool IsEnabled
        {
            get
            {
                return this.RemapShader.IsEnabled || this.PlaySound.IsEnabled;
            }
        }

        public MapscriptParser(string path)
        {
            this.filepath = path;
            this.lines = File.ReadAllLines(path);

            this.RemapShader = new MapscriptRemapShader();
            this.PlaySound = new MapscriptPlaysound();

            this.RemapShader.PropertyChanged += this.ParserMatch_PropertyChanged;
            this.PlaySound.PropertyChanged += this.ParserMatch_PropertyChanged;
        }

        private void ParserMatch_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IParser<object>.IsEnabled))
            {
                PropertyChanged?.Invoke(
                    sender,
                    new PropertyChangedEventArgs(nameof(this.IsEnabled)));
            }
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

            if (!this.IsEnabled)
            {
                return script;
            }

            foreach ((var line, var index) in this.Lines.Clean().SkipComments())
            {
                _ = this.RemapShader.Process(line, script) ??
                    this.PlaySound.Process(line, script);
            }

            return script;
        }
    }
}
