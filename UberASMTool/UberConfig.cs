// FIX: need a way to determine if two strings point to the same resource...it does handle interspersed ".." and "."s now, but not case differences
// TODO: the variable naming in the resource statement handling is pretty gross
// TODO: passing the config statement to AddResource() just so I can pass it along to ListError() is gross

namespace UberASMTool;

// this should maybe go elsewhere, but leaving here for now:
public enum FileType { Global, Statusbar, Macrolib, ROM };

// really need a common term for "level", "gamemode", and "overworld"
// uberasm execution context?

// keeps config info about each individual level/gm/ow
// make this a struct?
public class ConfigInfo
{
    public List<ResourceCall> Calls { get; set; } = new();
    public bool HasNMI { get; set; } = false;
}

public static class UberConfig
{
    public static string ROMFile { get; set; } = null;
    public static ConfigInfo[] LevelConfigs { get; } = new ConfigInfo[512];
    public static ConfigInfo[] GamemodeConfigs { get; } = new ConfigInfo[256];
    public static ConfigInfo[] OverworldConfigs { get; } = new ConfigInfo[7];
    public static ConfigInfo AllLevelConfig { get; } = new();
    public static ConfigInfo AllGamemodeConfig { get; } = new();
    public static ConfigInfo AllOverworldConfig { get; } = new();

    static UberConfig()
    {
        int i;

        for (i = 0; i < 512; i++)
            LevelConfigs[i] = new ConfigInfo();
        for (i = 0; i < 256; i++)
            GamemodeConfigs[i] = new ConfigInfo();
        for (i = 0; i < 7; i++)
            OverworldConfigs[i] = new ConfigInfo();
    }

    public static bool ProcessList(IEnumerable<ConfigStatement> statements)
    {
        bool globalFile = false;
        bool statusbarFile = false;
        bool macrolibFile = false;
        bool freeRAM = false;
        var currentMode = ResourceType.None;
        bool valid = true;

        foreach (ConfigStatement statement in statements)
        {
            switch (statement)
            {
                case VerboseStatement s:
                    MessageWriter.VerboseMode = s.IsOn;
                    break;

                case ModeStatement s:
                    currentMode = s.Mode;
                    break;

                case FileStatement s when s.Type == FileType.Global:
                    valid &= CheckFileStatement(ref globalFile, s, "GlobalCodeFile", "Global code file");
                    break;

                case FileStatement s when s.Type == FileType.Statusbar:
                    valid &= CheckFileStatement(ref statusbarFile, s, "StatusbarCodeFile", "Statusbar code file");
                    break;

                case FileStatement s when s.Type == FileType.Macrolib:
                    valid &= CheckFileStatement(ref macrolibFile, s, "MacrolibFile", "Macro library file");
                    break;

                // warn if overridden on command line?
                case FileStatement s when s.Type == FileType.ROM:
                    if (ROMFile != null)
                    {
                        ListError(s, "ROM file already specified.");
                        valid = false;
                    }
                    else
                        ROMFile = s.Filename;
                    break;

                case FreeramStatement s:
                    if (freeRAM)
                    {
                        ListError(s, "Freeram statement already specified.");
                        valid = false;
                    }
                    else
                    {
                        freeRAM = true;
                        ROM.AddDefine("UberFreeram", $"${s.Addr:X6}");
                        if (!(s.Addr >= 0x7E0000 && s.Addr <= 0x7FFFFF) || (s.Addr >= 0x400000 && s.Addr <= 0x41FFFF))
                        {
                            ListError(s, "Freeram address must be in the range 7E0000-7FFFFF or 400000-41FFFF.");
                            valid = false;
                        }
                    }
                    break;

                case ResourceStatement s:
                    bool validResource = true;      // bleh
                    List<ResourceCall> calls = null;       // the list of calls for this level/gm/ow, to be populated
                    switch (currentMode)
                    {
                        case ResourceType.None:
                            ListError(s, "Unspecified resource type (level/overworld/gamemode).");
                            validResource = false;
                            break;

                        case ResourceType.Level:
                            validResource = SetupResourceStatement(s, 0x1FF, "Level", LevelConfigs, AllLevelConfig, ref calls);
                            break;

                        case ResourceType.Gamemode:
                            validResource = SetupResourceStatement(s, 0xFF, "Gamemode", GamemodeConfigs, AllGamemodeConfig, ref calls);
                            break;

                        case ResourceType.Overworld:
                            validResource = SetupResourceStatement(s, 6, "Overworld", OverworldConfigs, AllOverworldConfig, ref calls);
                            break;
                    }
                    if (!validResource)
                    {
                        valid = false;
                        break;
                    }

                    // the list of calls from the information in list.txt
                    foreach (ResourceCall call in s.Calls)
                    {
                        // TODO: write this generically so the code here doesn't have to care what resource types exist
                        string dir = currentMode switch
                        {
                            ResourceType.Level => "level",
                            ResourceType.Gamemode => "gamemode",
                            ResourceType.Overworld => "overworld",
                            _ => throw new Exception("Unexpected resource type")
                        };
                        string file = Path.GetFullPath(Path.Combine(Program.MainDirectory, dir, call.File)).Replace("\\", "/");
                        Console.WriteLine(file);

                        if (!GetOrAddResource(file, out ResourceInfo resource, s))
                        {
                            valid = false;
                            break;
                        }

                        if (call.Bytes.Count != resource.Bytes)
                        {
                            ListError(s, $"Incorrect number of extra bytes supplied: Expected {resource.Bytes}, but found {call.Bytes.Count}");
                            valid = false;
                            break;
                        }

                        if (calls.Any(x => x.File == file))
                        {
                            ListError(s, "Duplicate resource specified.");
                            valid = false;
                            break;
                        }

                        var newcall = new ResourceCall { File = file, Bytes = call.Bytes };
                        calls.Add(newcall);
                    }
                    break;
            }
        }

        valid &= CheckCommandSpecified(globalFile, "global:");
        valid &= CheckCommandSpecified(statusbarFile, "statusbar:");
        valid &= CheckCommandSpecified(macrolibFile, "macrolib:");
        valid &= CheckCommandSpecified(freeRAM, "freeram:");

        return valid;
    }

