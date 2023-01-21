namespace UberASMTool;

public class ResourceHandler
{
    public int Size { get; private set; }

    private List<Resource> resources = new();

    // add a failed flag for resources, so subsequent attempts to use it don't generate more errors
    // consider keeping this as a dictionary as it was before...not a huge difference
    // file - full filename of the resource
    // this puts the resource associated to file into the resource parameter,
    // if already loaded, it will use that, if not, it will try to load it
    // passes along an exception if the file couldn't be read
    // returns false on other errors
    public bool GetOrAddResource(string file, out Resource resource, ROM rom)
    {
        resource = resources.Find(x => x.Filename == file);
        if (resource != null)
            return true;

        resource = new Resource(file, resources.Count);
        resources.Add(resource);
        return resource.Preprocess();
    }

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

        // print something probably
        return true;
    }

}
