using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XtremeWorlds.Client.Features.UI.Styles;

namespace XtremeWorlds.Client.Features.UI.Controls;

public class Button(Style style) : Control
{
    private bool _isMouseOver;
    private bool _isMousePressed;

    private readonly StylePart? _stylePartNormal = style.GetPart("ButtonNormal");
    private readonly StylePart? _stylePartMouseOver = style.GetPart("ButtonHot");
    private readonly StylePart? _stylePartMousePressed = style.GetPart("ButtonPressed");

    public SpriteFontBase? Font { get; set; }
    public Texture2D? Icon { get; set; }
    public Vector2 IconOffset { get; set; }

    public override void Render(int x, int y)
    {
        var part = GetActiveStylePart();
        if (part is not null)
        {
            var destinationRectangle = new Rectangle(X + x, Y + y, Width, Height);

            part.Draw(GameClient.SpriteBatch, destinationRectangle);
        }

        if (Icon is not null)
        {
            var dx = X + x + (Width - Icon.Width) / 2;
            var dy = Y + y + (Height - Icon.Height) / 2;

            GameClient.SpriteBatch.Draw(Icon,
                new Vector2(
                    dx + IconOffset.X,
                    dy + IconOffset.Y),
                Color.White);
        }

        if (Font is not null && !string.IsNullOrEmpty(Text))
        {
            var textSize = Font.MeasureString(Text);
            var textX = X + x + (Width - textSize.X) / 2;
            var textY = Y + y + (Height - Font.LineHeight) / 2;

            GameClient.SpriteBatch.DrawString(Font, Text, new Vector2(textX + 1, textY + 1), Color.Black);
            GameClient.SpriteBatch.DrawString(Font, Text, new Vector2(textX, textY), Color.White);
        }

        OnDraw();
    }

    private StylePart? GetActiveStylePart()
    {
        StylePart? stylePart = null;

        if (_isMousePressed)
        {
            stylePart = _stylePartMousePressed;
        }
        else if (_isMouseOver)
        {
            stylePart = _stylePartMouseOver;
        }

        return stylePart ?? _stylePartNormal;
    }

    protected override void OnMouseEnter()
    {
        _isMouseOver = true;

        base.OnMouseEnter();
    }

    protected override void OnMouseLeave()
    {
        _isMouseOver = false;

        base.OnMouseLeave();
    }

    protected override void OnMousePressed(int x, int y)
    {
        Gui.CaptureMouse(this);

        _isMousePressed = true;

        base.OnMousePressed(x, y);
    }

    protected override void OnMouseReleased(int x, int y)
    {
        Gui.ReleaseMouse();

        _isMousePressed = false;

        if (Contains(x, y))
        {
            OnClick();
        }
        else
        {
            _isMouseOver = false;
        }

        base.OnMouseReleased(x, y);
    }
}