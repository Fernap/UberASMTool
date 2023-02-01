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