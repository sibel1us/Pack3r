using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfReleaser.Parsers;

namespace WolfReleaser.Objects
{
    public class Mapscript
    {
        public string MapName { get; set; }
        public string FullPath { get; set; }
        public HashSet<string> Remaps { get; set; }
        public HashSet<string> Sounds { get; set; }

        public override string ToString()
        {
            return string.Format(
                "{0} ({1} remaps, {2} sounds)",
                Path.GetFileName(this.FullPath),
                this.Remaps.Count,
                this.Sounds.Count);
        }
    }
}
