// WARNING!
// Make sure to sync any changes in assets/asm to the folder that VS puts the executable for testing. (or vice versa)

// TODO:
// if a resource fails to load (invalid bytes command, and maybe file not found), add it, but mark it as failed, so that subsequent uses
//   of it don't try to reload it and get the same error over and over...also don't want to say "expected 0 bytes" for a malformed
//   bytes command

global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Text;
global using AsarCLR;


namespace UberASMTool;

public class Program
{
    public static string MainDirectory { get; set; }
    public static readonly int UberMajorVersion = 2;
    public static readonly int UberMinorVersion = 1;

    static bool pause = true;

    private static int Main(string[] args)
    {
        MainDirectory = System.AppContext.BaseDirectory;
        Directory.SetCurrentDirectory(MainDirectory);

        List<string> other = new();
        foreach (string arg in args)
        {
            if (arg == "")
                continue;
            if (arg[0] == '-')
            {
                switch (arg)
                {
                    case "-v":
                        MessageWriter.Write(VerboseLevel.Quiet, $"UberASM Tool version {UberMajorVersion}.{UberMinorVersion}");
                        return 0;
                    case "-p":
                        pause = false;
                        break;
                    case "-q":
                        MessageWriter.Verbosity = VerboseLevel.Quiet;
                        break;
                    default:
                        MessageWriter.Write(VerboseLevel.Quiet, $"Unknown option {arg}.  See the readme for a full list.");
                        Pause();
                        return 1;
                }
            }
            else
            {
                other.Add(arg);
            }
        }

        if (other.Count > 2)
        {
            PrintUsage(VerboseLevel.Quiet);
            Pause();
            return 1;
        }

        // really not sure if this should print out usage when no args are given if everything is okay anyway
        if (other.Count == 0)
            PrintUsage(VerboseLevel.Normal);

        string listFile = (other.Count >= 1) ? other[0] : "list.txt";

        if (!Asar.init())
        {
            MessageWriter.Write(VerboseLevel.Quiet, "Could not initialize or find asar.dll.  Please redownload the program.");
            Pause();
            return 1;
        }

        if (!FileUtils.CreateDirs())
        {
            Pause();
            return 1;
        }

        MessageWriter.Write(VerboseLevel.Normal, "Processing list file...");
        IEnumerable<ConfigStatement> statements = ListParser.ParseList(listFile);
        if (statements == null) { Abort(); return 1; }

        var config = new UberConfig();
        var resourceHandler = new ResourceHandler();
        var lib = new LibraryHandler();
        var rom = new ROM();
        rom.AddDefine("UberMajorVersion", $"{UberMajorVersion}");
        rom.AddDefine("UberMinorVersion", $"{UberMinorVersion}");

        if (!config.ProcessList(statements, resourceHandler, rom)) { Abort(); return 1; }

        string romfile = (other.Count >= 2) ? other[1] : config.ROMFile;
        if (romfile == null)
        {
            MessageWriter.Write(VerboseLevel.Quiet, "No ROM file specified in list file or on command line.");
            Abort();
            return 1;
        }
        if (!rom.Load(romfile)) { Abort(); return 1; }

        MessageWriter.Write(VerboseLevel.Normal, "Cleaning previous runs...");
        if (!rom.Patch("asm/base/clean.asm", null)) { Abort(); return 1; }

        MessageWriter.Write(VerboseLevel.Normal, "Scanning for shared routines...");
        if (!rom.ScanRoutines()) { Abort(); return 1; }

        MessageWriter.Write(VerboseLevel.Normal, "Building library...");
        if (!lib.BuildLibrary(rom)) { Abort(); return 1; }
        if (!lib.GenerateLibraryLabelFile()) { Abort(); return 1; }

        MessageWriter.Write(VerboseLevel.Normal, "Building resources...");
        if (!resourceHandler.BuildResources(config, rom)) { Abort(); return 1; }
        if (!resourceHandler.GenerateResourceLabelFile()) { Abort(); return 1; }

        MessageWriter.Write(VerboseLevel.Normal, "Building main patch...");
        config.AddNMIDefines(rom);
        if (!config.GenerateCallFile()) { Abort(); return 1; }
        if (!GeneratePointerListFile(rom)) { Abort(); return 1; }
        if (!rom.Patch("asm/base/main.asm", null)) { Abort(); return 1; }
        if (!rom.ProcessPrints("asm/base/main.asm", out _, out int mainSize, false)) { Abort(); return 1; }

        if (!rom.Save())
        { 
            FileUtils.DeleteTempFiles();
            Pause();                          // don't want to print the standard "your rom has not been modified" message in this case
            return 1;
        }

        MessageWriter.Write(VerboseLevel.Verbose, $"  Main patch insert size: {mainSize} bytes.");
        MessageWriter.Write(VerboseLevel.Verbose, $"  Library insert size: {lib.Size} bytes.");
        MessageWriter.Write(VerboseLevel.Verbose, $"  Resource insert size: {resourceHandler.Size} bytes.");
        MessageWriter.Write(VerboseLevel.Verbose, $"  Other (routines and prots) insert size: {rom.ExtraSize} bytes.");
        MessageWriter.Write(VerboseLevel.Normal,  $"  Total insert size: {mainSize + lib.Size + resourceHandler.Size + rom.ExtraSize} bytes.");

        MessageWriter.Write(VerboseLevel.Normal, "");
        MessageWriter.Write(VerboseLevel.Normal, "All code inserted successfully.");

        WriteRestoreComment(romfile, $"{UberMajorVersion}.{UberMinorVersion}");
        FileUtils.DeleteTempFiles();

        Pause();
        return 0;
    }

