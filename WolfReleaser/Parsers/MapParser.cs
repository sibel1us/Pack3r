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
    public static class MapParser
    {
        private enum LineStatus
        {
            None, Entity, Brush, Patch
        }

        public static Map ParseMap(string mapFilePath)
        {
            if (!File.Exists(mapFilePath))
            {
                Log.Error($"Map file not found: '{mapFilePath}'");
                return null;
            }

            IEnumerable<string> lines = File.ReadAllLines(mapFilePath);

            var mapFiles = new Map
            {
                Name = Path.GetFileNameWithoutExtension(mapFilePath),
                FullPath = mapFilePath,
                Shaders = new HashSet<string>(),
                Models = new HashSet<string>(),
                Sounds = new HashSet<string>(),
                Terrains = new HashSet<string>()
            };

            LineStatus currentState = LineStatus.None;
            string expect = null;
            string readUntil = null;

            var cache = new Dictionary<string, string>();

            foreach ((string line, int lineNumber) in lines.Select((s, i) => (s.Trim(), i)))
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

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
                            $"in {mapFilePath} line {lineNumber}");
                        break;
                    }
                }

                if (readUntil != null)
                {
                    if (line == readUntil)
                    {
                        readUntil = null;
                    }
                    continue;
                }

                switch (currentState)
                {
                    case LineStatus.None:
                    {
                        if (line.StartsWith("// entity"))
                        {
                            currentState = LineStatus.Entity;
                        }
                        else
                        {
                            throw new Exception(
                                $"Unexpected token in {mapFilePath} line {lineNumber}");
                        }
                        expect = "{";
                        break;
                    }
                    case LineStatus.Entity:
                    {
                        // End of entity
                        if (line.StartsWith("}"))
                        {
                            currentState = LineStatus.None;

                            if (cache.TryGetValue("classname", out string classname))
                            {
                                if (classname == "misc_gamemodel" && cache.ContainsKey("model"))
                                {
                                    mapFiles.Models.Add(cache["model"]);
                                }

                                if (cache.ContainsKey("shader"))
                                {
                                    // Terrains require special handling and don't have
                                    // required textures/ -prefix
                                    if (cache.TryGetValue("terrain", out string isTerrain) &&
                                        isTerrain.Equalish("1"))
                                    {
                                        mapFiles.Terrains.Add("textures/" + cache["shader"]);
                                    }
                                    else
                                    {
                                        mapFiles.Shaders.Add(cache["shader"]);
                                    }
                                }
                            }
                            else
                            {
                                Log.Warn($"Entity without classname on line {lineNumber}");
                            }


                            cache = new Dictionary<string, string>();

                            continue;
                        }
                        else if (line.StartsWith("// brush"))
                        {
                            currentState = LineStatus.Brush;
                            expect = "{";
                            continue;
                        }
                        else
                        {
                            if (line.StartsWith("\"classname\""))
                            {
                                cache["classname"] = ParseValue(line);
                            }
                            else if (line.StartsWith("\"model\""))
                            {
                                cache["model"] = ParseValue(line);
                            }
                            else if (line.StartsWith("\"noise\"") || line.StartsWith("\"sound\""))
                            {
                                mapFiles.Sounds.Add(ParseValue(line));
                            }
                            else if (line.StartsWith("\"_fog\""))
                            {
                                mapFiles.Shaders.Add(ParseValue(line));
                            }
                            else if (line.StartsWith("\"shader\""))
                            {
                                cache["shader"] = ParseValue(line);
                            }
                            else if (line.StartsWith("\"_remap\""))
                            {
                                mapFiles.Shaders.Add(ParseValue(line).Split(';').Last());
                            }
                            else if (line.StartsWith("\"terrain\""))
                            {
                                cache["terrain"] = ParseValue(line);
                            }
                        }
                        break;
                    }
                    case LineStatus.Brush:
                    {
                        if (line.StartsWith("patchDef2"))
                        {
                            currentState = LineStatus.Patch;
                            expect = "{";
                        }
                        else if (line.StartsWith("}"))
                        {
                            currentState = LineStatus.Entity;
                        }
                        else
                        {
                            mapFiles.Shaders.Add("textures/" + line.GetSplitPart(15));
                        }
                        break;
                    }
                    case LineStatus.Patch:
                    {
                        mapFiles.Shaders.Add(line);
                        readUntil = "}";
                        currentState = LineStatus.Brush;
                        break;
                    }
                }
            }

            return mapFiles;
        }

        private static string ParseValue(string line)
        {
            return line.Substring(line.IndexOf(' ') + 1).Replace("\"", "");
        }
    }
}
