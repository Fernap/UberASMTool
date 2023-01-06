using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UberASMTool.Model;

namespace UberASMTool;

class UberConfigProcessor
{
    private Logger logger = Logger.GetLogger();

    public string OverrideROM { get; set; }

    private string listFile;

    private readonly List<Code> codeList = new List<Code>();
    private bool verbose = false;
    private string romPath = null;
    private string globalFile = null;
    private string statusBarFile = null;
    private string macroLibraryFile = null;
    private readonly List<int>[][] list = new List<int>[3][] { new List<int>[512], new List<int>[7], new List<int>[256] };
    private readonly List<int>[] globalList = new List<int>[3];
    private int freeRAM = 0;

    private IEnumerable<ConfigStatement> statements = null;

    public string GetLogs()
    {
        return logger.GetOutput();
    }

    public UberConfig Build()
    {
        return new UberConfig()
        {
            VerboseMode = verbose,
            ROMPath = OverrideROM ?? romPath,
            GlobalFile = globalFile,
            StatusBarFile = statusBarFile,
            MacroLibraryFile = macroLibraryFile,
            FileASMList = list.Select(c => c.Select(d => d?.ToArray()).ToArray()).ToArray(),
            FreeRAM = freeRAM,
            CodeList = codeList.ToArray(),
        };
    }

    public void LoadListFile(string path)
    {
        listFile = File.ReadAllText(path);
        // Alternatively, File.ReadAllLines(path);
        // then strip comments,
        // then .Join("\n");
    }

    public bool ParseList()
    {
        statements = ListParser.ParseList(listFile);
        return (statements != null);
    }

    public bool ProcessList()
    {
        var resourceMode = ResourceMode.None;
        bool valid = true;

        foreach (ConfigStatement statement in statements)
        {
            switch (statement)
            {
                case VerboseStatement s:
                    verbose = s.IsOn;
                    break;

                case FileStatement s when s.Type == FileType.Global:
                    if (globalFile != null)
                    {
                        // error, file already given
                        valid = false;
                    }
                    else
                        globalFile = s.Filename;
                    break;
                    // these should probably all be one...file names stored in an array indexed by the filetype enum

                 // etc

                case FreeramStatement s:
                    if (freeRAM != 0)
                    {
                        // error: already defined
                        valid = false;
                    }
                    else if ((freeRAM >= 0x7E0000 && freeRAM <= 0x7FFFFF) || (freeRAM >= 0x400000 && freeRAM <= 0x41FFFF))
                        freeRAM = s.Addr;
                    else
                    {
                        // error: invalid ram address -- maybe allow low ram mirror
                        valid = false;
                    }
                    break;

                case ModeStatement s:
                    resourceMode = s.Mode;
                    break;

                case ResourceStatement s:
                    if (resourceMode == ResourceMode.None)
                    {
                        // error: no mode currently selected
                        valid = false;
                    }

                    // if this resource num already had resources, error
                    // if any files are duplicated, error (ehh)
                    // otherwise
                    // loop over files given, prepend appropriate path...if file doesn't exist, error
                    // if file hasn't been used yet
                    //    process file (read ;>dbr and ;>bytes) and add it to the resource pool, and keep the new index
                    // else
                    //    grab the index of this file
                    // if ;>bytes doesn't match with given bytes, error

                    break;

                default:
                    throw new Exception("Invalid config statement type.  This should never happen; please report this as a bug.");
            }
        }

        // ditto for the other files...putting these in an array would simplify things
        if (globalFile == null)
        {
            // error no global file given
            valid = false;
        }

        if (freeRAM == 0)
        {
            // error: no free ram addr specified
            valid = false;
        }

        return valid;
    }


// ---------------------------------------
        public bool ParseList()
        {
            // 0 = level, 1 = ow, 2 = gamemode
            int mode = -1;

            for (int i = 0; i < listFile.Length; ++i)
            {
                string line = listFile[i].Trim();

                if (line.StartsWith(";") || line.StartsWith("#") || line == "")
                {
                    continue;
                }

                string lw = line.ToLower();

                switch (lw)
                {
                    case "level:": mode = 0; continue;
                    case "overworld:": mode = 1; continue;
                    case "gamemode:": mode = 2; continue;
                }

                string[] split = line.Split(new string[] { " ", "\t" }, StringSplitOptions.RemoveEmptyEntries);

                if (split.Length < 2)
                {
                    logger.Error("Missing file name or command.", i);
                    return false;
                }

                split[0] = split[0].ToLower();

                string value = String.Join(" ", split, 1, split.Length - 1);

                int index = value.IndexOfAny(new char[] { ';', '#' });

                if (index != -1)
                {
                    value = value.Substring(0, index).Trim();
                }

                string valueHex = value;

                if (valueHex.StartsWith("$"))
                {
                    valueHex = valueHex.Substring(1);
                }
                else if (valueHex.StartsWith("0x"))
                {
                    valueHex = valueHex.Substring(2);
                }

                switch (split[0])
                {
                    case "verbose:":
                        verbose = value.ToLower() == "on";
                        continue;

                    case "rom:":
                        if (!ParseGlobalFileDeclaration(ref romPath, "ROM", value, i))
                            return false;
                        continue;

                    case "macrolib:":
                        if (!ParseGlobalFileDeclaration(ref macroLibraryFile, "Macro Library", value, i))
                            return false;
                        continue;

                    case "global:":
                        if (!ParseGlobalFileDeclaration(ref globalFile, "Global ASM", value, i)) return false;
                        continue;

                    case "statusbar:":
                        if (!ParseGlobalFileDeclaration(ref statusBarFile, "Status Bar ASM", value, i)) return false;
                        continue;

                    case "freeram:":
                        if (!ParseHexDefineDeclaration(ref freeRAM, "Free RAM address", valueHex, i)) return false;
                        continue;
                }

                int hexValue;

                try
                {
                    hexValue = Convert.ToUInt16(split[0], 16);
                }
                catch
                {
                    logger.Error("invalid hex number.", i);
                    return false;
                }

                switch (mode)
                {
                    case -1:
                        logger.Error("unspecified code type (level/overworld/gamemode).", i);
                        return false;

                    // level
                    case 0:
                        if (hexValue > 0x1FF)
                        {
                            logger.Error("level out of range (000 - 1FF).", i);
                            return false;
                        }
                        break;

                    // overworld
                    case 1:
                        if (hexValue > 6)
                        {
                            logger.Error("overworld number out of range (0-6).", i);
                            return false;
                        }
                        break;

                    // game mode
                    case 2:
                        if (hexValue > 0xFF)
                        {
                            logger.Error("game mode number out of range (00 - FF).", i);
                            return false;
                        }
                        break;
                }

                try
                {
                    AddLevelCode(value, hexValue, mode);
                    continue;
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message, i);
                    return false;
                }
            }

