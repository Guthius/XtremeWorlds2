using Core.Globals;
using Microsoft.Xna.Framework;

namespace XtremeWorlds.Client.Features.UI.Controls;

public sealed class ComboBox : Control
{
    private const int ArrowSprite = 66;

    public List<string> Items { get; } = [];
    
    public Design Design { get; set; } = Design.None;
    public Color Color { get; set; } = Color.White;

    public int SelectedIndex { get; set; } = -1;

    public override void Render(int x, int y)
    {
        switch (Design)
        {
            case Design.ComboBoxNormal:
                DesignRenderer.Render(Design.TextBlack, X + x, Y + y, Width, Height);

                if (SelectedIndex > 0)
                {
                    if (SelectedIndex <= Items.Count - 1)
                    {
                        TextRenderer.RenderText(Items[SelectedIndex], X + x, Y + y, Color, Color.Black);
                    }
                }

                var path = Path.Combine(DataPath.Gui, ArrowSprite.ToString());

                GameClient.RenderTexture(ref path, X + x + Width, Y + y, 0, 0, 5, 4, 5, 4);
                break;
        }
        
        OnDraw();
    }
}