using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using WolfReleaser.General;
using WolfReleaser.Parsers;

namespace WolfReleaserConsole.Commands
{
    [Command("scanshaders")]
    public class ScanShadersCommand : ICommand
    {
        [Required]
        [DefaultArgument]
        [Argument(ArgType.Folder, "--path", "-P")]
        public string Path { get; set; }

        public void Execute()
        {
            var shaders = ShaderParser.ReadShaders(this.Path);
            ShaderParser.FindDuplicates(shaders);
        }
    }
}
