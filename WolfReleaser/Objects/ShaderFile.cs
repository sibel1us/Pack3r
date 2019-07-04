using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WolfReleaser.Objects
{
    public class ShaderFile
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public bool InShaderlist { get; set; }
        public List<Shader> Shaders { get; set; }

        public override string ToString()
        {
            return $"{this.FileName} ({this.Shaders.Count} shaders)";
        }
    }
}
