﻿using System;
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
    public class SpeakerNoiseMatch : BaseParserMatch<Speakerscript>
    {
        public override bool IsMatch(string line)
        {
            return line.StartsWith("noise ", CMP);
        }

        public override object Process(string line, Speakerscript target)
        {
            if (this.IsEnabled && this.IsMatch(line))
            {
                target.Sounds.Add(line.GetSplitPart(1).TrimQuotes());
                return this.pass;
            }

            return null;
        }
    }

    public class SpeakerscriptParser : BaseParser<Speakerscript>
    {
        public SpeakerNoiseMatch SpeakerNoise { get; } = new SpeakerNoiseMatch();

        public static bool HasSpeakerscript(Map map)
        {
            return File.Exists(GetScriptPath(map.FullPath));
        }

        public SpeakerscriptParser(string path)
        {
            this.filepath = path;
            this.lines = File.ReadAllLines(path);
        }

        public SpeakerscriptParser(Map map)
            : this(GetScriptPath(map.FullPath)) { }

        public static string GetScriptPath(string mapPath)
        {
            var etmain = Directory.GetParent(Path.GetDirectoryName(mapPath)).FullName;
            var sps = Path.ChangeExtension(Path.GetFileNameWithoutExtension(mapPath), "sps");
            var full = Path.Combine(etmain, "sound", "map", sps);
            return full;
        }

        public override Speakerscript Parse()
        {
            var script = new Speakerscript
            {
                FullPath = this.FullPath,
                Sounds = new HashSet<string>()
            };

            foreach ((string line, int lineNumber) in lines.Clean().SkipComments())
            {
                this.SpeakerNoise.Process(line, script);
            }

            return script;
        }
    }
}
