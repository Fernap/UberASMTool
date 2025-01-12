namespace UberASMTool;

public enum UberContextType { None, Level, Gamemode, Overworld }

// collects all the members of a context together
public class UberContext
{
    private ContextMember star;           // would like this to be "default", but that's a keyword...so using "star" because that's how it's denoted in the list file
    private ContextMember[] singles;
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
        star = new ContextMember();
        singles = new ContextMember[max];
        for (int i = 0; i < max; i++)
            singles[i] = new ContextMember();
    }

    public ContextMember GetMember(int num)
    {
        if (num == -1)
            return star;
        else
            return singles[num];
    }
    
    public void GenerateExtraBytes(StringBuilder output, Resource resource)
    {
        star.GenerateExtraBytes(output, resource, $"{Name}Default");
        for (int i = 0; i < singles.Length; i++)
            singles[i].GenerateExtraBytes(output, resource, $"{Name}{i:X}");
    }

    // returns the value of the general NMI define for this context, NOT success/failure
    public bool AddNMIDefines(ROM rom)
    {
        bool nmi = star.HasNMI;
        if (!nmi)
            foreach (ContextMember single in singles)
                if (single.HasNMI)
                {
                    nmi = true;
                    break;
                }

        rom.AddDefine($"Uber{Name}NMI", nmi ? "1" : "0");

        return nmi;
    }

    public void GenerateCalls(StringBuilder output)
    {
        GenerateDefaultCalls(output, false);
        GenerateDefaultCalls(output, true);

        GenerateSpecifiedCalls(output, false);
        GenerateSpecifiedCalls(output, true);
    }

    private void GenerateDefaultCalls(StringBuilder output, bool nmi)
    {
        for (int i = 0; i < singles.Length; i++)
            if (singles[i].Empty)
                output.AppendLine($"{Name}{i:X}{(nmi ? "NMI" : "")}:");

        star.GenerateCalls(output, nmi, Name, "Default", _ => false);
        output.AppendLine("    rts").AppendLine();
    }

    private void GenerateSpecifiedCalls(StringBuilder output, bool nmi)
    {
        for (int i = 0; i < singles.Length; i++)
        {
            if (singles[i].Empty)
                continue;
            output.AppendLine($"{Name}{i:X}{(nmi ? "NMI" : "")}:");
            star.GenerateCalls(output, nmi, Name, "Default", r => singles[i].SkipsResource(r));
            singles[i].GenerateCalls(output, nmi, Name, $"{i:X}", _ => false);
            output.AppendLine("    rts").AppendLine();
        }
    }

}
