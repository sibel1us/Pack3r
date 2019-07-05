using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WolfReleaser.Objects
{
    public class Speakerscript
    {
        public string MapName => Path.GetFileNameWithoutExtension(this.FullPath);
        public string FullPath { get; set; }
        public HashSet<string> Sounds { get; set; }

        public override string ToString()
        {
            return $"{Path.GetFileName(this.FullPath)} ({this.Sounds.Count} sounds)";
        }
    }
}
