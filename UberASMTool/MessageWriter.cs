namespace UberASMTool;

public static class MessageWriter
{
    public static bool VerboseMode { get; set; } = false;

    public static void Write(bool important, string msg, params object[] args)
    {
        if (VerboseMode || important)
            Console.WriteLine(msg, args);
    }
}
