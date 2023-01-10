namespace UberASMTool;

//public class ResourceCall
//{
//    public string File { get; set; }
//    public List<int> Bytes { get; set; }
//}

// but I want resourcecall to just have a Resource member, not the file

// add an enum that holds context type and a static method that translates that to a string
// and simplify stuff in terms of that

// ehhhhhhh maybe not
public enum UberContextType { None, Level, Gamemode, Overworld }

// collects all the members of a context together
public class UberContext
{
    private ContextMember all;
    private ContextMember[] singles;
    public bool HasNMI { get; private set; }
    public string Name { get; init; }
    public string Directory => Name.ToLower();
    public int Size { get; init; }

    public static string TypeToName(UberContextType contextType) => contextType switch
    {
        UberContextType.Level => "Level",
        UberContextType.Gamemode => "Gamemode",
        UberContextType.Overworld => "Overworld",
        _ => throw new ArgumentException()
    };

// contextName = upper case: "Level", etc, used for labels
    public UberContext(UberContextType type, int max)
    {
        Name = TypeToName(type);
        Size = max;
        all = new ContextMember();
        singles = new ContextMember[max];
        for (int i = 0; i < max; i++)
            singles[i] = new ContextMember();
    }

    public ContextMember GetMember(int num)
    {
        if (num == -1)
            return all;
        else
            return singles[num];
    }
    
    public void GenerateExtraBytes(StringBuilder output, Resource resource)
    {
        all.GenerateExtraBytes(output, resource, $"{Name}All");
        for (int i = 0; i < singles.Length; i++)
            singles[i].GenerateExtraBytes(output, resource, $"{Name}{i}");
    }

    // returns the value of the general NMI define for this context, NOT success/failure
    public bool AddNMIDefines(ROM rom)
    {
        bool allNMI = all.HasNMI;

        bool normalNMI = false;
        foreach (ContextMember single in singles)
            if (single.HasNMI)
            {
                normalNMI = true;
                break;
            }

        bool overallNMI = allNMI || normalNMI;

        rom.AddDefine($"Uber{Name}NMIAll", allNMI ? "1" : "0");
        rom.AddDefine($"Uber{Name}NMINormal", normalNMI ? "1" : "0");
        rom.AddDefine($"Uber{Name}NMI", overallNMI ? "1" : "0");

        return overallNMI;
    }

// could skip if there are no NMIs, but it's just the labels (and a possibly empty macro), so it doesn't really matter
    public void GenerateCalls(StringBuilder output)
    {
        GenerateUnusedLabels(output);
        output.AppendLine("    rts").AppendLine();

        GenerateUsedLabels(output, false);
        GenerateUsedLabels(output, true);

        GenerateAllMacro(output, false);
        GenerateAllMacro(output, true);
    }

    private void GenerateUnusedLabels(StringBuilder output)
    {
        for (int i = 0; i < singles.Length; i++)
        {
            if (singles[i].Empty)
                output.AppendLine($"{Name}{i:X}JSLs:");
            if (singles[i].Empty)
                output.AppendLine($"{Name}{i:X}NMIJSLs:");
        }
    }

    private void GenerateUsedLabels(StringBuilder output, bool nmi)
    {
        for (int i = 0; i < singles.Length; i++)
        {
            bool used = nmi ? singles[i].HasNMI : !singles[i].Empty;

            if (!used)
                continue;
            output.AppendLine($"{Name}{i:X}{(nmi ? "NMI" : "")}JSLs:");
            singles[i].GenerateCalls(output, nmi, Name, $"{i:X}");
            output.AppendLine("    rts").AppendLine();
        }
    }

    private void GenerateAllMacro(StringBuilder output, bool nmi)
    {
        output.AppendLine($"macro {Name}All{(nmi ? "NMI" : "")}JSLs()");
        all.GenerateCalls(output, nmi, Name, "All");
        output.AppendLine("endmacro").AppendLine();
    }

}
