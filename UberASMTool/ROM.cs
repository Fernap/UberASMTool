namespace UberASMTool;

using AsarCLR;

public class ROM
{
    public int ExtraSize { get; private set; } = 0;      // running extra insert size -- prots and shared routines
    public List<int> Cleans { get; private set; } = new();
    public bool Deprecations { get; set; } = false;

    private byte[] romData;        // contains the actual ROM data (without the header if there is one)
    private byte[] headerData;     // stays null if ROM is headerless
    private string romPath;
    private Dictionary<string, string> defines = new();
    private HashSet<string> routines = new();

    // Add a define -- not implementing a way to remove, and not checking for duplicate keys
    // don't really need anything fancier
    public void AddDefine(string define, string value)
    {
        MessageWriter.Write(VerboseLevel.Debug, $"Define added: !{define} = {value}");
        defines[define] = value;
    }

    public bool ScanRoutines()
    {
        string[] files;
        try
        {
            files = Directory.GetFiles("routines", "*.asm");
        }
        catch
        {
            MessageWriter.Write(VerboseLevel.Quiet, "Could not read contents of routines/ directory.");
            return false;
        }

        foreach (string file in files)
            routines.Add(Path.GetFileNameWithoutExtension(file));

        return true;
    }

    public bool AddRoutine(string filename, string name, int location)
    {
        if (!routines.Contains(name))
        {
            MessageWriter.Write(VerboseLevel.Quiet, $"Error: {filename}: Unknown shared routine \"{name}\".");
            return false;
        }    

        AddDefine($"UberRoutine_{name}", $"${location:X6}");
        return true;
    }

    public bool Load(string filename)
    {
        byte[] fileData;

        romPath = filename;

        if (!File.Exists(romPath))
        {
            MessageWriter.Write(VerboseLevel.Quiet, $"Error: ROM file \"{romPath}\" not found.");
            return false;
        }

        try
        {
            fileData = File.ReadAllBytes(romPath);
        }
        catch (Exception e)
        {
            MessageWriter.Write(VerboseLevel.Quiet, $"Error reading ROM file \"{romPath}\": {e.Message}");
            return false;
        }

        int romSize = fileData.Length;
        if (romSize % 0x8000 == 512)
        {
            romSize -= 512;
            headerData = new byte[512];
            romData = new byte[romSize];
            Array.Copy(fileData, headerData, 512);
            Array.Copy(fileData, 512, romData, 0, romSize);
        }
        else
            romData = fileData;

        if (romSize > 8 * 1024 * 1024)
        {
            MessageWriter.Write(VerboseLevel.Quiet, "ROM file too large.");
            return false;
        }
        if (romSize < 1024 * 1024)
        {
            MessageWriter.Write(VerboseLevel.Quiet, "ROM file too small.  It must be an SMW ROM expanded to at least 1 MB.");
            return false;
        }

        // 21 bytes long
        //            123456789012345678901
        string smw = "SUPER MARIOWORLD     ";
        for (int i = 0; i < smw.Length; i++)
            if (romData[0x7FC0 + i] != smw[i])
            {
                MessageWriter.Write(VerboseLevel.Quiet, "ROM file does not appear to be a valid Super Mario World ROM.");
                return false;
            }

        return true;
    }

    public bool Save()
    {
        try
        {
            using FileStream fs = new FileStream(romPath, FileMode.Create);
            if (headerData != null)
                fs.Write(headerData, 0, headerData.Length);
            fs.Write(romData, 0, romData.Length);
        }
        catch (Exception e)
        {
            MessageWriter.Write(VerboseLevel.Quiet, $"Error saving ROM file: {e.Message}");
            MessageWriter.Write(VerboseLevel.Quiet, $"Please double check the intergrity of your ROM.");
            return false;
        }

        return true;
    }


    // asmfile is relative to the main directory
    public bool Patch(string asmfile, Dictionary<string, string> extraDefines)
    {
        Dictionary<string, string> allDefines;

        // this will throw an exception if there's a duplicate key
        // shouldn't happen currently, but this may or may not be preferable to letting extraDefines override defines
        if (extraDefines == null)
            allDefines = defines;
        else
            allDefines = defines.Concat(extraDefines).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);   // ew

