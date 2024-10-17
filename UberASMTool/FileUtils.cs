namespace UberASMTool;

public static class FileUtils
{
    public static bool TryWriteFile(string file, string text)
    {
        try
        {
            File.WriteAllText(file, text);
        }
        catch (Exception e)
        {
            MessageWriter.Write(VerboseLevel.Quiet, $"Error writing file \"{file}\": {e}");
            return false;
        }
        return true;
    }

    // attempts to create any default dirs that are initially empty, in case you want to keep in a github repo with .gitkeeps lying around
    // doesn't do asm/base/, other/, or routines/, since those have stuff by default
    public static bool CreateDirs()
    {
        try
        {
            Directory.CreateDirectory("asm/work");
            Directory.CreateDirectory("library");
            Directory.CreateDirectory("level");
            Directory.CreateDirectory("overworld");
            Directory.CreateDirectory("gamemode");
        }
        catch (Exception e)
        {
            MessageWriter.Write(VerboseLevel.Quiet, $"Error: could not create initial directory structure: {e.Message}");
            return false;
        }

        return true;
    }

    public static void DeleteTempFiles()
    {
        // there should probably be a separate option for this, but for now, skipping temp file deletion if verbose: debug
        if (MessageWriter.Verbosity == VerboseLevel.Debug)
            return;

        string tempDir = Path.Combine(Program.MainDirectory, "asm/work");
        try
        {
            string[] files = Directory.GetFiles(tempDir, "*.asm");
            foreach (string file in files)
                File.Delete(file);
        }
        catch (Exception e)
        {
            MessageWriter.Write(VerboseLevel.Normal, $"Warning: could not delete temporary files: {e.Message}");
        }
    }
}