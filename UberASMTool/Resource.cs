namespace UberASMTool;

// something like
public class ResourceInfo
{
    public int ID { get; set; }       // each resource gets a unique ID -- starts at 0 and increments for each new resource
    public int Bytes { get; set; } = 0;        // how many extra bytes this resource uses (0 for none, the default)
    public int EntryAddress { get; set; }   // ROM address of the ResourceEntry: label for this resource
    public int NMIAddress { get; set; }     
    public List<Asarlabel> BytesLabels { get; set; } = new();  // ROM address of the ExtraBytes: sublabels for this resource
    public bool HasNMI { get; set; }       // true if this resource has an nmi: label, false if not
    public bool SetDBR { get; set; } = true;   // changed to false if the resource contains the ">dbr off" command
}

// holds the info for a single invocation of a resource obtained from list.txt
// this is used by both ResourceStatement (where file refers to the actual value in list.txt),
// and by UberConfig.LevelCalls, etc, where the File is the full pathname that indexes the UberConfig.Resources dictionary
public class ResourceCall
{
    public string File { get; set; }
    public List<int> Bytes { get; set; }
}

public enum ResourceType { None, Level, Gamemode, Overworld };


public static class ResourceHandler
{
    // maybe resources should just be a list instead of a dictionary....meh
    public static Dictionary<string, ResourceInfo> Resources { get; } = new();

    public static bool GenerateResourceLabelFile()
    {
        var output = new StringBuilder();

        // don't really need to sort, but whatever
        foreach (ResourceInfo resource in Resources.Values.OrderBy(info => info.ID))
        {
            output.AppendLine($"UberResource{resource.ID}_ResourceEntry = ${resource.EntryAddress:X6}");
            if (resource.HasNMI)
                output.AppendLine($"UberResource{resource.ID}_NMI = ${resource.NMIAddress:X6}");

            foreach (Asarlabel label in resource.BytesLabels)
                output.AppendLine($"UberResource{resource.ID}_{label.Name} = ${label.Location:X6}");
        }

        return Program.TryWriteFile("asm/work/resource_labels.asm", output.ToString());
    }

    private static bool GenerateExtraBytesFile(ResourceInfo resource)
    {
        var output = new StringBuilder();

        // if we want to support resources with a variable number of extra bytes, extra logic goes here
        if (resource.Bytes > 0)
        {
            WriteDb(UberConfig.AllLevelConfig.Calls, "LevelAll");
            WriteDb(UberConfig.AllGamemodeConfig.Calls, "GamemodeAll");
            WriteDb(UberConfig.AllOverworldConfig.Calls, "OverworldAll");

            for (int i = 0; i < UberConfig.LevelConfigs.Length; i++)
                WriteDb(UberConfig.LevelConfigs[i].Calls, $"Level{i:X}");
            for (int i = 0; i < UberConfig.GamemodeConfigs.Length; i++)
                WriteDb(UberConfig.GamemodeConfigs[i].Calls, $"Gamemode{i:X}");
            for (int i = 0; i < UberConfig.OverworldConfigs.Length; i++)
                WriteDb(UberConfig.OverworldConfigs[i].Calls, $"Overworld{i:X}");
        }

        return Program.TryWriteFile("asm/work/extra_bytes.asm", output.ToString());

        // local function
        void WriteDb(List<ResourceCall> calls, string sublabel)
        {
            ResourceCall call = calls.Find(x => Resources[x.File].ID == resource.ID);
            if (call == null)
                return;

            // note only the first call to this resource for this level/ow/gm is used, which is okay since we're not allowing duplicate calls
            output.AppendLine($".{sublabel}:");
            output.AppendFormat("    db {0}", String.Join(", ", call.Bytes.Select(x => $"${x:X2}")));
            output.AppendLine();
        }
    }

