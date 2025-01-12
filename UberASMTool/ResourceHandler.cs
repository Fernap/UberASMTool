namespace UberASMTool;

public class ResourceHandler
{
    public int Size { get; private set; }

    private List<Resource> resources = [];

    // this puts the resource associated to file into the resource parameter,
    // if already loaded, it will use that, if not, it will try to load it
    // file - full filename of the resource
    // returns false on other errors
    // passes along an exception if the file couldn't be read
    public bool GetOrAddResource(string file, out Resource resource)
    {
        resource = GetResource(file);
        if (resource != null)
            return true;

        resource = new Resource(file, resources.Count);
        resources.Add(resource);
        return resource.Preprocess();
    }

    // This is kind of an ugly hack to prevent resources specified with different casing to be inserted multiple times.
    // The upshot is that the first invocation uses the case sensitivity of the underlying file system, while
    //   subsequent invocations are case-insensitive and will match any previous invocations.
    public Resource GetResource(string file) =>
        resources.Find(x => x.Filename.ToLower() == file.ToLower());

    public bool GenerateResourceLabelFile()
    {
        var output = new StringBuilder();

        foreach (Resource resource in resources)
            resource.GenerateLabels(output);

        return FileUtils.TryWriteFile("asm/work/resource_labels.asm", output.ToString());
    }

    public bool BuildResources(UberConfig config, ROM rom)
    {
        foreach (Resource resource in resources)
        {
            if (!resource.Add(rom, config))
                return false;
            Size += resource.Size;
        }

        if (resources.Count > 0)
            MessageWriter.Write(VerboseLevel.Normal, $"  Processed {resources.Count} resource file(s).");

        return true;
    }

}
