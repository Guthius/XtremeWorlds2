using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XtremeWorlds.Client.Features.UI.Styles;

public sealed class StylePart(string name, Texture2D texture, Rectangle textureRect, Rectangle? ninePatch)
{
    private Rectangle _ninePatchDestinationRectangle;
    private Rectangle[] _ninePatchDestinationParts = [];
    private readonly Rectangle[]? _ninePatchSourceParts = BuildNinePatchSourceParts(textureRect, ninePatch);

    public int Width => textureRect.Width;
    public int Height => textureRect.Height;

    public override string ToString()
    {
        return name;
    }

    public void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        spriteBatch.Draw(texture, position, textureRect, Color.White);
    }

    public void Draw(SpriteBatch spriteBatch, Rectangle destinationRectangle)
    {
        if (destinationRectangle == Rectangle.Empty)
        {
            return;
        }

        if (_ninePatchSourceParts is not null)
        {
            DrawNinePatch(spriteBatch, destinationRectangle, _ninePatchSourceParts);
            return;
        }

        spriteBatch.Draw(texture, destinationRectangle, textureRect, Color.White);
    }

    private void DrawNinePatch(SpriteBatch spriteBatch, Rectangle destinationRectangle, Rectangle[] ninePatchParts)
    {
        if (_ninePatchDestinationRectangle != destinationRectangle)
        {
            _ninePatchDestinationRectangle = destinationRectangle;
            _ninePatchDestinationParts = BuildNinePatchDestinationParts(destinationRectangle, ninePatchParts);
        }

        for (var i = 0; i < ninePatchParts.Length; i++)
        {
            spriteBatch.Draw(texture, _ninePatchDestinationParts[i], ninePatchParts[i], Color.White);
        }
    }

    private static Rectangle[] BuildNinePatchDestinationParts(Rectangle destinationRectangle, Rectangle[] sourceParts)
    {
        var parts = new Rectangle[9];

        var widthLeft = sourceParts[0].Width;
        var widthRight = sourceParts[2].Width;
        var width = destinationRectangle.Width - widthLeft - widthRight;

        var heightTop = sourceParts[0].Height;
        var heightBottom = sourceParts[6].Height;
        var height = destinationRectangle.Height - heightTop - heightBottom;

        var x1 = destinationRectangle.Left;
        var x2 = x1 + widthLeft;
        var x3 = x2 + width;

        var y1 = destinationRectangle.Top;
        var y2 = y1 + heightTop;
        var y3 = y2 + height;

        parts[0] = new Rectangle(x1, y1, widthLeft, heightTop); // Top Left Corner
        parts[1] = new Rectangle(x2, y1, width, heightTop); // Top Border
        parts[2] = new Rectangle(x3, y1, widthRight, heightTop); // Top Right Corner
        parts[3] = new Rectangle(x1, y2, widthLeft, height); // Left Border
        parts[4] = new Rectangle(x2, y2, width, height); // Center
        parts[5] = new Rectangle(x3, y2, widthRight, height); // Right Border
        parts[6] = new Rectangle(x1, y3, widthLeft, heightBottom); // Bottom Left Corner
        parts[7] = new Rectangle(x2, y3, width, heightBottom); // Bottom Border
        parts[8] = new Rectangle(x3, y3, widthRight, heightBottom); // Bottom Right Corner

        return parts;
    }

    private static Rectangle[]? BuildNinePatchSourceParts(Rectangle textureRect, Rectangle? nineSlice)
    {
        if (nineSlice is null)
        {
            return null;
        }

        var parts = new Rectangle[9];

        var widthLeft = nineSlice.Value.Left;
        var widthRight = textureRect.Width - nineSlice.Value.Right;
        var width = textureRect.Width - widthLeft - widthRight;

        var heightTop = nineSlice.Value.Top;
        var heightBottom = textureRect.Height - nineSlice.Value.Bottom;
        var height = textureRect.Height - heightTop - heightBottom;

        var x1 = textureRect.Left;
        var x2 = x1 + widthLeft;
        var x3 = x2 + width;

        var y1 = textureRect.Top;
        var y2 = y1 + heightTop;
        var y3 = y2 + height;

        parts[0] = new Rectangle(x1, y1, widthLeft, heightTop); // Top Left Corner
        parts[1] = new Rectangle(x2, y1, width, heightTop); // Top Border
        parts[2] = new Rectangle(x3, y1, widthRight, heightTop); // Top Right Corner
        parts[3] = new Rectangle(x1, y2, widthLeft, height); // Left Border
        parts[4] = new Rectangle(x2, y2, width, height); // Center
        parts[5] = new Rectangle(x3, y2, widthRight, height); // Right Border
        parts[6] = new Rectangle(x1, y3, widthLeft, heightBottom); // Bottom Left Corner
        parts[7] = new Rectangle(x2, y3, width, heightBottom); // Bottom Border
        parts[8] = new Rectangle(x3, y3, widthRight, heightBottom); // Bottom Right Corner

        return parts;
    }
}