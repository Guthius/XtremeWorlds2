namespace XtremeWorlds.Client.Features.UI.Controls;

public abstract class Control : Component
{
    public string Text { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;

    public event Action? MouseEnter;
    public event Action? MouseLeave;

    public abstract void Render(int x, int y);

    public void HandleMouseEnter()
    {
        OnMouseEnter();
    }

    protected virtual void OnMouseEnter()
    {
        MouseEnter?.Invoke();
    }

    public void HandleMouseLeave()
    {
        OnMouseLeave();
    }

    protected virtual void OnMouseLeave()
    {
        MouseLeave?.Invoke();
    }
}