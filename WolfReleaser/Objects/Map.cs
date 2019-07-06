using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WolfReleaser.Objects
{
    public class Map
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public string ETMain => MapFileCollection.GetETMain(this.FullPath);
        public HashSet<string> Shaders { get; set; }
        public HashSet<string> Models { get; set; }
        public HashSet<string> Sounds { get; set; }
        public HashSet<string> Terrains { get; set; }

        public override string ToString()
        {
            return $"{this.Name} ({Shaders.Count} shaders, " +
                $"{Models.Count} models, {Sounds.Count} models)";
        }
    }
}
