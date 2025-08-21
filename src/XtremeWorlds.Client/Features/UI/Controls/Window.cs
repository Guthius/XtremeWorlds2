using Core.Globals;
using Microsoft.Xna.Framework;
using XtremeWorlds.Client.Features.States;
using XtremeWorlds.Client.Features.UI.Styles;

namespace XtremeWorlds.Client.Features.UI.Controls;

public sealed class Window(Style style) : Component
{
    private const int TitleBarHeight = 25;
    
    public bool ShowTitleBar { get; set; } = true;

    private readonly StylePart? _stylePartTitleBar = style.GetPart("WindowTitleBar");
    private readonly StylePart? _stylePartBack = style.GetPart("WindowBack");
    
    
    
    private Control? _mouseOverControl;
    private bool _dragging;
    private int _dragOffsetX;
    private int _dragOffsetY;

    public int InitialX { get; set; }
    public int InitialY { get; set; }
    public int MovedX { get; set; }
    public int MovedY { get; set; }

    public bool CanDrag { get; set; } = true;
    public bool CanFocus { get; set; } = true;

    public Font Font { get; set; }
    public string Text { get; set; } = string.Empty;
    public int XOffset { get; set; }
    public int YOffset { get; set; }
    public int Icon { get; set; }
    public long Value { get; set; }
    public int Group { get; set; }
    public bool Clickthrough { get; set; }
    public Control? ParentControl { get; set; }
    public List<string> List { get; set; } = []; // Drop down items?
    
    // Controls in this window
    public List<Control> Controls { get; } = [];
    public Control? LastControl { get; set; }
    public Control? ActiveControl { get; set; }

    public void Render()
    {
        _stylePartBack?.Draw(GameClient.SpriteBatch, new Rectangle(X, Y, Width, Height));

        if (ShowTitleBar)
        {
            _stylePartTitleBar?.Draw(GameClient.SpriteBatch, new Rectangle(X, Y, Width, TitleBarHeight));

            TextRenderer.RenderText(Text, X + 32, Y + 2, Color.White, Color.Black);
        }

        OnDraw();

        RenderChildren();
    }

    private void RenderChildren()
    {
        foreach (var control in Controls.Where(x => x.Visible))
        {
            control.Render(X, Y);
        }
    }

    public void MoveToCenter()
    {
        X = (GameState.ResolutionWidth - Width) / 2;
        Y = (GameState.ResolutionHeight - Height) / 2;
        InitialX = X;
        InitialY = Y;
    }

    public T GetChild<T>(string controlName) where T : Control
    {
        foreach (var control in Controls)
        {
            if (!string.Equals(control.Name, controlName, StringComparison.CurrentCultureIgnoreCase))
            {
                continue;
            }

            if (control is not T result)
            {
                throw new UIException("Control " + controlName + " is not of type " + typeof(T).Name);
            }

            return result;
        }

        throw new UIException("Control not found: " + controlName);
    }

    public Control GetChild(string controlName)
    {
        foreach (var control in Controls)
        {
            if (string.Equals(control.Name, controlName, StringComparison.CurrentCultureIgnoreCase))
            {
                return control;
            }
        }

        throw new UIException("Control not found: " + controlName);
    }

    protected override void OnMouseMove(int x, int y)
    {
        if (_dragging)
        {
            (x, y) = PointToScreen(x, y);

            X = x - _dragOffsetX;
            Y = y - _dragOffsetY;
            return;
        }

        if (_mouseOverControl is not null)
        {
            _mouseOverControl.HandleMouseLeave();
            _mouseOverControl = null;
        }

        for (var index = Controls.Count - 1; index >= 0; index--)
        {
            var control = Controls[index];
            if (!control.Visible)
            {
                continue;
            }

            if (!control.Contains(x, y))
            {
                continue;
            }

            if (_mouseOverControl != control)
            {
                _mouseOverControl?.HandleMouseLeave();
            }

            _mouseOverControl = control;
            _mouseOverControl.HandleMouseEnter();
            _mouseOverControl.HandleMouseMove(x, y);
            return;
        }
    }

    protected override void OnMousePressed(int x, int y)
    {
        Gui.MoveToFront(this);

        if (_mouseOverControl is not null &&
            _mouseOverControl.Contains(x, y))
        {
            _mouseOverControl.HandleMousePressed(x, y);
            return;
        }

        if (!CanDrag || y > TitleBarHeight)
        {
            return;
        }

        _dragging = true;
        _dragOffsetX = x;
        _dragOffsetY = y;

        Gui.CaptureMouse(this);
    }

    protected override void OnMouseReleased(int x, int y)
    {
        if (_dragging)
        {
            _dragging = false;

            Gui.ReleaseMouse();
            return;
        }

        if (_mouseOverControl is not null &&
            _mouseOverControl.Contains(x, y))
        {
            _mouseOverControl.HandleMouseReleased(x, y);
        }
    }
}