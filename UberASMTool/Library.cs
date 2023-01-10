// label exporting sucks --
// "foo/bar.asm" and "foo bar.asm" will share the same prefix
// (disallowing spaces in filenames would solve some problems)
// (underscores in filenames would have the exact same problem)
// "foo/bar.asm"'s prefix (foo_bar) will start with "foo.asm"'s prefix (foo)
// only way to stop this is to just export top-level labels (without underscores)
// you'd still get "bar:" in foo.asm clashing with "foo/bar.asm"'s auto-top label (those can be removed, which would mean still error on no label)
// even still, bar: in foo.asm would clash with /foo/bar.bin (binary files strip extensions)
// fuck it, just check for duplicates and move on with life I guess

namespace UberASMTool;

public static class LibraryHandler
{
    private static List<Asarlabel> labels = new ();     // all exposed labels (with prefixes)

    public static bool BuildLibrary(ROM rom)
    {
        MessageWriter.Write(false, "Building external library..." + Environment.NewLine);

        string[] files;
        
        try
        {
            files = Directory.GetFiles("library", "*", SearchOption.AllDirectories);
        }
        catch
        {
            MessageWriter.Write(true, "Could not read contents of library/ directory.");
            return false;
        }

        string libPath = Path.GetFullPath("library");

        foreach (string file in files)
        {
            string relPath = Path.GetRelativePath(libPath, file).Replace("\\", "/");
            string prefix = Path.ChangeExtension(relPath, null).Replace(" ", "_").Replace("/", "_");
            bool binary = Path.GetExtension(relPath).ToLower() != ".asm";

            MessageWriter.Write(false, $"Processing {(binary ? "binary " : "")}file \"{relPath}\":");

            string output = "incsrc \"../base/library_template.asm\"" + Environment.NewLine +
                            $"%UberLibrary(\"{relPath}\", {(binary ? 1 : 0)})" + Environment.NewLine;

            if (!Program.TryWriteFile("asm/work/library.asm", output))
                return false;
            if (!rom.Patch("asm/work/library.asm"))
                return false;
            if (!rom.ProcessPrints(file, out int start, out int end, true))
                return false;

            int insertSize = end - start + 8;
            Program.ProtPointers.Add(start);
            MessageWriter.Write(false, $"  Inserted at ${start:X6}");
            MessageWriter.Write(false, $"  Insert size: {insertSize} (0x{insertSize:X}) bytes");
            // TODO: add to total insert size somewhere?

            if (!binary)
            {
                int numlabels = 0;

                foreach (Asarlabel label in Asar.getlabels())
                {
                    if (label.Name.Contains(":"))      // this skips macro-local and +/- labels
                        continue;
                    if (!AddLabel($"{prefix}_{label.Name}", label.Location))
                        return false;
                    numlabels++;
                }

                if (numlabels == 0)
                {
                    MessageWriter.Write(true, $"Error: No labels found in library file \"{file}\".");
                    return false;
                }
                MessageWriter.Write(false, $"  Processed {numlabels} label(s).");
            }

            // consider adding top-level label for source files too
            if (binary)
               if (!AddLabel(prefix, start))
                   return false;
        }

        if (files.Length > 0)
            MessageWriter.Write(false, $"Processed {files.Length} library file(s).");

        return true;
    }

    private static bool AddLabel(string name, int addr)
    {
        if (labels.Any(x => x.Name == name))
        {
            MessageWriter.Write(true, "Error: Duplicate library label.");
            return false;
        }

        labels.Add(new Asarlabel { Name = name, Location = addr } );
        return true;
    }

    public static bool GenerateLibraryLabelFile()
    {
        var output = new StringBuilder();

    // TODO: add subdir and spaces changing to undescores in doc
        foreach (Asarlabel label in labels)
            output.AppendLine($"{label.Name} = ${label.Location:X6}");

        return Program.TryWriteFile("asm/work/library_labels.asm", output.ToString());
    }
}
