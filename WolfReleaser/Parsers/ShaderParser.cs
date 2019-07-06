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
    public class ShaderTextureMatch : BaseParserMatch<Shader>
    {
        public override bool IsMatch(string line)
        {
            return line.StartsWith("map ", CMP)
                || line.StartsWith("implicit", CMP)
                || line.StartsWith("animmap ", CMP)
                || line.StartsWith("clampmap ", CMP)
                || line.StartsWith("videomap ", CMP);
        }

        public override object Process(string line, Shader target)
        {
            if (!this.IsEnabled || !this.IsMatch(line))
                return null;

            // implicitMap/Mask etc. is handled in an odd manner. If the line
            // doesn't equal any other options, but still starts with "implicit",
            // implicitMap is used.
            if (line.StartsWith("implicit", CMP))
            {
                var image = line.GetSplitPart(1);
                target.ImageFiles.Add(image == "-" ? target.Name : image);
                return this.pass;
            }
            // map and clampMap map normally
            else if (line.StartsWith("map ", CMP) || line.StartsWith("clampmap ", CMP))
            {
                var image = line.GetSplitPart(1);

                if (!image.Equalish("$lightmap") &&
                    !image.Equalish("$whiteimage"))
                {
                    target.ImageFiles.Add(image);
                    return this.pass;
                }
            }
            // animMap contains a list of textures after the keyword and frames-argument
            else if (line.StartsWith("animmap ", CMP))
            {
                target.ImageFiles.AddRange(line.GetSplit().Skip(2));
                return this.pass;
            }
            // videomap contains video name "videomap test.roq" which resides in etmain/video/
            else if (line.StartsWith("videomap ", CMP))
            {
                target.ImageFiles.Add($"video/{line.GetSplitPart(1)}");
                return this.pass;
            }

            return null;
        }
    }

    public static class ShaderParser
    {
        private static string currentFile = "";

        private static readonly ShaderTextureMatch texMatch = new ShaderTextureMatch();

        /// <summary>
        /// Reads all shader files in the target folder.
        /// </summary>
        /// <param name="scriptsFolder"></param>
        /// <returns></returns>
        public static IEnumerable<ShaderFile> ReadShaders(string scriptsFolder)
        {
            if (!Directory.Exists(scriptsFolder))
            {
                Log.Fatal($"scripts-directory not found in '{scriptsFolder}'");
                yield break;
            }

            var shaderlist = ReadShaderList(
                Path.Combine(scriptsFolder, "shaderlist.txt"));

            Log.Debug($"Scanning folder for shaders... '{scriptsFolder}'");

            var files = Directory.GetFiles(scriptsFolder, "*.shader");

            if (files.Length == 0)
            {
                Log.Fatal($"No .shader-files found in '{scriptsFolder}'");
            }
            else
            {
                Log.Debug($"Found {files.Length} shaders");
            }

            foreach (var file in files)
            {
                var parsedShader = ReadShaderfile(file, shaderlist);
                //shaderlist.Remove(parsedShader.FileName);
                yield return parsedShader;
            }

            // TODO: handle things like egyptsoc_lights in shaderlist
            //foreach (var orphan in shaderlist)
            //{
            //    Log.Warn($"shaderlist.txt references missing shader {orphan}.shader");
            //}
        }

        public static HashSet<string> ReadShaderList(string path)
        {
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path)
                    .Select(s => s.Trim())
                    .Where(s => s.HasValue() && !s.IsComment());

                var hs = new HashSet<string>(lines);
                Log.Debug($"Found {hs.Count} shaders in shaderlist '{path}'");
                return hs;
            }
            else
            {
                Log.Warn($"shaderlist.txt not found in '{path}'");
                return new HashSet<string>();
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
                Log.Warn($"{fileName}.shader in folder but not in shaderlist.txt");
                shaderlist.Remove(fileName);
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
            Shader currentShader = null;

            foreach ((string line, int lineNumber) in lines.Clean().SkipComments())
            {
                if (expect != null)
                {
                    if (line.StartsWith(expect))
                    {
                        expect = null;
                        continue;
                    }
                    else
                    {
                        Log.Error($"Expecting {expect} on line {lineNumber} in {currentFile}");
                        yield break;
                    }
                }

                if (!inShader)
                {
                    currentShader = new Shader
                    {
                        Name = line,
                        ImageFiles = new HashSet<string>()
                    };
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
                            yield return currentShader;

                            currentShader = null;
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
                            Log.Error($"Bracket depth too deep in {currentFile} line {lineNumber}");
                            yield break;
                        }
                    }

                    if (inDirective)
                    {
                        texMatch.Process(line, currentShader);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the required image and shader files by using a list of the map's shaders.
        /// </summary>
        public static (HashSet<string> images, HashSet<string> shaders) GetRequiredFiles(
            Map map,
            IEnumerable<ShaderFile> shaderFiles,
            string etmain = null)
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
                    Log.Warn($"Shader definition '{shaderName}' not found, " +
                        $"using only texture if it exists.");
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

                    images.AddRange(matches);
                }
            }

            // Get levelshots
            string levelshotPath = "levelshots/" + map.Name;

            foreach (var shaderFile in shaderFiles)
            {
                var levelshot = shaderFile.Shaders
                        .FirstOrDefault(s => s.Name.StartsWith(levelshotPath));

                if (levelshot != null)
                {
                    images.AddRange(levelshot.ImageFiles);
                    shaders.Add(shaderFile.FullPath);
                }
            }

            return (images, shaders);
        }
    }
}
