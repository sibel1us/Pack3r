using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfReleaser.General;
using WolfReleaser.Objects;

namespace WolfReleaser.Parsers
{
    public class MapParser : BaseParser<Map>
    {
        private enum LineStatus
        {
            None, Entity, Brush, Patch
        }

        public MapParser(string path)
        {
            this.filepath = path;
            this.lines = File.ReadAllLines(path);
        }

        public override Map Parse()
        {
            if (!File.Exists(filepath))
            {
                Log.Fatal($"Map file not found: '{filepath}'");
                return null;
            }

            var map = new Map
            {
                Name = Path.GetFileNameWithoutExtension(filepath),
                FullPath = filepath,
                Shaders = new HashSet<string>(),
                Models = new HashSet<string>(),
                Sounds = new HashSet<string>(),
                Terrains = new HashSet<string>()
            };

            LineStatus currentState = LineStatus.None;
            string expect = null;
            string readUntil = null;

            var cache = new Dictionary<string, string>();

            Log.Debug($"Reading {lines.Length} lines in map {map.Name}");

            foreach ((string line, int lineNumber) in lines.Clean())
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
                        Log.Error($"Expecting {expect}, got {line} " +
                            $"in {filepath} line {lineNumber}");
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
                                $"Unexpected token in {filepath} line {lineNumber}");
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
                                    map.Models.Add(cache["model"]);
                                }

                                if (cache.ContainsKey("shader"))
                                {
                                    // Terrains require special handling and don't have
                                    // required textures/ -prefix
                                    if (cache.TryGetValue("terrain", out string isTerrain) &&
                                        isTerrain.Equalish("1"))
                                    {
                                        map.Terrains.Add("textures/" + cache["shader"]);
                                    }
                                    else
                                    {
                                        map.Shaders.Add(cache["shader"]);
                                    }
                                }
                            }
                            else
                            {
                                Log.Warn($"Entity without classname, line {lineNumber}");
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
                            else if (line.StartsWith("\"model2\""))
                            {
                                map.Models.Add(ParseValue(line));
                            }
                            else if (line.StartsWith("\"noise\"") || line.StartsWith("\"sound\""))
                            {
                                map.Sounds.Add(ParseValue(line));
                            }
                            else if (line.StartsWith("\"_fog\""))
                            {
                                map.Shaders.Add(ParseValue(line));
                            }
                            else if (line.StartsWith("\"shader\""))
                            {
                                cache["shader"] = ParseValue(line);
                            }
                            else if (line.StartsWith("\"_remap"))
                            {
                                // Remaps can have suffix (_remap1, _remap2)
                                map.Shaders.Add(ParseValue(line).Split(';').Last());
                            }
                            else if (line.StartsWith("\"terrain\""))
                            {
                                cache["terrain"] = ParseValue(line);
                            }
                            // TODO: skins are not supported yet!
                            //else if (line.StartsWith("\"skin\""))
                            //{
                            //    map.Models.Add(ParseValue(line));
                            //}
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
                            map.Shaders.Add("textures/" + line.GetSplitPart(15));
                        }
                        break;
                    }
                    case LineStatus.Patch:
                    {
                        map.Shaders.Add("textures/" + line);
                        readUntil = "}";
                        currentState = LineStatus.Brush;
                        break;
                    }
                }
            }

            Log.Debug(string.Format(
                "Found {0} shaders/terrains, {1} gamemodels, {2} sounds, in map '{4}'",
                map.Shaders.Count,
                map.Models.Count,
                map.Sounds.Count,
                map.Terrains.Count,
                map.Name));

            return map;
        }

        private string ParseValue(string line)
        {
            return line.Substring(line.IndexOf(' ') + 1).Replace("\"", "");
        }
    }
}