    public static bool BuildResources()
    {
        foreach (KeyValuePair<string, ResourceInfo> kvp in Resources)
        {
            ResourceInfo resource = kvp.Value;
            string filename = kvp.Key;

            // print something probably

            if (!GenerateExtraBytesFile(resource))
                return false;

            string output = "incsrc \"../base/resource_template.asm\"" + Environment.NewLine +
                            $"%UberResource(\"{filename}\", {(resource.SetDBR ? 1 : 0)})" + Environment.NewLine;
            if (!Program.TryWriteFile("asm/work/resource.asm", output))
                return false;

            if (!ROM.Patch("asm/work/resource.asm"))
                return false;

            if (!ROM.ProcessPrints(filename, out int start, out int end, true))
                return false;
            Program.ProtPointers.Add(start);

            int insertSize = end - start + 8;
            MessageWriter.Write(false, $"  Inserted at ${start:X6}");
            MessageWriter.Write(false, $"  Insert size: {insertSize} (0x{insertSize:X}) bytes");
            // TODO: something about total insert size

            Asarlabel[] labels = Asar.getlabels();

            // Asar.getlabelval() crashes instead of returning -1 if the label doesn't exist for some reason, so doing this instead
            // it shouldn't really ever fail to find the label, but just in case
            int index = Array.FindIndex(labels, x => x.Name == "Inner_ResourceEntry");
            if (index < 0)
            {
                MessageWriter.Write(true, $"Error adding \"{filename}\": ResourceEntry label not found.");
                return false;
            }
            resource.EntryAddress = labels[index].Location;

            // this keeps the base ExtraBytes: label, even though nothing uses it
            foreach (Asarlabel label in labels)
                if (label.Name.StartsWith("Inner_ExtraBytes"))
                    resource.BytesLabels.Add(new Asarlabel { Name = label.Name.Substring("Inner_".Length), Location = label.Location });

            // Asar.getlabelval() crashes instead of returning -1 if the label doesn't exist for some reason, so doing this instead
            index = Array.FindIndex(labels, x => x.Name == "Inner_nmi");
            resource.HasNMI = (index >= 0);
            if (index > 0)
                resource.NMIAddress = labels[index].Location;
        }

        return true;
    }

    public static bool GenerateCallFile()
    {
        var output = new StringBuilder();

        GenerateCallsOfType(output, UberConfig.LevelConfigs, UberConfig.AllLevelConfig, "Level");
        GenerateCallsOfType(output, UberConfig.GamemodeConfigs, UberConfig.AllGamemodeConfig, "Gamemode");
        GenerateCallsOfType(output, UberConfig.OverworldConfigs, UberConfig.AllOverworldConfig, "Overworld");

        return Program.TryWriteFile("asm/work/resource_calls.asm", output.ToString());
    }

    private static void GenerateCallsOfType(StringBuilder output, ConfigInfo[] singles, ConfigInfo all, string type)
    {
        AddUnusedLabels(output, singles, type);
        output.AppendLine("    rts").AppendLine();

        AddUsedLabels(output, singles, true, type);
        AddUsedLabels(output, singles, false, type);

        AddAllMacro(output, all, false, type);
        AddAllMacro(output, all, true, type);
    }

    private static void AddUnusedLabels(StringBuilder output, ConfigInfo[] singles, string type)
    {
        for (int i = 0; i < singles.Length; i++)
        {
            if (!singles[i].Calls.Any())
                output.AppendLine($"{type}{i:X}JSLs:");
            if (!singles[i].HasNMI)
                output.AppendLine($"{type}{i:X}NMIJSLs:");
        }
    }

    private static void AddUsedLabels(StringBuilder output, ConfigInfo[] singles, bool nmi, string type)
    {
        for (int i = 0; i < singles.Length; i++)
        {
            bool used = nmi ? singles[i].HasNMI : singles[i].Calls.Any();

            if (!used)
                continue;
            output.AppendLine($"{type}{i:X}{(nmi ? "NMI" : "")}JSLs:");
            AddCalls(output, singles[i].Calls, nmi, type, $"{i:X}");
            output.AppendLine("    rts").AppendLine();
        }
    }

    private static void AddAllMacro(StringBuilder output, ConfigInfo all, bool nmi, string type)
    {
        output.AppendLine($"macro {type}All{(nmi ? "NMI" : "")}JSLs()");
        AddCalls(output, all.Calls, nmi, type, "All");
        output.AppendLine("endmacro").AppendLine();
    }

    private static void AddCalls(StringBuilder output, IEnumerable<ResourceCall> calls, bool nmi, string type, string which)
    {
        foreach (ResourceCall call in calls)
        {
            ResourceInfo resource = Resources[call.File];
            // don't call for NMI if this resource doesn't have an nmi: label
            if (nmi && !resource.HasNMI)
                continue;
            if (call.Bytes.Any())
                output.AppendLine($"    %CallUberResourceWithBytes({resource.ID}, {(nmi ? "1" : "0")}, {type}, {which})");
            else
                output.AppendLine($"    %CallUberResource({resource.ID}, {(nmi ? "1" : "0")})");
        }
    }


}
