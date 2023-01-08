// WARNING!
// Make sure to sync any changes in assets/asm to the folder that VS puts the executable for testing. (or vice versa)

// TODO:
// getting freespace leak warnings from asar for library files (probably resource too)...
//    so I'm adding in a "cleaned" modifier to suppress it...go back and see if that's really necessary (resource_template.asm and library_template.asm)
// look at LM tool use reporting thingy
// clean up/standardize error message formats
// go through and make sure i'm printing the same info (more or less) that 1.x does
// I may not need to bail out at the first sign of an error...look into how far I can push until that's really needed
// Asar.getlabelval() seems to cause an exception for a nonexistent label 
// make the naming of NMI labels consistent in the patches
// optimize level/ow/gm call code for situations where none are being called
// Put something in empty folders so they actually go to github, even just readmes
// add a note to readme about legal library names (and how spaces/subdirs are treated), will resolve #15
// rename ResourceType to UberEnvironment (or something)
// note in readme that DBR does *not* need to be restored if set manually
// don't set DBR for nmi, even if the resource doesn't have it turned off...this doesn't break compat...we just treat NMI as special

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
    public static List<int> ProtPointers = new();                    // (not just prots, but resource/library freecode areas too) as should this
    public const int UberMajorVersion = 2;                           // put these somewhere better..take from solution settings?
    public const int UberMinorVersion = 0;

    private static int Main(string[] args)
    {
        Directory.SetCurrentDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
        MainDirectory = Environment.CurrentDirectory + "/";

        if (!Asar.init())
        {
            Console.WriteLine("Could not initialize or find asar.dll");
            Console.WriteLine("Please redownload the program.");
            Pause();
            return 1;
        }

        if (args.Length == 0 || args.Length > 2)
        {
            Console.WriteLine("Usage: UberASMTool [<list file> [<ROM file>]]");
            Console.WriteLine("If list file is not specified, UberASM Tool will try loading 'list.txt'.");
            Console.WriteLine("If ROM file is not specified, UberASM Tool will search for the one in the list file.");
            Console.WriteLine("Unless absolute paths are given, the directory relative to the UberASM Tool executable will be used.");
            Console.WriteLine();
        }

        if (args.Length > 2) { Pause(); return 1; }
        string listFile = (args.Length >= 1) ? args[0] : "list.txt";
        IEnumerable<ConfigStatement> statements = ListParser.ParseList(listFile);
        if (statements == null) { Abort(); return 1; }
        if (!UberConfig.ProcessList(statements)) { Abort(); return 1; }

        if (args.Length < 2 && UberConfig.ROMFile == null)
        {
            MessageWriter.Write(true, "No ROM file specified in list file or on command line.");
            Abort();
            return 1;
        }

        ROM.AddDefine("UberMajorVersion", $"{UberMajorVersion}");
        ROM.AddDefine("UberMinorVersion", $"{UberMinorVersion}");
        if (!ROM.Init(args.Length >= 2 ? args[1] : UberConfig.ROMFile)) { Abort(); return 1; }
        if (!ROM.Patch("asm/base/clean.asm")) { Abort(); return 1; }
        if (!LibraryHandler.BuildLibrary()) { Abort(); return 1; }
        if (!LibraryHandler.GenerateLibraryLabelFile()) { Abort(); return 1; }
        if (!ResourceHandler.BuildResources()) { Abort(); return 1; }
        if (!ResourceHandler.GenerateResourceLabelFile()) { Abort(); return 1; }
        UberConfig.AddNMIDefines();
        if (!ResourceHandler.GenerateCallFile()) { Abort(); return 1; }
        if (!GeneratePointerListFile()) { Abort(); return 1; }

        //TODO:
        // should process prints, can add to total insert size
        if (!ROM.Patch("asm/base/main.asm")) { Abort(); return 1; }
        if (!ROM.ProcessPrints("asm/base/main.asm", out int start, out int end, false)) { Abort(); return 1; }
        if (!ROM.Save()) { Abort(); return 1; }

        // sucess, print some stuff
// 1.x does this (verbose) -- total is from resource & libraries (note it doesn't catch prots..ehh
// adds the main patch to it, which it finds with a "print freespaceuse" in the main patch...I just have a single freecode block with a startl/endl
// so process prints like normal, but error if it finds any _prots -- can't use prot macros in global/status files
//Console.WriteLine("Main patch insert size: {0} (0x{0:X4}) bytes", insertSize);
//Console.WriteLine();
//Console.WriteLine("Total: {0} (0x{0:X4}) bytes", insertSize + totalInsertSize);
//Console.WriteLine();
        return 0;
    }

// TODO: clean up temp files
    private static void Abort()
    {
        Console.WriteLine("Some errors occured while running UberASM Tool.  Process aborted.");
        Console.WriteLine("Your ROM has not been modified.");
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

// this doesn't really fit anywhere else
// maybe in ROM, but ehh

    private static bool GeneratePointerListFile()
    {
        var output = new StringBuilder();

        foreach (int addr in ProtPointers)
            output.AppendLine($"dl ${addr:X6}");

        return TryWriteFile("asm/work/pointer_list.asm", output.ToString());
    }

// this should probably go in a FileUtils helper class or something
    public static bool TryWriteFile(string file, string text)
    {
        try
        {
            File.WriteAllText(file, text);
        }
        catch (Exception e)
        {
            MessageWriter.Write(true, $"Error writing file \"{file}\": {e}");
            return false;
        }
        return true;
    }
}
