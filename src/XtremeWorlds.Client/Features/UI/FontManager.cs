using FontStashSharp;
using Serilog;

namespace XtremeWorlds.Client.Features.UI;

public static class FontManager
{
    private const string DefaultFontName = "Coolvetica";
    
    private static readonly Dictionary<string, FontSystem> Fonts = new(StringComparer.OrdinalIgnoreCase);

    public static void Reset()
    {
        Fonts.Clear();
    }

    public static void Load(string fontName, params ReadOnlySpan<string> fileNames)
    {
        var fontSystem = new FontSystem();

        foreach (var fileName in fileNames)
        {
            var path = Path.Combine("Content", "Fonts", fileName);
            if (!File.Exists(path))
            {
                Log.Warning("Unable to load font '{FontName}'. File '{Path}' does not exist.", fontName, path);
                continue;
            }

            fontSystem.AddFont(File.ReadAllBytes(path));
        }

        Log.Information("Loaded font '{FontName}'", fontName);

        Fonts[fontName] = fontSystem;
    }

    public static SpriteFontBase? GetFont(string? fontName, int size)
    {
        fontName ??= DefaultFontName;
        
        if (!Fonts.TryGetValue(fontName, out var fontSystem))
        {
            return null;
        }

        return fontSystem.GetFont(size);
    }
}