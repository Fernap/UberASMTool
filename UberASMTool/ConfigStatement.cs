// Data container classes that hold the result of a single statemnt read from list.txt

namespace UberASMTool;

public class ConfigStatement
{
    public int Line { get; set; }

    public void Error(string msg) => MessageWriter.Write(VerboseLevel.Quiet, $"Error on line {Line}: {msg}");
}

// "verbose:"
public class VerboseStatement : ConfigStatement
{
    public VerboseLevel Verbosity { get; init; }
}

// "deprecations:"
public class DeprecationsStatement : ConfigStatement
{
    public bool Warn { get; init; }
}

// this should maybe go elsewhere, but leaving here for now:
public enum FileType { Global, Statusbar, Macrolib, ROM };

// global:, macrolib:, statusbar:, rom:
public abstract class FileStatement : ConfigStatement
{
    public string Filename { get; set; }
    public abstract string Description { get; }
    public abstract string Define { get; }

    public bool Process(ref bool already, ROM rom)
    {
        if (already)
        {
            Error($"{Description} already specified.");
            return false;
        }

        already = true;
        string path = Path.Combine(Program.MainDirectory, Filename).Replace("\\", "/");      // to make asar happy
        if (!File.Exists(path))
        {
            Error($"File \"{path}\" not found.");
            return false;
        }
        path = path.Replace("!", "\\!");
        rom.AddDefine(Define, path);
        return true;
    }
}

public class GlobalFileStatement : FileStatement
{
    public override string Description => "Global file statement";
    public override string Define => "GlobalCodeFile";
}

public class StatusbarFileStatement : FileStatement
{
    public override string Description => "Statusbar file statement";
    public override string Define => "StatusbarCodeFile";
}

public class MacrolibFileStatement : FileStatement
{
    public override string Description => "Macro library statement";
    public override string Define => "MacrolibFile";
}

public class ROMStatement : ConfigStatement
{
    public string Filename { get; init; } = null!;      // I guess

    public bool Process(ref string path)
    {
        if (path != null)
        {
            Error($"ROM file already specified.");
            return false;
        }

        path = Path.Combine(Program.MainDirectory, Filename).Replace("\\", "/");      // to make asar happy
        return true;
    }
}

// "freeram:"
public class FreeramStatement : ConfigStatement
{
    public int Addr { get; init; }

    public bool Process(ref bool already, ROM rom)
    {
        if (already)
        {
            Error("Freeram statement already specified.");
            return false;
        }

        already = true;
        if (!(Addr >= 0x7E0000 && Addr <= 0x7FFFFF) || (Addr >= 0x400000 && Addr <= 0x41FFFF))
        {
            Error("Freeram address must be in the range 7E0000-7FFFFF or 400000-41FFFF.");
            return false;
        }

        rom.AddDefine("UberFreeram", $"${Addr:X6}");
        return true;
    }
}

// this covers "level:", "overworld:", and "gamemode:"
public class ModeStatement : ConfigStatement
{
    public UberContextType Mode { get; init; }
}

public class ResourceStatement : ConfigStatement
{
    public class Call
    {
        public string Filename { get; init; }
        public List<int> Bytes { get; init; }
    }

    public int Number { get; set; }
    public List<Call> Calls { get; init; } = new();


    public bool Process(UberContext context, ResourceHandler handler, ROM rom)
    {
        if (Number < -1 || Number >= context.Size)
        {
            Error($"{context.Name} out of range (0 - {context.Size - 1:X}).");
            return false;
        }

        ContextMember member = context.GetMember(Number);
        if (!member.Empty)
        {
            string numstr = Number == -1 ? "\"*\"" : $"{Number:X}";
            Error($"Resource(s) for {context.Name.ToLower()} {numstr} already specified.");
            return false;
        }

        foreach (Call call in Calls)     // aaaaaaaaaaaaaaaaaa
        {
            string file = Path.GetFullPath(Path.Combine(Program.MainDirectory, context.Directory, call.Filename)).Replace("\\", "/");

            Resource resource;
            try
            {
                if (!handler.GetOrAddResource(file, out resource, rom))    // a failure will already have an error printed (without the line num)
                    return false;
            }
            catch
            {
                Error($"Could not read file \"{file}\"");
                return false;
            }

            if (member.CallsResource(resource))
            {
                Error("Duplicate resource specified.");
                return false;
            }

            if (resource.VarBytes)
            {
                if (call.Bytes.Count > 255)
                {
                    Error("Too many extra bytes supplied (max is 255)");
                    return false;
                }
            }
            else
            {
                if (call.Bytes.Count != resource.NumBytes)
                {
                    Error($"Incorrect number of extra bytes supplied: Expected {resource.NumBytes}, but found {call.Bytes.Count}");
                    return false;
                }
            }

            member.AddCall(resource, call.Bytes);
        }

        return true;
    }
}