            return ValidateDefinitions();
        }

        private bool ParseHexDefineDeclaration(ref int defineDestination, string defineType, string valueHex, int i)
        {
            if (defineDestination == 0)
            {
                try
                {
                    defineDestination = Convert.ToInt32(valueHex, 16);
                    return true;
                }
                catch
                {
                    logger.Error($"invalid {defineType} hex number.", i);
                    return false;
                }
            }
            else
            {
                logger.Warning($"{defineType} was already defined.", i);
                return true;
            }
        }

        private bool ParseGlobalFileDeclaration(ref string fileDefinition, string fileType, string value, int i)
        {
            if (fileDefinition == null)
            {
                if (File.Exists(value))
                {
                    fileDefinition = value;
                    return true;
                }
                else
                {
                    logger.Error("file does not exist.", i);
                    return false;
                }
            }
            else
            {
                logger.Warning($"{fileType} file was already defined, new define ignored.", i);
                return true;
            }
        }

        private bool ValidateDefinitions()
        {
            if (macroLibraryFile == null)
            {
                logger.Error("macro library file was not defined.");
                return false;
            }
            if (statusBarFile == null)
            {
                logger.Error("status bar file was not defined.");
                return false;
            }
            if (globalFile == null)
            {
                logger.Error("global file was not defined.");
                return false;
            }
            return true;
        }

        private void AddLevelCode(string path, int level, int type)
        {
            List<int> currentList;

            if (list[type][level] == null)
            {
                currentList = list[type][level] = new List<int>();
            }
            else
            {
                currentList = list[type][level];
            }

            // TO DO: use hashes or anything better than path matching.
            int codeIdentifier = codeList.FindIndex(x => x.Path == path);

            if (codeIdentifier == -1)
            {
                // add new ASM file
                codeIdentifier = codeList.Count;
                codeList.Add(new Code(path));
            }

            currentList.Add(codeIdentifier);
        }
    }
