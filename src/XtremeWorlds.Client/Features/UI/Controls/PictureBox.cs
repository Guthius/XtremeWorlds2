using Microsoft.Xna.Framework;
using XtremeWorlds.Client.Features.UI.Styles;

namespace XtremeWorlds.Client.Features.UI.Controls;

public class PictureBox(Style style) : Control
{
    private readonly StylePart? _stylePartBackground = style.GetPart("Background");
    
    public string? ImagePath { get; set; }
    public int Alpha { get; set; } = 255;
    
    public override void Render(int x, int y)
    {
        _stylePartBackground?.Draw(GameClient.SpriteBatch, new Rectangle(X + x, Y + y, Width, Height));
        
        // TODO: Add a image property that points to a texture, draw if it is set...
        
        OnDraw();
    }
}