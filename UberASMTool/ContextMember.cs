namespace UberASMTool;

// holds the info for a single invocation of a resource obtained from list.txt
public class ResourceCall
{
    public Resource ToCall { get; init; }
    public List<int> Bytes { get; init; }
}


// keeps information about what resources are called for a particular context member (ie, level/ow/gm)
public class ContextMember
{
    private List<ResourceCall> calls { get; set; } = new();
    public bool Empty => !calls.Any();

    // could memoize this since once it becomes true, it never changes
    // but this is more future-proof in case that ever changes
    public bool HasNMI => calls.Any(x => x.ToCall.HasNMI);

    // throws an ArgumentException if this member already calls the given resource
    public void AddCall(Resource resource, List<int> bytes)
    {
        if (CallsResource(resource))
            throw new ArgumentException("Context member already calls this resource.");
        calls.Add(new ResourceCall { ToCall = resource, Bytes = bytes } );
    }

    public bool CallsResource(Resource res) => calls.Any(x => x.ToCall == res);

    public void GenerateExtraBytes(StringBuilder output, Resource resource, string sublabel)
    {
        ResourceCall call = calls.Find(x => x.ToCall == resource);
        if (call == null)
            return;

        // note only the first call to this resource for this level/ow/gm is used, which is okay since we're not allowing duplicate calls
        output.AppendLine($".{sublabel}:");
        output.AppendFormat("    db {0}", String.Join(", ", call.Bytes.Select(x => $"${x:X2}")));
        output.AppendLine();
    }

    // might be better to set/keep type/which at construction time rather than passing it in
    public void GenerateCalls(StringBuilder output, bool nmi, string type, string which)
    {
        foreach (ResourceCall call in calls)
        {
            // don't call for NMI if this resource doesn't have an nmi: label
            if (nmi && !call.ToCall.HasNMI)
                continue;
            if (call.Bytes.Any())
                output.AppendLine($"    %CallUberResourceWithBytes({call.ToCall.ID}, {(nmi ? "1" : "0")}, {type}, {which})");
            else
                output.AppendLine($"    %CallUberResource({call.ToCall.ID}, {(nmi ? "1" : "0")})");
        }
    }

}
