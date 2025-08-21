using Microsoft.Xna.Framework.Graphics;

namespace XtremeWorlds.Client.Services;

public interface ITextureService
{
    Texture2D GetTexture(string assetPath);
}