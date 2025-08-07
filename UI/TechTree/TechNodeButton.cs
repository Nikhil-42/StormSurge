using Godot;

public partial class TechNodeButton : TextureButton {
    [Export] public string NodeName;
    [Signal] public delegate void NodePurchasedEventHandler(string nodeName);
    [Signal] public delegate void HoveredEventHandler(string nodeName, Vector2 mousePosition);
    [Signal] public delegate void UnhoveredEventHandler();

    private Label NameLabel;
    public TechNode<GlobalVars> BoundNode { get; set; }
    private bool _bound = false;
    private bool isLocked = false;

    private AudioStreamPlayer purchaseSound;

    public override void _Ready() {
        purchaseSound = GetNode<AudioStreamPlayer>("PurchaseSound");

        NameLabel = GetNode<Label>("Label");
        Pressed += OnPressed;

        // Bind to the tree node
        var tree = GameManager.Instance?.Game?.stormTree;
        if (tree == null) return;

        BoundNode = tree.GetNode(NodeName);
        if (BoundNode == null) return;

        _bound = true;
        NameLabel.Text = BoundNode.Name;

        UpdateVisual();

        //GD.Print($"[Bind] Button {NodeName} → BoundNode: {BoundNode} (Hash: {BoundNode.GetHashCode()})");

        // Hover signals
        MouseEntered += () => EmitSignal(SignalName.Hovered, NodeName, GetGlobalMousePosition());
        MouseExited += () => EmitSignal(SignalName.Unhovered);
    }

    public void UpdateVisual() {
        if (!_bound) {
            // Bind to the tree node
            var tree = GameManager.Instance?.Game?.stormTree;
            if (tree == null) return;

            BoundNode = tree.GetNode(NodeName);
            if (BoundNode == null) return;

            _bound = true;
            NameLabel.Text = BoundNode.Name;

            UpdateVisual();
        }

        if (BoundNode == null) {
            GD.PrintErr($"[TechNodeButton] {NodeName} has no BoundNode.");
            return;
        }

        ToggleMode = true;

        if (BoundNode.Purchased) {
            ButtonPressed = true;
            isLocked = true;
            NameLabel.AddThemeColorOverride("font_color", Colors.White);
            Modulate = new Color(1f, 1f, 1f, 1f); // Set the button to full brightness and opacity
            MouseFilter = MouseFilterEnum.Ignore;
        } else if (BoundNode.Available) {
            ButtonPressed = false;
            isLocked = false;
            Disabled = false;
            NameLabel.AddThemeColorOverride("font_color", Colors.White);
            Modulate = new Color(1f, 1f, 1f, 1f); // Keep the button fully visible and bright
            MouseFilter = MouseFilterEnum.Stop;

        } else // Not bought not available
          {
            ButtonPressed = false;
            isLocked = true;
            NameLabel.AddThemeColorOverride("font_color", Colors.Gray);
            Modulate = new Color(0.5f, 0.5f, 0.5f, 1f); // Dim the button

            // Hover allowed, disable clicks
            MouseFilter = MouseFilterEnum.Stop;
            Disabled = true;
        }
    }

    // Buy node 
    private void OnPressed() {
        float balance = GameManager.Instance.Game.Solar;
        if (GameManager.Instance.Game.stormTree.BuyNode(BoundNode, ref balance)) {
            GameManager.Instance.Solar = balance;
            UpdateVisual();
            EmitSignal(SignalName.NodePurchased, BoundNode.Name);
            purchaseSound.Play();
        } else {
            GD.Print($"[TechNodeButton] Failed to purchase node {BoundNode.Name}. Insufficient funds.");
            UpdateVisual();
        }
    }

}
