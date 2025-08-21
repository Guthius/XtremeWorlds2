using Microsoft.Xna.Framework.Graphics;
using Serilog;

namespace XtremeWorlds.Client.Services;

public sealed class TextureService(GraphicsDevice graphicsDevice) : ITextureService
{
    private readonly Texture2D _emptyTexture = new(graphicsDevice, 1, 1);
    private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _textureLock = new();

    public Texture2D GetTexture(string assetPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(assetPath, nameof(assetPath));

        lock (_textureLock)
        {
            if (_textures.TryGetValue(assetPath, out var texture))
            {
                return texture;
            }

            texture = LoadTexture(assetPath);

            _textures.Add(assetPath, texture);

            return texture;
        }
    }

    private Texture2D LoadTexture(string assetPath)
    {
        try
        {
            var texture = Texture2D.FromFile(graphicsDevice, assetPath);

            Log.Information("Loaded texture '{Path}'", assetPath);

            return texture;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load texture '{Path}'", assetPath);

            return _emptyTexture;
        }
    }
}