        var WarnSettings = new Dictionary<string, bool>
        {
            ["Wfeature_deprecated"] = Deprecations
        };

        // passing an empty path list; this should be fine since files aren't being moved before assembly
        bool status = Asar.patch(Path.Combine(Program.MainDirectory, asmfile),
                                 ref romData,
                                 null,
                                 true,
                                 allDefines,
                                 null,           // std include file
                                 null,           // std define file
                                 WarnSettings);

        foreach (Asarerror error in Asar.getwarnings().Concat(Asar.geterrors()))
            MessageWriter.Write(VerboseLevel.Quiet, $"  {error.Fullerrdata}");

        return status;
    }


    public bool ProcessPrints(string filename, out int startAddr, out int size, bool allowNested)
    {
        bool started = false;
        bool ended = false;
        size = 0;
        startAddr = 0;
        Stack<int> starts = new();

        foreach (string print in Asar.getprints())
        {
            if (ended)
            {
                MessageWriter.Write(VerboseLevel.Quiet, $"  Error: {filename}: Unexpected print after _endl command.");
                return false;
            }

            string trimmed = print.Trim();        // Not sure if asar prints include newlines, but the Trim() will get rid of them if so
            string command = trimmed;
            int? value = null;
            int space = print.IndexOf(' ');
            if (space > 0)
            {
                command = print.Substring(0, space);
                try
                {
                    value = Convert.ToInt32(print.Substring(space + 1), 16);
                }
                catch { }
            }

            if (!started && command != "_startl")
            {
                MessageWriter.Write(VerboseLevel.Quiet, $"  Error: {filename}: Unexpected print before _startl command.");
                return false;
            }

            switch (command)
            {
                case "_startl":
                    if (value == null)
                    {
                        MessageWriter.Write(VerboseLevel.Quiet, $"  Error: {filename}: Invalid value in _startl command.");
                        return false;
                    }

                    // this is iffy, but we don't want to add the start address to the clean list for the main patch, which disallows nesting
                    if (allowNested)
                        Cleans.Add(value.Value);

                    if (!started)
                    {
                        started = true;
                        startAddr = value.Value;
                    }
                    else
                    {
                        if (!allowNested)
                        {
                            MessageWriter.Write(VerboseLevel.Quiet, $"  Error: {filename}: Invalid nested _startl command.  This is most likely from using an uncalled shared routine or a %prot macro in the global code or statusbar files");
                            return false;
                        }
                        else
                        {
                            starts.Push(value.Value);
                        }
                    }

                    break;

                case "_endl":
                    if (value == null)
                    {
                        MessageWriter.Write(VerboseLevel.Quiet, $"  Error: {filename}: Invalid value in _endl command.");
                        return false;
                    }

                    int tmpStart;
                    int tmpEnd = value.Value;
                    if (starts.Count == 0)
                    {
                        ended = true;
                        tmpStart = startAddr;
                        size = tmpEnd - tmpStart + 8;
                    }
                    else
                        tmpStart = starts.Pop();

                    if (tmpStart == tmpEnd)
                    {
                        MessageWriter.Write(VerboseLevel.Quiet, $"  Error: {filename}: Zero insert size.");
                        return false;
                    }
                    if (tmpStart > tmpEnd)
                    {
                        MessageWriter.Write(VerboseLevel.Quiet, $"  Error: {filename}: Negative insert size.");
                        return false;
                    }

                    if (!ended)
                    {
                        MessageWriter.Write(VerboseLevel.Debug, $"  {filename}: routine/prot added, {tmpEnd - tmpStart + 8} bytes");
                        ExtraSize += tmpEnd - tmpStart + 8;
                    }

                    break;

                default:
                    MessageWriter.Write(VerboseLevel.Normal, print);
                    break;
            }
        }

        if (!ended)
        {
            MessageWriter.Write(VerboseLevel.Quiet, $"  {filename}: Missing _endl command.");
            return false;
        }

        return true;
    }
}
