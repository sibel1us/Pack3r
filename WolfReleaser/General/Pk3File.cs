using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WolfReleaser.General
{
    public sealed class Pk3File
    {
        public string Path { get; set; }
        public DateTimeOffset LastWrite { get; set; }
    }
}
