// Data container classes that hold the result of a single statemnt read from list.txt

namespace UberASMTool;

public class ConfigStatement
{
    public int Line { get; set; }
}

// "verbose:"
public class VerboseStatement : ConfigStatement
{
    public bool IsOn { get; init; }
    public bool IsOff { get => !IsOn; }
}

// global:, macrolib:, statusbar:, rom:
public class FileStatement : ConfigStatement
{
    public string Filename { get; init; } = null!;      // I guess
    public FileType Type { get; set; }
}

// "freeram:"
public class FreeramStatement : ConfigStatement
{
    public int Addr { get; init; }
}

// this covers "level:", "overworld:", and "gamemode:"
public class ModeStatement : ConfigStatement
{
    public ResourceType Mode { get; init; }
}

public class ResourceStatement : ConfigStatement
{
    public int Number { get; set; }
    public List<ResourceCall> Calls { get; init; } = new();
}
