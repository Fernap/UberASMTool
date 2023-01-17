// WARNING!
// Make sure to sync any changes in assets/asm to the folder that VS puts the executable for testing. (or vice versa)

// TODO:
// I may not need to bail out at the first sign of an error...look into how far I can push until that's really needed
// make the naming of NMI labels consistent in the patches
// optimize level/ow/gm call code for situations where none are being called
// Put something in empty folders so they actually go to github, even just readmes
// rethink how %prot() macros work in terms of directories (currently it's relative to parent of macrolib file, with no way to specify absolute path)
// if a resource fails to load (invalid bytes command, and maybe file not found), add it, but mark it as failed, so that subsequent uses
//   of it don't try to reload it and get the same error over and over...also don't want to say "expected 0 bytes" for a malformed
//   bytes command
// acount for prot files in insert sizes

global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Text;
global using AsarCLR;


namespace UberASMTool;

public class Program
{
    public static string MainDirectory { get; set; }      // this should probably go elsewhere
    public const int UberMajorVersion = 2;                           // put these somewhere better..take from solution settings?
    public const int UberMinorVersion = 0;

    private static int Main(string[] args)
    {
        Directory.SetCurrentDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
        MainDirectory = Environment.CurrentDirectory;

        if (!Asar.init())
        {
            MessageWriter.Write(VerboseLevel.Quiet, "Could not initialize or find asar.dll.  Please redownload the program.");
            Pause();
            return 1;
        }

        // this should respect quiet mode when no args given if everything else is otherwise ok
        // really not sure if this should print out usage when no args are given if everything is okay anyway
        if (args.Length == 0 || args.Length > 2)
            PrintUsage();

        if (args.Length > 2)
        {
            Pause();
            return 1;
        }

        string listFile = (args.Length >= 1) ? args[0] : "list.txt";

        // want to print this as "normal" priority, but it should also respect quiet mode, which we don't know
        // yet because it's in the list file.  Need a command line option to force it
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

        string romfile = (args.Length >= 2) ? args[2] : config.ROMFile;
        if (romfile == null)
        {
            MessageWriter.Write(VerboseLevel.Quiet, "No ROM file specified in list file or on command line.");
            Abort();
            return 1;
        }
        if (!rom.Load(romfile)) { Abort(); return 1; }

        MessageWriter.Write(VerboseLevel.Normal, "Cleaning previous runs...");
        if (!rom.Patch("asm/base/clean.asm")) { Abort(); return 1; }

        MessageWriter.Write(VerboseLevel.Normal, "Building library...");
        if (!lib.BuildLibrary(rom)) { Abort(); return 1; }
        if (!lib.GenerateLibraryLabelFile()) { Abort(); return 1; }

        MessageWriter.Write(VerboseLevel.Normal, "Building resources...");
        if (!resourceHandler.BuildResources(config, rom)) { Abort(); return 1; }
        if (!resourceHandler.GenerateResourceLabelFile()) { Abort(); return 1; }

        MessageWriter.Write(VerboseLevel.Normal, "Building main patch...");
        config.AddNMIDefines(rom);
        if (!config.GenerateCallFile()) { Abort(); return 1; }
        if (!GeneratePointerListFile(lib, resourceHandler)) { Abort(); return 1; }
        if (!rom.Patch("asm/base/main.asm")) { Abort(); return 1; }
        if (!rom.ProcessPrints("asm/base/main.asm", out int start, out int end, null)) { Abort(); return 1; }
        int mainSize = end - start + 8;

        if (!rom.Save())
        { 
            FileUtils.DeleteTempFiles();
            Pause();                          // don't want to print the standard "your rom has not been modified" message in this case
            return 1;
        }

        MessageWriter.Write(VerboseLevel.Verbose, $"  Main patch insert size: {mainSize} bytes.");
        MessageWriter.Write(VerboseLevel.Verbose, $"  Library insert size: {lib.Size} bytes.");
        MessageWriter.Write(VerboseLevel.Verbose, $"  Resource insert size: {resourceHandler.Size} bytes.");
        MessageWriter.Write(VerboseLevel.Normal,  $"  Total insert size: {mainSize + lib.Size + resourceHandler.Size} bytes.");

        MessageWriter.Write(VerboseLevel.Normal, "");
        MessageWriter.Write(VerboseLevel.Normal, "All code inserted successfully.");

        WriteRestoreComment(config.ROMFile, $"{UberMajorVersion}.{UberMinorVersion}");
        FileUtils.DeleteTempFiles();

        // Pause();
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
        Console.Write("Press any key to continue...");

        try
        {
            Console.ReadKey(true);
        }
        catch {	}
    }

    public static void PrintUsage()
    {
        MessageWriter.Write(VerboseLevel.Quiet, "Usage: UberASMTool [<list file> [<ROM file>]]");
        MessageWriter.Write(VerboseLevel.Quiet, "If list file is not specified, UberASM Tool will try loading 'list.txt'.");
        MessageWriter.Write(VerboseLevel.Quiet, "If ROM file is not specified, UberASM Tool will search for the one in the list file.");
        MessageWriter.Write(VerboseLevel.Quiet, "Unless absolute paths are given, the directory relative to the UberASM Tool executable will be used.");
        MessageWriter.Write(VerboseLevel.Quiet, "");
    }

    // this doesn't really fit anywhere else
    // maybe in ROM, but ehh

    private static bool GeneratePointerListFile(LibraryHandler lib, ResourceHandler res)
    {
        var output = new StringBuilder();

        foreach (int addr in lib.Cleans.Concat(res.Cleans))
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
