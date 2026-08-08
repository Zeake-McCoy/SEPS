namespace SEPS.Rendering;

public interface IRenderLayer
{
    string Id { get; }
    string DisplayName { get; }
    bool IsEnabled { get; set; }
}

public sealed class RenderLayer : IRenderLayer
{
    public string Id { get; }
    public string DisplayName { get; }
    public bool IsEnabled { get; set; } = true;

    public RenderLayer(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }
}
