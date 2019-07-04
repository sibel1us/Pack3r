using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WolfReleaser.Objects
{
    public class Shader
    {
        public string Name { get; set; }
        public HashSet<string> ImageFiles { get; set; }

        public override string ToString()
        {
            return $"{this.Name} ({this.ImageFiles.Count} images)";
        }
    }
}
