namespace UberASMTool;

public enum VerboseLevel { Quiet, Normal, Verbose, Debug }

public static class MessageWriter
{
    public static VerboseLevel Verbosity { get; set; } = VerboseLevel.Normal;

    public static void Write(VerboseLevel level, string msg)
    {
        if (Verbosity >= level)
            Console.WriteLine(msg);
    }
}
