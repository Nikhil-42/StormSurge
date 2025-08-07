using Godot;
using System;

public partial class Tutorial : Node {
    [Export] public Texture2D StartNormal;
    [Export] public Texture2D StartHover;
    [Export] public Texture2D StartPressed;

    private Label _label;
    private TextureButton _nextButton;
    private int _step = 0;

    private UI _ui;
    private ScreenFader _screenFader;

    public override void _Ready() {
        // Fade screen
        _screenFader = GetNode<ScreenFader>("ScreenFader");
        _screenFader.FadeIn();

        // Get references to UI nodes
        _label = GetNode<Label>("TutorialOverlay/TextureRect/VBoxContainer/Label");
        _nextButton = GetNode<TextureButton>("TutorialOverlay/TextureRect/VBoxContainer/TextureButton");

        // Connect button pressed signal
        _nextButton.Pressed += OnNextButtonPressed;

        // Get UI
        _ui = GetNode<UI>("Root/Control2");

        // Disable UI
        _ui.TechTreeEnabled = false;
        _ui.NotificationHistoryEnabled = false;

        // Enable tutorial mode for controlled nodes
        GetNodeOrNull<Node>("Root/CameraRig")?.Set("IsTutorialMode", true);
        GetNodeOrNull<Node>("Root/Storm")?.Set("IsTutorialMode", true);
        GetNodeOrNull<Node>("Root/World")?.Set("IsTutorialMode", true);

    }

    public override void _UnhandledInput(InputEvent @event) {
        // Eat all input (mouse & keyboard)
        GetViewport().SetInputAsHandled();
    }

    private async void OnNextButtonPressed() {
        _step++;

        switch (_step) {
            case 1:
                _label.Text = "You can click and drag to rotate the globe or use the mouse wheel to zoom. ";

                // Enable camera input
                GetNodeOrNull("Root/CameraRig")?.Set("IsTutorialMode", false);
                break;

            case 2:
                _label.Text = "Right click and drag anywhere in the ocean to spawn a storm.";

                // Enable storm spawn
                GetNodeOrNull("Root/Storm")?.Set("IsTutorialMode", false);
                break;

            case 3:
                _label.Text = "Spawning storms cost 100 solar each. You can see your current solar in the top left corner.";

                break;

            case 4:
                _label.Text = "You can view damage caused and other information by right clicking a region.";

                // Enable region data
                GetNodeOrNull("Root/World")?.Set("IsTutorialMode", false);
                break;

            case 5:
                _label.Text = "You will also get notifications about humanity's progress throughout the game.";

                _ui.Notify("Humanity is fighting back!");

                break;

            case 6:
                _label.Text = "Notification history can be viewed by clicking the i in the top right corner.";

                // enable notification history
                _ui.NotificationHistoryEnabled = true;

                break;

            case 7:
                _label.Text = "You can access the tech tree by clicking the storm icon in the top right corner.";

                // enable techtree
                _ui.TechTreeEnabled = true;

                break;

            case 8:
                _label.Text = "Spend solar to upgrade your storm or press the middle storm icon to exit.";

                break;

            case 9:
                _label.Text = "You are now ready to destroy the world!";

                _nextButton.TextureNormal = StartNormal;
                _nextButton.TextureHover = StartHover;
                _nextButton.TexturePressed = StartPressed;


                // Re-enable game control scripts
                GetNodeOrNull("Root/CameraRig")?.Set("IsTutorialMode", false);
                GetNodeOrNull("Root/Storm")?.Set("IsTutorialMode", false);
                break;
            case 10:

                await _screenFader.FadeOut(0.5f); // Fade to black

                // Load default
                GetTree().ChangeSceneToFile("res://Scenes/default.tscn");
                break;
        }
    }
}
