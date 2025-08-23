using Godot;

public partial class ScrollArea : Control {
    private Vector2 dragStart;
    private Vector2 offset = Vector2.Zero;
    private bool dragging = false;

    private float zoom = 1f;
    private const float ZoomStep = 0.1f;
    private const float ZoomMin = 0.5f;
    private const float ZoomMax = 2.5f;

    public override void _GuiInput(InputEvent @event) {
        // Zoom 
        if (@event is InputEventMouseButton mb && mb.IsPressed()) {
            if (mb.ButtonIndex == MouseButton.WheelUp)
                SetZoom(zoom + ZoomStep);
            else if (mb.ButtonIndex == MouseButton.WheelDown)
                SetZoom(zoom - ZoomStep);
        }

        // Begin drag
        if (@event is InputEventMouseButton dragBtn) {
            if (dragBtn.ButtonIndex == MouseButton.Left && dragBtn.IsPressed()) {
                dragging = true;
                dragStart = GetGlobalMousePosition();
            } else if (dragBtn.ButtonIndex == MouseButton.Left && !dragBtn.IsPressed()) {
                dragging = false;
            }
        }

        // Do drag
        if (@event is InputEventMouseMotion motion && dragging) {
            Vector2 delta = GetGlobalMousePosition() - dragStart;
            offset += delta;
            dragStart = GetGlobalMousePosition();
            UpdateTransform();
        }
    }

    private void SetZoom(float newZoom) {
        zoom = Mathf.Clamp(newZoom, ZoomMin, ZoomMax);
        UpdateTransform();
    }

    private void UpdateTransform() {
        Scale = new Vector2(zoom, zoom);
        Position = offset;
    }
}
