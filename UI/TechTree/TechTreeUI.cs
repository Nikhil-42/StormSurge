using Godot;
using System.Collections.Generic;

public partial class TechTreeUI : Control {
    [Export] public TextureButton CloseButton;
    [Export] public Control techTreeContent;
    private Dictionary<string, TechNodeButton> nameToButton = new(); // Maps node names to their buttons

    private HoverPopup hoverPopup;
    [Export] private PackedScene popupScene;

    [Signal]
    public delegate void UIClickEventHandler();

    public override void _Ready() {
        // Close button
        CloseButton.Pressed += OnClosePressed;

        // Node details hover popup
        hoverPopup = (HoverPopup)popupScene.Instantiate();
        AddChild(hoverPopup);
        hoverPopup.Hide();

        // Set listeners for buttons
        foreach (Node child in techTreeContent.GetChildren()) {
            if (child is TechNodeButton btn) {
                btn.NodePurchased += OnNodePurchased;
                btn.Connect("Hovered", new Callable(this, nameof(OnNodeHovered)));
                btn.Connect("Unhovered", new Callable(this, nameof(OnNodeUnhovered)));
            }
        }

        // Get actual storm tree
        var tree = GameManager.Instance?.Game?.stormTree;
        if (tree == null) return;

        // lookup table
        nameToButton.Clear();
        foreach (Node child in techTreeContent.GetChildren()) {
            if (child is TechNodeButton btn)
                nameToButton[btn.NodeName] = btn;
        }

        UpdateAllNodeButtons();
        DrawConnectionLines();
    }

    private void OnClosePressed() {
        EmitSignal(SignalName.UIClick);
        QueueFree();
    }

    private void DrawConnectionLines() {
        var linesNode = techTreeContent.GetNodeOrNull<Control>("LinesLayer");
        if (linesNode == null) {
            GD.PrintErr("LinesLayer not found in TreeArea.");
            return;
        }

        foreach (var child in linesNode.GetChildren())
            child.QueueFree();

        foreach (var btn in nameToButton.Values) {
            foreach (string parentName in btn.BoundNode.Parents) {
                if (!nameToButton.TryGetValue(parentName, out var parentBtn))
                    continue;

                var modulate = btn.BoundNode.Blocked && parentBtn.BoundNode.Blocked
                    ? new Color(1f, 0.5f, 0.5f, 0.5f) // Dimmed red for blocked nodes
                    : new Color(1f, 1f, 1f, 1f); // Normal white for available nodes

                var line = new Line2D {
                    Width = 2,
                    DefaultColor = new Color(1, 1, 1, 1),
                    Antialiased = true,
                    Modulate = modulate, // Dim the line
                    ZIndex = 1
                };

                Vector2 from = parentBtn.Position + parentBtn.Size / 2;
                Vector2 to = btn.Position + btn.Size / 2;

                line.Points = new Vector2[] { from, to };
                linesNode.AddChild(line);
            }
        }
    }

    // NodePurchased signal received
    private void OnNodePurchased(string nodeName) {
        var tree = GameManager.Instance?.Game?.stormTree;
        if (tree == null) return;

        // Rebind to updated state and update visuals
        UpdateAllNodeButtons();
        DrawConnectionLines();
    }

    // Update all node visuals 
    public void UpdateAllNodeButtons() {
        //GD.Print("[TechTreeUi] UpdateAllNodeButtons called");

        foreach (Node child in techTreeContent.GetChildren()) {
            if (child is TechNodeButton btn) {
                //GD.Print($"[TechTreeUi] Updating {btn.Name} → Node: {btn.NodeName}");
                btn.UpdateVisual();
            }
        }
    }

    // On hover method
    private void OnNodeHovered(string nodeName, Vector2 position) {
        var tree = GameManager.Instance?.Game?.stormTree;
        if (tree == null) return;

        var node = tree.GetNode(nodeName);
        if (node == null) return;

        string desc = $"Cost: {node.Cost}";

        hoverPopup.SetInfo(node, desc);
        hoverPopup.ShowAt(position + new Vector2(24, 12));
    }

    // Exit hover
    private void OnNodeUnhovered() {
        hoverPopup.Hide();
    }

}
