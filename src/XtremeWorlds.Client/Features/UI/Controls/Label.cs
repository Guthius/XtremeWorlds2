using Core.Globals;
using FontStashSharp;
using Microsoft.Xna.Framework;

namespace XtremeWorlds.Client.Features.UI.Controls;

public sealed class Label : Control
{
    public SpriteFontBase? Font { get; set; }
    public Color Color { get; set; } = Color.White;
    public Alignment Align { get; set; } = Alignment.Left;

    public override void Render(int x, int y)
    {
        if (Font is null || string.IsNullOrEmpty(Text))
        {
            return;
        }

        switch (Align)
        {
            default:
            case Alignment.Left:
                RenderLeftAligned(x, y, Font);
                break;

            case Alignment.Right:
                RenderRightAligned(x, y, Font);
                break;

            case Alignment.Center:
                RenderCenterAligned(x, y, Font);
                break;
        }

        OnDraw();
    }

    private void RenderLeftAligned(int x, int y, SpriteFontBase font)
    {
        var textSize = font.MeasureString(Text);
        var textWidth = (int) textSize.X;

        if (textWidth <= Width)
        {
            GameClient.SpriteBatch.DrawString(font, Text, new Vector2(X + x, Y + y), Color.Black);

            return;
        }

        // var lines = Array.Empty<string>();
        // var lineOffset = 0;
        //
        // TextRenderer.WordWrap(Text, Font, Width, ref lines);
        //
        // foreach (var line in lines)
        // {
        //     var size = TextRenderer.Fonts[Font].MeasureString(line);
        //     var padding = (int) (size.X / 6);
        //
        //     TextRenderer.RenderText(line,
        //         X + x + padding,
        //         Y + y + lineOffset,
        //         Color, Color.Black, Font);
        //
        //     lineOffset += 14;
        // }
    }

    private void RenderRightAligned(int x, int y, SpriteFontBase font)
    {
        var textSize = font.MeasureString(Text);
        var textWidth = (int) textSize.X;

        if (textWidth <= Width)
        {
            var offset = (Width - textWidth) / 2;
            
            GameClient.SpriteBatch.DrawString(font, Text, new Vector2(X + x + offset, Y + y), Color.Black);

            return;
        }


        //
        // var lines = Array.Empty<string>();
        // var lineOffset = 0;
        //
        // TextRenderer.WordWrap(Text, Font, Width, ref lines);
        //
        // foreach (var line in lines)
        // {
        //     var size = TextRenderer.Fonts[Font].MeasureString(line);
        //     var padding = (int) (size.X / 6);
        //
        //     TextRenderer.RenderText(line,
        //         X + Width - (int) size.X + x + padding,
        //         Y + y + lineOffset,
        //         Color, Color.Black, Font);
        //
        //     lineOffset += 14;
        // }
    }

    private void RenderCenterAligned(int x, int y, SpriteFontBase font)
    {
        var textSize = font.MeasureString(Text);
        var textWidth = (int) textSize.X;

        if (textWidth <= Width)
        {
            var offset = (Width - textWidth) / 2;
            
            GameClient.SpriteBatch.DrawString(font, Text, new Vector2(X + x + offset + 1, Y + y + 1), Color.Black);
            GameClient.SpriteBatch.DrawString(font, Text, new Vector2(X + x + offset, Y + y), Color.White);
            
            return;
        }
        //
        // var lines = Array.Empty<string>();
        // var lineOffset = 0;
        //
        // TextRenderer.WordWrap(Text, Font, Width, ref lines);
        //
        // foreach (var line in lines)
        // {
        //     var size = TextRenderer.Fonts[Font].MeasureString(line);
        //
        //     var dx = X + x + (Width - (int) size.X) / 2;
        //     var dy = Y + y + lineOffset;
        //
        //     TextRenderer.RenderText(line,
        //         dx, dy,
        //         Color, Color.Black, Font);
        //
        //     lineOffset += 14;
        // }
    }
}