using FontStashSharp;
using Microsoft.Xna.Framework;
using XtremeWorlds.Client.Features.UI.Styles;

namespace XtremeWorlds.Client.Features.UI.Controls;

public sealed class CheckBox(Style style) : Button(style)
{
    private readonly StylePart? _stylePartUnchecked = style.GetPart("CheckBoxUnchecked");
    private readonly StylePart? _stylePartChecked = style.GetPart("CheckBoxChecked");

    public bool IsChecked { get; set; } = true;

    public override void Render(int x, int y)
    {
        var offset = 0;

        var part = IsChecked ? _stylePartChecked : _stylePartUnchecked;
        if (part is not null)
        {
            var dx = X + x;
            var dy = Y + y + (Height - part.Height) / 2;

            offset = part.Width + 2;

            part.Draw(GameClient.SpriteBatch, new Vector2(dx, dy));
        }

        if (Font is not null && !string.IsNullOrEmpty(Text))
        {
            var dx = X + x + offset;
            var dy = Y + y + (Height - Font.LineHeight) / 2;

            GameClient.SpriteBatch.DrawString(Font, Text, new Vector2(dx + 1, dy + 1), Color.Black);
            GameClient.SpriteBatch.DrawString(Font, Text, new Vector2(dx, dy), Color.White);
        }

        OnDraw();
    }

    protected override void OnClick()
    {
        if (Enabled)
        {
            IsChecked = !IsChecked;
        }

        base.OnClick();
    }
}