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
    }
}
