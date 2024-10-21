namespace UberASMTool;

public class LibraryHandler
{
    public int Size { get; private set; } = 0;

    private Dictionary<string, int> labels = new();


    // patches all the library files into the rom and creates the label file all at once
    public bool BuildLibrary(ROM rom)
    {
        string[] files;
        
        try
        {
            files = Directory.GetFiles("library", "*", SearchOption.AllDirectories);
        }
        catch
        {
            MessageWriter.Write(VerboseLevel.Quiet, "Could not read contents of library/ directory.");
            return false;
        }

        string libPath = Path.GetFullPath("library");

        foreach (string file in files)
        {
            string relPath = Path.GetRelativePath(libPath, file).Replace("\\", "/");
            string prefix = Path.ChangeExtension(relPath, null).Replace(" ", "_").Replace("/", "_");
            bool binary = Path.GetExtension(relPath).ToLower() != ".asm";

            MessageWriter.Write(VerboseLevel.Verbose, $"  Processing {(binary ? "binary" : "asm")} file \"{relPath}\":");

            string output = "incsrc \"../base/library_template.asm\"" + Environment.NewLine +
                            $"%UberLibrary(\"{relPath}\", {(binary ? 1 : 0)})" + Environment.NewLine;

            if (!FileUtils.TryWriteFile("asm/work/library.asm", output))
                return false;
            if (!rom.Patch("asm/work/library.asm", null))
                return false;
            if (!rom.ProcessPrints(file, out int start, out int insertSize, true))
                return false;

            MessageWriter.Write(VerboseLevel.Verbose, $"    Inserted at ${start:X6}");
            MessageWriter.Write(VerboseLevel.Verbose, $"    Insert size: {insertSize} (0x{insertSize:X}) bytes");
            MessageWriter.Write(VerboseLevel.Verbose, "");
            Size += insertSize;

            // consider adding top-level label for source files too
            if (binary)
                if (!AddLabel(prefix, start))
                    return false;

            if (!binary)
                if (!GetLabels(prefix, file, rom))
                    return false;
        }

        if (files.Length > 0)
            MessageWriter.Write(VerboseLevel.Normal, $"  Processed {files.Length} library file(s).");

        return true;
    }

    // gets all the labels from a patched library .asm file and adds them to labels
    private bool GetLabels(string prefix, string file, ROM rom)
    {
        int numlabels = 0;

        foreach (Asarlabel label in Asar.getlabels())
        {
            if (label.Name.Contains(":"))      // skips macro-local and +/- labels
                continue;
            if (label.Name.Contains("."))      // also skips struct fields, which aren't legal label names either
                continue;
            if (label.Name.StartsWith("UberRoutine_"))
            {
                if (!rom.AddRoutine(file, label.Name.Substring("UberRoutine_".Length), label.Location))
                    return false;
                continue;
            }
            if (!AddLabel($"{prefix}_{label.Name}", label.Location))
                return false;
            numlabels++;
        }

        if (numlabels == 0)
        {
            MessageWriter.Write(VerboseLevel.Quiet, $"Error: No labels found in library file \"{file}\".");
            return false;
        }
        MessageWriter.Write(VerboseLevel.Verbose, $"  Processed {numlabels} label(s).");
        return true;
    }

    private bool AddLabel(string name, int addr)
    {
        if (labels.ContainsKey(name))
        {
            MessageWriter.Write(VerboseLevel.Quiet, $"Error: Duplicate library label \"{name}\".");
            return false;
        }

        labels[name] = addr;
        return true;
    }

    public bool GenerateLibraryLabelFile()
    {
        var output = new StringBuilder();

        foreach (KeyValuePair<string, int> label in labels)
            output.AppendLine($"{label.Key} = ${label.Value:X6}");

        return FileUtils.TryWriteFile("asm/work/library_labels.asm", output.ToString());
    }
}
