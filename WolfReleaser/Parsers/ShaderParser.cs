using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfReleaser.General;
using WolfReleaser.Objects;

namespace WolfReleaser.Parsers
{
    public static class ShaderParser
    {
        private const StringComparison CMP = StringComparison.OrdinalIgnoreCase;
        private static string currentFile = "";

        /// <summary>
        /// Reads all shader files in the target folder.
        /// </summary>
        /// <param name="scriptsFolder"></param>
        /// <returns></returns>
        public static IEnumerable<ShaderFile> ReadShaders(string scriptsFolder)
        {
            if (!Directory.Exists(scriptsFolder))
            {
                Log.Error($"scripts-directory not found in '{scriptsFolder}'");
                yield break;
            }

            var shaderlist = new HashSet<string>();
            var shaderlistPath = Path.Combine(scriptsFolder, "shaderlist.txt");

            if (File.Exists(shaderlistPath))
            {
                foreach (var line in File.ReadAllLines(shaderlistPath).Select(s => s.Trim()))
                {
                    if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("//"))
                    {
                        shaderlist.Add(line);
                    }
                }
            }
            else
            {
                Log.Warn($"shaderlist.txt not found in '{scriptsFolder}'");
            }

            var files = Directory.GetFiles(scriptsFolder, "*.shader");

            if (files.Length == 0)
            {
                Log.Error($"No .shader-files found in '{scriptsFolder}'");
            }

            foreach (var file in files)
            {
                yield return ReadShaderfile(file, shaderlist);
            }
        }

        /// <summary>
        /// Reads a single .shader-file.
        /// </summary>
        public static ShaderFile ReadShaderfile(
            string shaderFilePath,
            HashSet<string> shaderlist)
        {
            currentFile = shaderFilePath;

            var fileName = Path.GetFileNameWithoutExtension(shaderFilePath);
            var inShaderList = shaderlist.Contains(fileName);

            if (!inShaderList && shaderlist.Count > 0)
            {
                Log.Warn($"Shader '{fileName}' not found in shaderlist");
            }

            return new ShaderFile
            {
                FullPath = shaderFilePath,
                FileName = fileName,
                InShaderlist = inShaderList,
                Shaders = ParseShaders(File.ReadAllLines(shaderFilePath)).ToList()
            };
        }

        /// <summary>
        /// Crudely parses a shader file and returns each shader and the image files it uses.
        /// </summary>
        private static IEnumerable<Shader> ParseShaders(IEnumerable<string> lines)
        {
            string expect = null;
            bool inDirective = false;

            bool inShader = false;
            string shaderName = "";
            var files = new List<string>();

            foreach ((string line, int lineNumber) in lines.Select((s,i) => (s.Trim(), i)))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    continue;

                if (expect != null)
                {
                    if (line.StartsWith(expect))
                    {
                        expect = null;
                        continue;
                    }
                    else
                    {
                        Log.Error($"Expecting {expect}, got {line} " +
                            $"in {currentFile} line {lineNumber}");
                        yield break;
                    }
                }

                if (!inShader)
                {
                    shaderName = line;
                    expect = "{";
                    inShader = true;
                    continue;
                }
                else
                {
                    if (line.StartsWith("}"))
                    {
                        if (inDirective)
                        {
                            inDirective = false;
                            continue;
                        }
                        else
                        {
                            yield return new Shader
                            {
                                Name = shaderName,
                                ImageFiles = new HashSet<string>(files)
                            };

                            shaderName = "";
                            files = new List<string>();
                            inShader = false;
                            continue;
                        }
                    }
                    else if (line.StartsWith("{"))
                    {
                        if (!inDirective)
                        {
                            inDirective = true;
                            continue;
                        }
                        else
                        {
                            Log.Error($"Bracket depth too deep in " +
                                $"{currentFile} line {lineNumber}");
                            yield break;
                        }
                    }

                    if (inDirective)
                    {
                        if (line.StartsWith("implicit", CMP)
                        || line.StartsWith("map ", CMP)
                        || line.StartsWith("clampmap ", CMP))
                        {
                            var imageFile = line.GetSplitPart(1);

                            if (!imageFile.Equalish("$lightmap"))
                            {
                                files.Add(imageFile == "-" ? shaderName : imageFile);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the required image and shader files by using a list of the map's shaders.
        /// </summary>
        public static (HashSet<string> images, HashSet<string> shaders) GetRequiredFiles(
            MapFiles map,
            IEnumerable<ShaderFile> shaderFiles)
        {
            var images = new HashSet<string>();
            var shaders = new HashSet<string>();

            foreach (var shaderName in map.Shaders)
            {
                bool found = false;

                foreach (var shaderFile in shaderFiles)
                {
                    var shader = shaderFile.Shaders.FirstOrDefault(s => s.Name == shaderName);

                    if (shader != null)
                    {
                        images.AddRange(shader.ImageFiles);
                        shaders.Add(shaderFile.FullPath);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Log.Warn($"Shader {shaderName} not found in any shaders");
                }
            }

            foreach (var terrain in map.Terrains)
            {
                foreach (var shaderFile in shaderFiles)
                {
                    var matches = shaderFile
                        .Shaders
                        .Where(s => s.Name.StartsWith(terrain))
                        .SelectMany(s => s.ImageFiles);

                    shaders.AddRange(matches);
                }
            }

            return (images, shaders);
        }
    }
}
