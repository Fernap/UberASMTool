// TODO: be smarter about reading/writing to the actual ROM file

namespace UberASMTool;

using AsarCLR;

//    public struct Asarlabel
//    {
//        public string Name;
//        public int Location;
//    }

public class ROM
{
    private byte[] romData;        // contains the actual ROM data (without the header if there is one)
    private byte[] headerData;     // stays null if ROM is headerless
    private string romPath;
    private int romSize;
    private Dictionary<string, string> defines = new();

    // Add a define -- not implementing a way to remove, and not checking for duplicate keys
    // don't really need anything fancier
    public void AddDefine(string define, string value)
    {
        defines[define] = value;
    }

    public bool Load(string filename)
    {
        byte[] fileData;

        romPath = filename;

        if (!File.Exists(romPath))
        {
            MessageWriter.Write(true, $"Error: ROM file \"{romPath}\" not found.");
            return false;
        }

        try
        {
            fileData = File.ReadAllBytes(romPath);
        }
        catch (Exception e)
        {
            MessageWriter.Write(true, $"Error reading ROM file \"{romPath}\": {e.Message}");
            return false;
        }

        romSize = fileData.Length;
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
            MessageWriter.Write(true, "ROM file too large.");
            return false;
        }
        if (romSize < 1024 * 1024)
        {
            MessageWriter.Write(true, "ROM file too small.  It must be an SMW ROM expanded to at least 1 MB.");
            return false;
        }

        // 21 bytes long
        //            123456789012345678901
        string smw = "SUPER MARIOWORLD     ";
        for (int i = 0; i < smw.Length; i++)
            if (romData[0x7FC0 + i] != smw[i])
            {
                MessageWriter.Write(true, "ROM file does not appear to be a valid Super Mario World ROM.");
                return false;
            }

        return true;
    }

    // TODO: have this just write directly from romData and headerData instead of copying to a separate buffer
    public bool Save()
    {
        byte[] final = new byte[romSize + ((headerData != null) ? 512 : 0)];

        headerData?.CopyTo(final, 0);
        romData.CopyTo(final, headerData == null ? 0 : 512);

        try
        {
            File.WriteAllBytes(romPath, final);
        }
        catch (Exception e)
        {
            MessageWriter.Write(true, $"Error saving ROM file: {e.Message}");
            MessageWriter.Write(true, $"Please double check the intergrity of your ROM.");
            return false;
        }

        return true;
    }


    //        public static bool patch(string patchLocation, ref byte[] romData, string[] includePaths = null,
    //            bool shouldReset = true, Dictionary<string, string> additionalDefines = null,
    //            string stdIncludeFile = null, string stdDefineFile = null)

    // starting out with empty path list...shouldn't really be needed, but can add if needed
    // asmfile is relative to the main directory
    public bool Patch(string asmfile)
    {
        bool status = Asar.patch(Program.MainDirectory + asmfile, ref romData, null, true, defines);

        foreach (Asarerror error in Asar.getwarnings().Concat(Asar.geterrors()))
            MessageWriter.Write(true, $"  {error.Fullerrdata}");

        return status;
    }


    public bool ProcessPrints(string filename, out int startAddr, out int endAddr, bool allowProts)
    {
        bool startl = false;
        bool endl = false;
        startAddr = 0;
        endAddr = 0;

        foreach (string print in Asar.getprints())
        {
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

            if (!startl && command != "_startl")
            {
                MessageWriter.Write(true, $"  {filename}: error: unexpected print before _startl command.");
                return false;
            }

            if (endl)
            {
                MessageWriter.Write(true, $"  {filename}: error: unexpected print after _endl command.");
                return false;
            }

            switch (command)
            {
                case "_startl":
                    if (value == null)
                    {
                        MessageWriter.Write(true, $"  {filename}: error: invalid value in _startl command.");
                        return false;
                    }
                    startl = true;
                    startAddr = value.Value;
                    break;

                case "_endl":
                    if (value == null)
                    {
                        MessageWriter.Write(true, $"  {filename}: error: invalid value in _endl command.");
                        return false;
                    }
                    endl = true;
                    endAddr = value.Value;
                    break;

                case "_prot":
                    if (!allowProts)
                    {
                        MessageWriter.Write(true, $"  Invalid use of _prot command.  This is most likely from using %prot_file() or %prot_source() in the global code or status bar files, which is not allowed.");
                        return false;
                    }
                    if (value == null)
                    {
                        MessageWriter.Write(true, $"  {filename}: error: invalid value in _prot command.");
                        return false;
                    }
                    Program.ProtPointers.Add(value.Value);
                    break;

                default:
                    MessageWriter.Write(true, print);
                    break;
            }
        }

        if (!endl)
        {
            MessageWriter.Write(true, $"  {filename}: error: missing _endl command.");
            return false;
        }

        if (startAddr == endAddr)
        {
            MessageWriter.Write(true, $"  {filename}: error: empty assembled file.");
            return false;
        }

        return true;
    }
}
