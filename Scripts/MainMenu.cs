using Godot;

public partial class MainMenu : Control {
    [Export] private PackedScene _nextScene;
    [Export] private Node3D _globe;

    private ScreenFader _screenFader;

    public override void _Ready() {
        _screenFader = GetNode<ScreenFader>("ScreenFader");
        _screenFader.FadeIn();
    }

    private async void _on_start_button_pressed() {
        await _screenFader.FadeOut(0.5f); // Fade to black

        var error = GetTree().ChangeSceneToPacked(_nextScene);

        if (error != Error.Ok) {
            GD.PrintErr($"Failed to load scene: {error}");
        }
    }

    [Export] private float _globeRotationSpeed = 4f;

    public override void _Process(double delta) {
        _globe.RotateY(Mathf.DegToRad(_globeRotationSpeed * (float)delta));
    }
}
