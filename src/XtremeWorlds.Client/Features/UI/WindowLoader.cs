using System.Xml;
using Core.Globals;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XtremeWorlds.Client.Features.UI.Controls;
using XtremeWorlds.Client.Features.UI.Styles;

namespace XtremeWorlds.Client.Features.UI;

public static class WindowLoader
{
    private const Font DefaultWindowFont = Font.Georgia;

    private const string DefaultWindowStyle = "Window";
    private const string DefaultButtonStyle = "Button";
    private const string DefaultCheckBoxStyle = "CheckBox";

    private const int DefaultFontSize = 18;

    private sealed record CommonAttributes(
        string Name,
        string Text,
        int X,
        int Y,
        int Width,
        int Height,
        SpriteFontBase? Font,
        bool Enabled,
        bool Visible,
        Style Style);

    public static Window FromLayout(string layoutName)
    {
        var basePath = Path.Combine("Content", "Skins");

        var path = Path.Combine(basePath, "Layouts", layoutName + ".xml");
        if (!File.Exists(path))
        {
            throw new UIException(
                $"Unable to load window layout '{layoutName}'. " +
                $"Layout file '{path}' does not exist.");
        }

        using var stream = File.OpenRead(path);

        using var xmlReader = XmlReader.Create(stream, new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true
        });

        xmlReader.MoveToContent();
        if (xmlReader.NodeType != XmlNodeType.Element ||
            xmlReader.Name != "Window")
        {
            throw new UIException("Window layout file is missing root 'Window' element.");
        }

