namespace XtremeWorlds.Client.Features.UI.Styles;

public sealed class Style(Dictionary<string, StylePart> parts)
{
    public static readonly Style Empty = new([]);
    
    public StylePart? GetPart(string partName)
    {
        return parts.GetValueOrDefault(partName);
    }
}