    private static bool CheckFileStatement(ref bool already, FileStatement s, string define, string desc)
    {
        if (already)
        {
            ListError(s, $"{desc} already specified.");
            return false;
        }

        already = true;
        string path = Path.Combine(Program.MainDirectory, s.Filename).Replace("\\", "/");      // to make asar happy
        if (!File.Exists(path))
        {
            ListError(s, $"File \"{path}\" not found.");
            return false;
        }
        ROM.AddDefine(define, path);
        return true;
    }

    private static bool CheckCommandSpecified(bool val, string cmd)
    {
        if (!val)
            MessageWriter.Write(true, $"Error: no \"{cmd}\" command found.");
        return val;
    }

    private static bool SetupResourceStatement(ResourceStatement s, int max, string type,
        ConfigInfo[] singles, ConfigInfo all, ref List<ResourceCall> calls)
    {
        if (s.Number == -1)
            calls = all.Calls;
        else
        {
            if (s.Number < 0 || s.Number > max)
            {
                ListError(s, $"{type} out of range (0 - {max:X}).");
                return false;
            }
            calls = singles[s.Number].Calls;
        }

        if (calls.Count > 0)
        {
            string numstr = s.Number == -1 ? "\"*\"" : $"{s.Number:X6}";
            ListError(s, $"Resource(s) for {type.ToLower()} {numstr} already specified.");
            return false;
        }

        return true;
    }

    // This would make more sense in ResourceHandler, but leaving here so it can call ListError() (poorly)
    private static bool GetOrAddResource(string file, out ResourceInfo resource, ConfigStatement s)
    {
        if (ResourceHandler.Resources.ContainsKey(file))
        {
            resource = ResourceHandler.Resources[file];
            return true;
        }

        resource = new ResourceInfo { ID = ResourceHandler.Resources.Count };
        ResourceHandler.Resources[file] = resource;
        string[] lines;
        try
        {
            lines = File.ReadAllLines(file);
        }
        catch
        {
            ListError(s, $"Could not read \"{file}\"");
            return false;
        }

        // TODO: More robust handling of resource commands
        if (Array.FindIndex(lines, x => x == ";>dbr off") >= 0)
            resource.SetDBR = false;

        string line = Array.Find(lines, x => x.StartsWith(";>bytes "));
        if (line != null)
        {
            int bytes;
            try
            {
                bytes = Convert.ToInt32(line.Substring(8));
            }
            catch
            {
                MessageWriter.Write(true, $"Invalid number in \">bytes\" command in \"{file}\".");
                return false;
            }
            if (bytes < 0 || bytes > 255)
            {
                MessageWriter.Write(true, $"Invalid value in \">bytes\" command in \"{file}\" (must be 0 - 255).");
                return false;
            }
            resource.Bytes = bytes;
        }

        return true;
    }

    public static void AddNMIDefines()
    {
        bool any = DoType(AllLevelConfig, LevelConfigs, "Level") |
                   DoType(AllGamemodeConfig, GamemodeConfigs, "Gamemode") |
                   DoType(AllOverworldConfig, OverworldConfigs, "Overworld");
        ROM.AddDefine("UberUseNMI", any ? "1" : "0");
        return;

        bool DoType(ConfigInfo all, ConfigInfo[] singles, string name)
        {
            bool normalNMI = false;

            foreach (ResourceCall call in all.Calls)
                if (ResourceHandler.Resources[call.File].HasNMI)
                {
                    all.HasNMI = true;
                    break;
                }

            foreach (ConfigInfo info in singles)
                foreach (ResourceCall call in info.Calls)
                    if (ResourceHandler.Resources[call.File].HasNMI)
                    {
                        normalNMI = true;      // at least one level/ow/gm has nmi
                        info.HasNMI = true;    // *this* level/ow/gm has nmi
                        break;                 // now break out of the inner loop and check the next
                    }

            ROM.AddDefine($"Uber{name}NMIAll", all.HasNMI ? "1" : "0");
            ROM.AddDefine($"Uber{name}NMINormal", normalNMI ? "1" : "0");
            ROM.AddDefine($"Uber{name}NMI", all.HasNMI || normalNMI ? "1" : "0");

            return all.HasNMI || normalNMI;
        }
    }

    private static void ListError(ConfigStatement statement, string msg) =>
        MessageWriter.Write(true, $"Error on line {statement.Line}: {msg}");
}
