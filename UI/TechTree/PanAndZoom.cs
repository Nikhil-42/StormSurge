using Godot;

public partial class PanAndZoom : Control {
    private Vector2 _dragStart;
    private bool _dragging = false;
    private float _zoom = 1f;

    private const float ZoomStep = 0.1f;
    private const float MinZoom = 0.3f;
    private const float MaxZoom = 3.0f;

    private Control _content;

    public override void _Ready() {
        _content = GetChild<Control>(0);
    }

    public override void _Input(InputEvent @event) {
        if (@event is InputEventMouseButton mouseEvent) {
            if (mouseEvent.ButtonIndex == MouseButton.Left) {
                if (mouseEvent.Pressed) {
                    _dragging = true;
                    _dragStart = GetGlobalMousePosition();
                } else {
                    _dragging = false;
                }
            } else if (mouseEvent.ButtonIndex == MouseButton.WheelUp || mouseEvent.ButtonIndex == MouseButton.WheelDown) {
                float direction = mouseEvent.ButtonIndex == MouseButton.WheelUp ? 1 : -1;
                float newZoom = Mathf.Clamp(_zoom + direction * ZoomStep, MinZoom, MaxZoom);

                // Zoom centered on cursor
                Vector2 cursorGlobal = GetGlobalMousePosition();
                Vector2 toCursor = cursorGlobal - GlobalPosition;

                float scaleRatio = newZoom / _zoom;
                _zoom = newZoom;
                Scale = new Vector2(_zoom, _zoom);

                Vector2 newToCursor = toCursor * scaleRatio;
                Vector2 offset = newToCursor - toCursor;

                _content.Position -= offset / _zoom; // Adjust position to keep zoom centered on mouse
                GetViewport().SetInputAsHandled();
            }
        } else if (@event is InputEventMouseMotion motion && _dragging) {
            Vector2 delta = motion.Relative;
            _content.Position += delta / _zoom; // Adjust position based on zoom level
        }
    }
}