    private static void Abort()
    {
        MessageWriter.Write(VerboseLevel.Quiet, "Some errors occured while running UberASM Tool.  Process aborted.");
        MessageWriter.Write(VerboseLevel.Quiet, "Your ROM has not been modified.");
        FileUtils.DeleteTempFiles();
        Pause();
    }

    private static void Pause()
    {
        if (!pause)
            return;

        Console.Write("Press any key to continue...");

        try
        {
            Console.ReadKey(true);
        }
        catch {	}
    }

    public static void PrintUsage(VerboseLevel verbosity)
    {
        MessageWriter.Write(verbosity, "Usage: UberASMTool [options...] [<list file> [<ROM file>]]");
        MessageWriter.Write(verbosity, "If <list file> is not specified, UberASM Tool will try loading \"list.txt\".");
        MessageWriter.Write(verbosity, "If <ROM file> is not specified, UberASM Tool will search for the one in the list file.");
        MessageWriter.Write(verbosity, "Unless absolute paths are given, the directory relative to the UberASM Tool executable will be used.");
        MessageWriter.Write(verbosity, "See the readme for a full list of valid options.");
        MessageWriter.Write(verbosity, "");
    }

    // this doesn't really fit anywhere else
    // maybe in ROM, but ehh

    private static bool GeneratePointerListFile(ROM rom)
    {
        var output = new StringBuilder();

        foreach (int addr in rom.Cleans)
            output.AppendLine($"dl ${addr:X6}");

        return FileUtils.TryWriteFile("asm/work/pointer_list.asm", output.ToString());
    }

    private static void WriteRestoreComment(string romfile, string ver)
    {
        string extfile = Path.ChangeExtension(romfile, "extmod");
        string addText = $"UberASM Tool v{ver} ";

        try
        {
            string contents = File.Exists(extfile) ? File.ReadAllText(extfile) : "";
            if (contents.EndsWith(addText))
                return;
            File.WriteAllText(extfile, contents + addText);
        }
        catch (Exception e)
        {
            MessageWriter.Write(VerboseLevel.Normal, $"Warning: could not update contents of extmod file: {e.Message}");
        }
    }
}
