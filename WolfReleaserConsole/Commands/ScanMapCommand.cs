using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfReleaser.General;
using WolfReleaser.Objects;
using WolfReleaser.Parsers;

namespace WolfReleaserConsole.Commands
{
    [Command("scanmap")]
    public class ScanMapCommand : ICommand
    {
        [Required]
        [DefaultArgument]
        [Argument(ArgType.File, "--path", "-P")]
        public string Path { get; set; }

        public void Execute()
        {
            var mapfiles = new MapFileCollection(this.Path);

            foreach (((var src, var trg), var i) in mapfiles.GetFiles().Select((x, i) => (x, i)))
            {
                Log.Info($"{(i + 1).ToString().PadRight(5)} {trg}");
            }
        }
    }
}
