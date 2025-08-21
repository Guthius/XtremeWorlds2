using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using XtremeWorlds.Client.Services;

namespace XtremeWorlds.Client.Features.UI.Styles;

public static class StyleManager
{
    private static readonly Dictionary<string, Style> Styles = new(StringComparer.OrdinalIgnoreCase);

    public static Style Get(string styleName, ITextureService textureService)
    {
        if (Styles.TryGetValue(styleName, out var style))
        {
            return style;
        }

        var basePath = Path.Combine("Content", "Skins");

        var path = Path.Combine(basePath, "Styles", styleName + ".xml");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Unable to load style '{styleName}'. " +
                $"File '{path}' not found.");
        }

        using var stream = File.OpenRead(path);
        using var xmlReader = XmlReader.Create(stream, new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true
        });

        xmlReader.MoveToContent();
        if (xmlReader.NodeType != XmlNodeType.Element ||
            xmlReader.Name != "Style")
        {
            throw new UIException("Style file is missing root 'Style' element.");
        }

        var texturePath = xmlReader.GetAttribute("Texture");
        if (string.IsNullOrEmpty(texturePath))
        {
            throw new UIException("Style definition is missing 'Texture' attribute.");
        }

        texturePath = Path.Combine(basePath, "Textures", texturePath);
        if (!File.Exists(texturePath))
        {
            throw new UIException($"Unable to load style texture '{texturePath}'.");
        }

        var texture = textureService.GetTexture(texturePath);

        var dictionary = new Dictionary<string, StylePart>(StringComparer.OrdinalIgnoreCase);
        while (xmlReader.Read())
        {
            if (xmlReader.NodeType == XmlNodeType.Element)
            {
                if (xmlReader.Name == "Part")
                {
                    ReadStylePart(xmlReader, texture, dictionary);
                }

                if (!xmlReader.IsEmptyElement)
                {
                    xmlReader.Skip();
                }
            }
            else if (xmlReader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }
        }

        Styles[styleName] = style = new Style(dictionary);

        return style;
    }

    private static void ReadStylePart(XmlReader xmlReader, Texture2D texture, Dictionary<string, StylePart> dictionary)
    {
        var name = xmlReader.GetAttribute("Name");
        if (string.IsNullOrEmpty(name))
        {
            throw new UIException("Style part is missing 'Name' attribute.");
        }

        var textureRect = GetRectangle(xmlReader.GetAttribute("TextureRect"));
        if (textureRect is null)
        {
            Log.Warning("Style part {PartName} is missing 'TextureRect' attribute", name);

            textureRect = new Rectangle(0, 0, texture.Width, texture.Height);
        }

        var ninePatchRect = GetRectangle(xmlReader.GetAttribute("NinePatchRect"));

        dictionary[name] = new StylePart(name, texture, textureRect.Value, ninePatchRect);
    }

    private static Rectangle? GetRectangle(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var tokens = value.Split(',');
        if (tokens.Length != 4)
        {
            return null;
        }

        if (!int.TryParse(tokens[0], out var x) ||
            !int.TryParse(tokens[1], out var y) ||
            !int.TryParse(tokens[2], out var width) ||
            !int.TryParse(tokens[3], out var height))
        {
            return null;
        }

        return new Rectangle(x, y, width, height);
    }
}