        return ReadWindow(xmlReader, basePath);
    }

    private static Window ReadWindow(XmlReader xmlReader, string basePath)
    {
        var name = xmlReader.GetAttribute("Name");
        var text = xmlReader.GetAttribute("Caption");
        var fontName = xmlReader.GetAttribute("Font");
        var font = GetFontByName(fontName, DefaultWindowFont);
        var sizeStr = xmlReader.GetAttribute("Size");
        var size = GetVector(sizeStr);
        var positionStr = xmlReader.GetAttribute("Position");
        var position = GetVector(positionStr);
        var icon = xmlReader.GetAttribute("Icon");
        var startPosition = xmlReader.GetAttribute("StartPosition");
        var visible = GetBoolean(xmlReader.GetAttribute("Visible"), true);
        var canDrag = GetBoolean(xmlReader.GetAttribute("CanDrag"), true);
        var canFocus = GetBoolean(xmlReader.GetAttribute("CanFocus"), true);
        var clickthrough = GetBoolean(xmlReader.GetAttribute("Clickthrough"));
        var styleName = xmlReader.GetAttribute("Style") ?? DefaultWindowStyle;
        var style = StyleManager.Get(styleName, GameClient.TextureService);
        var showTitlebar = GetBoolean(xmlReader.GetAttribute("ShowTitlebar"), true);

        var window = new Window(style)
        {
            Name = name ?? string.Empty,
            Text = text ?? string.Empty,
            X = (int) position.X,
            Y = (int) position.Y,
            InitialX = (int) position.X,
            InitialY = (int) position.Y,
            Width = (int) size.X,
            Height = (int) size.Y,
            Visible = visible,
            CanDrag = canDrag,
            Font = font,
            Icon = GetIcon(icon),
            CanFocus = canFocus,
            Clickthrough = clickthrough,
            ShowTitleBar = showTitlebar
        };

        Gui.Register(window);

        if (!string.IsNullOrEmpty(startPosition))
        {
            if (startPosition.Equals("Center", StringComparison.OrdinalIgnoreCase) ||
                startPosition.Equals("CenterScreen", StringComparison.OrdinalIgnoreCase))
            {
                window.MoveToCenter();
            }
        }

        while (xmlReader.Read())
        {
            if (xmlReader.NodeType == XmlNodeType.Element)
            {
                ReadControl(xmlReader, basePath, window);
            }
            else if (xmlReader.NodeType == XmlNodeType.EndElement)
            {
                break;
            }
        }

        return window;
    }

    private static void ReadControl(XmlReader xmlReader, string basePath, Window window)
    {
        switch (xmlReader.Name)
        {
            case "Button":
                ReadButton(xmlReader, basePath, window);
                break;

            case "CheckBox":
                ReadCheckBox(xmlReader, window);
                break;

            case "Label":
                ReadLabel(xmlReader, window);
                break;

            case "PictureBox":
                ReadPictureBox(xmlReader, window);
                break;

            case "TextBox":
                ReadTextBox(xmlReader, window);
                break;
        }

        if (!xmlReader.IsEmptyElement)
        {
            xmlReader.Skip();
        }
    }

    private static void ReadButton(XmlReader xmlReader, string basePath, Window window)
    {
        var attribs = ReadCommonAttributesWithStyle(xmlReader, window, DefaultButtonStyle);
        var icon = xmlReader.GetAttribute("Icon");
        var iconTexture = GetTexture(icon, basePath);
        var iconOffset = GetVector(xmlReader.GetAttribute("IconOffset"));

        window.Controls.Add(new Button(attribs.Style)
        {
            Parent = window,

            Name = attribs.Name,
            X = attribs.X,
            Y = attribs.Y,
            Width = attribs.Width,
            Height = attribs.Height,
            Visible = attribs.Visible,

            Text = attribs.Text,
            Enabled = attribs.Enabled,

            Font = attribs.Font,
            Icon = iconTexture,
            IconOffset = iconOffset
        });
    }

    private static void ReadCheckBox(XmlReader xmlReader, Window window)
    {
        var attribs = ReadCommonAttributesWithStyle(xmlReader, window, DefaultCheckBoxStyle);

        window.Controls.Add(new CheckBox(attribs.Style)
        {
            Parent = window,

            Name = attribs.Name,
            X = attribs.X,
            Y = attribs.Y,
            Width = attribs.Width,
            Height = attribs.Height,
            Visible = attribs.Visible,

            Text = attribs.Text,
            Enabled = attribs.Enabled,

            Font = attribs.Font
        });
    }

    private static void ReadLabel(XmlReader xmlReader, Window window)
    {
        var attribs = ReadCommonAttributes(xmlReader, window, null);
        var alignmentName = xmlReader.GetAttribute("Align");
        var alignment = GetAlignmentByName(alignmentName, Alignment.Left);

        window.Controls.Add(new Label
        {
            Parent = window,

            Name = attribs.Name,
            X = attribs.X,
            Y = attribs.Y,
            Width = attribs.Width,
            Height = attribs.Height,
            Visible = attribs.Visible,

            Text = attribs.Text,
            Enabled = attribs.Enabled,

            Font = attribs.Font,
            Align = alignment
        });
    }

    private static void ReadPictureBox(XmlReader xmlReader, Window window)
    {
        // var attribs = ReadCommonAttributes(xmlReader, window, "PictureBox");
        var name = xmlReader.GetAttribute("Name");
        var positionStr = xmlReader.GetAttribute("Position");
        var position = GetVector(positionStr);
        var sizeStr = xmlReader.GetAttribute("Size");
        var size = GetVector(sizeStr);
        var image = GetInt32(xmlReader.GetAttribute("Image"));
        var visible = GetBoolean(xmlReader.GetAttribute("Visible"), true);
        var styleName = xmlReader.GetAttribute("Style");
        var style = GetStyle(styleName);

        window.Controls.Add(new PictureBox(style)
        {
            Parent = window,

            Name = name ?? string.Empty,
            X = (int) position.X,
            Y = (int) position.Y,
            Width = (int) size.X,
            Height = (int) size.Y,
            Visible = visible,

            ImagePath = string.Empty,
            Alpha = 255
        });
    }

    private static void ReadTextBox(XmlReader xmlReader, Window window)
    {
        var attribs = ReadCommonAttributesWithStyle(xmlReader, window, "TextBox");
        var censor = GetBoolean(xmlReader.GetAttribute("Censor"));

        window.Controls.Add(new TextBox(attribs.Style)
        {
            Parent = window,

            Name = attribs.Name,
            X = attribs.X,
            Y = attribs.Y,
            Width = attribs.Width,
            Height = attribs.Height,
            Visible = attribs.Visible,

            Text = attribs.Text,

            Font = attribs.Font,
            Censor = censor
        });
    }

    private static Style GetStyle(string? styleName)
    {
        if (string.IsNullOrEmpty(styleName))
        {
            return Style.Empty;
        }

        return StyleManager.Get(styleName, GameClient.TextureService);
    }

    private static CommonAttributes ReadCommonAttributes(XmlReader xmlReader, Window parentWindow, Style? style)
    {
        var position = GetVector(xmlReader.GetAttribute("Position"));
        var size = GetVector(xmlReader.GetAttribute("Size"));
        var fontName = xmlReader.GetAttribute("Font");
        var fontSize = GetInt32(xmlReader.GetAttribute("FontSize"), DefaultFontSize);

        var x = (int) position.X;
        if (x < 0)
        {
            x = parentWindow.Width + x;
        }

        var y = (int) position.Y;
        if (y < 0)
        {
            y = parentWindow.Height + y;
        }

        return new CommonAttributes(
            Name: xmlReader.GetAttribute("Name") ?? string.Empty,
            Text: xmlReader.GetAttribute("Text") ?? string.Empty,
            X: x, Y: y,
            Width: (int) size.X,
            Height: (int) size.Y,
            Font: FontManager.GetFont(fontName, fontSize),
            Enabled: GetBoolean(xmlReader.GetAttribute("Enabled"), true),
            Visible: GetBoolean(xmlReader.GetAttribute("Visible"), true),
            Style: style ?? Style.Empty);
    }

    private static CommonAttributes ReadCommonAttributesWithStyle(XmlReader xmlReader, Window parentWindow, string defaultStyleName)
    {
        var styleName = xmlReader.GetAttribute("Style") ?? defaultStyleName;
        var style = StyleManager.Get(styleName, GameClient.TextureService);

        return ReadCommonAttributes(xmlReader, parentWindow, style);
    }

    private static Font GetFontByName(string? fontName, Font defaultValue)
    {
        if (string.IsNullOrEmpty(fontName))
        {
            return defaultValue;
        }

        if (Enum.TryParse<Font>(fontName, true, out var result))
        {
            return result;
        }

        return defaultValue;
    }

    private static Alignment GetAlignmentByName(string? alignmentName, Alignment defaultValue)
    {
        if (string.IsNullOrEmpty(alignmentName))
        {
            return defaultValue;
        }

        if (Enum.TryParse<Alignment>(alignmentName, true, out var result))
        {
            return result;
        }

        return defaultValue;
    }

    private static Vector2 GetVector(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Vector2.Zero;
        }

        var comma = value.IndexOf(',');
        if (comma == -1)
        {
            return Vector2.Zero;
        }

        if (!int.TryParse(value.AsSpan(0, comma), out var x)) x = 0;
        if (!int.TryParse(value.AsSpan(comma + 1), out var y)) y = 0;

        return new Vector2(x, y);
    }

    private static int GetIcon(string? icon)
    {
        if (string.IsNullOrEmpty(icon))
        {
            return 0;
        }

        if (int.TryParse(icon, out var result))
        {
            return result;
        }

        return 0;
    }

    private static bool GetBoolean(string? value, bool defaultValue = false)
    {
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        return defaultValue;
    }

    private static int GetInt32(string? value, int defaultValue = 0)
    {
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        if (int.TryParse(value, out var result))
        {
            return result;
        }

        return defaultValue;
    }

    private static Texture2D? GetTexture(string? path, string basePath)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        return GameClient.TextureService.GetTexture(Path.Combine(basePath, "Textures", path));
    }
}