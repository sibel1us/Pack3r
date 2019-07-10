using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WolfReleaser.General;
using WolfReleaser.Parsers;

namespace WolfReleaserConsole.Commands
{
    public enum ArgType
    {
        NONE,
        Folder,
        File,
        Bool,
        Number
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute
    {
        public string Command { get; }

        public CommandAttribute(string command)
        {
            this.Command = command;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ArgumentAttribute : Attribute
    {
        public ArgType ArgumentType { get; }
        public string[] Aliases { get; }

        public ArgumentAttribute(ArgType type, params string[] aliases)
        {
            this.ArgumentType = type;
            this.Aliases = aliases;
        }
    }

    public class DefaultArgumentAttribute : Attribute
    {
        public DefaultArgumentAttribute() { }
    }

    public static class CmdUtil
    {
        public static bool Match(ICommand cmd, string input, out string[] args)
        {
            var attr = cmd.GetType().GetCustomAttribute<CommandAttribute>(false);

            var index = input.IndexOf(attr.Command + " ");

            if (index == -1)
            {
                var outArgs = new List<string>();
                var value = input.Substring(index);

                args = outArgs.ToArray();
                return false;
            }

            args = null;
            return true;
        }

        public static void Do(string input)
        {
            var coll = new WolfReleaser.Objects.MapFileCollection(input);

            var stockPk3s = new[]
            {
                Path.Combine(coll.ETMain, "pak0.pk3"),
                Path.Combine(coll.ETMain, "pak1.pk3"),
                Path.Combine(coll.ETMain, "pak2.pk3")
            };

            var existingFiles = Pk3Reader.GetFiles(stockPk3s);

            Pk3Packer.PackPk3(
                Path.ChangeExtension(input, "pk3"),
                coll,
                existingFiles);

            Log.Info("Deleting temporary data...");
            FileUtil.DeleteTempData();
            Log.Info("Done.");
        }
    }

    public static class ArgUtil
    {
        private const StringComparison CMP = StringComparison.OrdinalIgnoreCase;

        private static Exception ArgEx(string msg)
        {
            return new ArgumentException(msg);
        }

        private static Exception FileNotFound(string file)
        {
            return new FileNotFoundException("File not found: " + file);
        }

        private static Exception FolderNotFound(string dir)
        {
            return new DirectoryNotFoundException("Directory not found: " + dir);
        }

        public static void Execute(Type t, string[] args)
        {
            var props = t
                .GetProperties()
                .Where(x => Attribute.IsDefined(x, typeof(ArgumentAttribute)))
                .Select(x => new
                {
                    prop = x,
                    type = x.GetCustomAttribute<ArgumentAttribute>().ArgumentType,
                    aliases = x.GetCustomAttribute<ArgumentAttribute>().Aliases,
                    isRequired = x.GetCustomAttribute<RequiredAttribute>() != null,
                    isDefault = x.GetCustomAttribute<DefaultArgumentAttribute>(false) != null,
                })
                .ToList();

            if (args.Length == 0 &&
                props.FirstOrDefault(p => p.isRequired)?.prop?.Name is string name)
                throw new Exception($"Argument for {name} is required.");

            if (args.Length > props.Count * 2)
                throw new Exception($"Too many arguments (max {props.Count})");

            if (args.Length % 2 == 1)
            {
            }

            var cmd = (ICommand)Activator.CreateInstance(t);

            var def = props.FirstOrDefault(x => x.isDefault);

            for (int i = 0; i < args.Length; i++)
            {
                int offset = 1;

                var arg = args[i];
                var match = (i == args.Length - 1) ? def : null
                    ?? props.FirstOrDefault(x => x.aliases.Contains(arg))
                    ?? throw new Exception($"Unknown argument '{arg}'");

                if (match == def)
                    offset = 0;

                switch (match.type)
                {
                    // bool flags are turned on by just argument name
                    case ArgType.Bool:
                        match.prop.SetValue(cmd, true);
                        break;
                    case ArgType.File:
                        var file = args.ElementAtOrDefault(i + offset)
                            ?? throw new Exception($"Missing argument for {match.prop.Name}");
                        if (File.Exists(file))
                        {
                            match.prop.SetValue(cmd, file);
                            i++;
                        }
                        else
                        {
                            throw new FileNotFoundException(
                                $"File not found: {Path.GetFullPath(file)}");
                        }
                        break;
                    case ArgType.Folder:
                        var dir = args.ElementAtOrDefault(i + offset)
                            ?? throw new Exception($"Missing argument for {match.prop.Name}");
                        if (Directory.Exists(dir))
                        {
                            match.prop.SetValue(cmd, dir);
                            i++;
                        }
                        else
                        {
                            throw new DirectoryNotFoundException(
                                $"Directory not found: {Path.GetFullPath(dir)}");
                        }
                        break;
                    case ArgType.Number:
                        var num = args.ElementAtOrDefault(i + offset)
                            ?? throw new Exception($"Missing argument for {match.prop.Name}");
                        if (int.TryParse(num, out int number))
                        {
                            match.prop.SetValue(cmd, number);
                            i++;
                        }
                        else
                            throw new FormatException(
                                $"Expecting number for {match.prop.Name}, got '{num}'");
                        break;
                }

                props.Remove(match);
            }

            if (props.FirstOrDefault(p => p.isRequired) is var o
                && o != null)
            {
                throw new Exception($"Argument for {o.prop.Name} is required.");
            }

            cmd.Execute();
        }
    }
}
