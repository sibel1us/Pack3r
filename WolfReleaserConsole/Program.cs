using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WolfReleaser.General;
using WolfReleaserConsole.Commands;

namespace WolfReleaserConsole
{
    class Program
    {
        private static IEnumerable<ICommand> commands = new ICommand[]
        {
            new ScanMapCommand(),
            new ScanShadersCommand(),
        };

        public static void Main(string[] args)
        {
            Log.SetConsoleLogging(true, LogLevel.All);

            while (true)
            {
                Console.Write(".map file: ");
                try
                {
                    string input = Console.ReadLine().Trim();
                    CmdUtil.Do(input);
                }
                catch(Exception e)
                {
                    Log.Fatal(e.ToString());
                    break;
                }
            }

            Log.Error("Press any key to close the window");
            Console.ReadLine();
        }

        private static void PrintHelp()
        {
            var assmbl = Assembly.GetAssembly(typeof(Pk3Packer));

            Log.Info($"Pack3r version {assmbl.GetName().Version}");
            Console.WriteLine();

        }
        /*            string input;

            while ((input = Console.ReadLine().Trim()).NotEmpty())
            {
                string input = Console.ReadLine().Trim()
                var matched = false;

                try
                {
                    foreach (var cmd in commands)
                    {
                        if (CmdUtil.Match(cmd, input, out string[] cmdArgs))
                        {
                            matched = true;
                            ArgUtil.Execute(cmd.GetType(), cmdArgs);
                        }
                        break;
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);
                    Log.Debug(e.ToString());
                }

                if (!matched)
                {

                }
            }*/
    }
}
