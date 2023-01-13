namespace UberASMTool;

public class Resource
{
    public int ID { get; init; }       // each resource gets a unique ID -- starts at 0 and increments for each new resource
    public string Filename { get; init; }

    public bool SetDBR { get; private set; } = true;   // changed to false if the resource contains the ">dbr off" command
    public int NumBytes { get; private set; } = 0;        // how many extra bytes this resource uses (0 for none, the default)

    public int EntryAddress { get; set; }   // ROM address of the ResourceEntry: label for this resource
    public bool HasNMI { get; private set; }       // true if this resource has an nmi: label, false if not
    public int NMIAddress { get; set; }
    public int Size { get; private set; }

    public List<Asarlabel> BytesLabels { get; set; } = new();  // ROM address of the ExtraBytes: sublabels for this resource

    public Resource(string file, int id)
    {
        Filename = file;
        ID = id;
    }

    // attempts to read the file and processes any ";>" commands
    // currently just >dbr and >bytes
    // throws any exceptions that arise from reading the file
    // returns false and prints any other errors
    public bool Preprocess()
    {
        string[] lines = File.ReadAllLines(Filename);

        // TODO: More robust handling of resource commands
        if (Array.FindIndex(lines, x => x == ";>dbr off") >= 0)
            SetDBR = false;

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
                MessageWriter.Write(VerboseLevel.Quiet, $"Invalid number in \">bytes\" command in \"{Filename}\".");
                return false;
            }
            if (bytes < 0 || bytes > 255)
            {
                MessageWriter.Write(VerboseLevel.Quiet, $"Invalid value in \">bytes\" command in \"{Filename}\" (must be 0 - 255).");
                return false;
            }
            NumBytes = bytes;
        }

        return true;
    }

    // bad name maybe...this patches a resource into the rom and gathers label data and such
    public bool Add(ROM rom, List<int> cleans)
    {
        MessageWriter.Write(VerboseLevel.Verbose, $"Adding resource \"{Filename}\"...");
        if (!GenerateResourceFile())
            return false;
        if (!rom.Patch("asm/work/resource.asm"))
            return false;
        if (!rom.ProcessPrints(Filename, out int start, out int end, cleans))
            return false;
        cleans.Add(start);

        Size = end - start + 8;
        MessageWriter.Write(VerboseLevel.Verbose, $"  Inserted at ${start:X6}");
        MessageWriter.Write(VerboseLevel.Verbose, $"  Insert size: {Size} (0x{Size:X}) bytes");
        MessageWriter.Write(VerboseLevel.Verbose, "");

        return ProcessLabels();
    }

// gets label information from Asar and adds it to this resource as needed
    private bool ProcessLabels()
    {
        Asarlabel[] labels = Asar.getlabels();

        // Asar.getlabelval() crashes instead of returning -1 if the label doesn't exist for some reason, so doing this instead
        // it shouldn't really ever fail to find the label, but just in case
        int index = Array.FindIndex(labels, x => x.Name == "Inner_ResourceEntry");
        if (index < 0)
        {
            MessageWriter.Write(VerboseLevel.Quiet, $"Error adding \"{Filename}\": ResourceEntry label not found.");
            return false;
        }
        EntryAddress = labels[index].Location;

        // this keeps the base ExtraBytes: label, even though nothing uses it
        foreach (Asarlabel label in labels)
            if (label.Name.StartsWith("Inner_ExtraBytes"))
                BytesLabels.Add(new Asarlabel { Name = label.Name.Substring("Inner_".Length), Location = label.Location });

        // Asar.getlabelval() crashes instead of returning -1 if the label doesn't exist for some reason, so doing this instead
        index = Array.FindIndex(labels, x => x.Name == "Inner_nmi");
        HasNMI = (index >= 0);
        if (HasNMI)
            NMIAddress = labels[index].Location;

        return true;
    }

// adds this resource's labels to the supplied StringBuilder for the main patch
    public void GenerateLabels(StringBuilder output)
    {
        output.AppendLine($"UberResource{ID}_ResourceEntry = ${EntryAddress:X6}");
        if (HasNMI)
            output.AppendLine($"UberResource{ID}_NMI = ${NMIAddress:X6}");

        foreach (Asarlabel label in BytesLabels)
            output.AppendLine($"UberResource{ID}_{label.Name} = ${label.Location:X6}");
    }

    // writes asm/work/resource.asm with this resource's information
    private bool GenerateResourceFile()
    {
        string output = "incsrc \"../base/resource_template.asm\"" + Environment.NewLine +
                        $"%UberResource(\"{Filename}\", {(SetDBR ? 1 : 0)})" + Environment.NewLine;
        return Program.TryWriteFile("asm/work/resource.asm", output);
    }
}
