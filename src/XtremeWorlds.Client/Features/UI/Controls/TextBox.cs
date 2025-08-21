using FontStashSharp;
using Microsoft.Xna.Framework;
using XtremeWorlds.Client.Features.UI.Styles;

namespace XtremeWorlds.Client.Features.UI.Controls;

public sealed class TextBox(Style style) : Control
{
    private const int Padding = 5;

    private readonly StylePart? _stylePartBackground = style.GetPart("Background");
    private string _passwordText = string.Empty;

    public SpriteFontBase? Font { get; set; }
    public Color Color { get; set; } = new(50, 50, 50, 255);
    public bool Censor { get; set; }

    public override void Render(int x, int y)
    {
        _stylePartBackground?.Draw(GameClient.SpriteBatch, new Rectangle(X + x, Y + y, Width, Height));

        if (Font is null)
        {
            OnDraw();

            return;
        }

        var text = Censor ? GetPasswordText() : Text;

        var dx = X + x + Padding;
        var dy = Y + y + (Height - Font.LineHeight) / 2;

        GameClient.SpriteBatch.DrawString(Font, text, new Vector2(dx, dy), Color);

        OnDraw();
    }

    private string GetPasswordText()
    {
        if (_passwordText.Length != Text.Length)
        {
            _passwordText = new string('*', Text.Length);
        }

        return _passwordText;
    }
}