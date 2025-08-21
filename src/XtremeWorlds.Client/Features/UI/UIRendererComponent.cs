using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XtremeWorlds.Client.Features.UI;

public sealed class UIRendererComponent(Game game) : DrawableGameComponent(game)
{
    public override void Draw(GameTime gameTime)
    {
        GameClient.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        
        Gui.Render();
        
        GameClient.SpriteBatch.End();
    }
}