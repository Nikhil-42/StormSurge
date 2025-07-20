using Godot;
using System;
using System.Collections.Generic;

public partial class TechNodeButton : TextureButton
{
	[Export] public string NodeName;
	[Signal] public delegate void NodePurchasedEventHandler(string nodeName);
	[Signal] public delegate void HoveredEventHandler(string nodeName, Vector2 mousePosition);
	[Signal] public delegate void UnhoveredEventHandler();

	public List<string> parentNames = new();

	private Label NameLabel;
	public TechNode BoundNode { get; set; }
	private bool _bound = false;
	private bool isLocked = false;

	public override void _Ready()
	{
		NameLabel = GetNode<Label>("Label");
		Pressed += OnPressed;

		// Bind to the tree node
		var tree = GameManager.Instance?.Game?.stormTree;
		if (tree == null) return;

		BoundNode = tree.getNode(NodeName);
		if (BoundNode == null) return;

		_bound = true;
		NameLabel.Text = BoundNode.name;

		UpdateVisual();
		
		// Hover signals
		MouseEntered += () => EmitSignal(SignalName.Hovered, NodeName, GetGlobalMousePosition());
		MouseExited += () => EmitSignal(SignalName.Unhovered);
	}

	public void UpdateVisual()
	{
		if (BoundNode == null)
		{
			GD.PrintErr($"[TechNodeButton] {NodeName} has no BoundNode.");
			return;
		}

		ToggleMode = true;

		if (BoundNode.bought)
		{
			ButtonPressed = true;
			isLocked = true;
			NameLabel.AddThemeColorOverride("font_color", Colors.White);
			MouseFilter = MouseFilterEnum.Ignore;
		}
		else if (BoundNode.available)
		{
			ButtonPressed = false;
			isLocked = false;
			NameLabel.AddThemeColorOverride("font_color", Colors.White);
			MouseFilter = MouseFilterEnum.Stop;
		}
		else
		{
			ButtonPressed = false;
			isLocked = true;
			NameLabel.AddThemeColorOverride("font_color", Colors.Gray);
			MouseFilter = MouseFilterEnum.Ignore;
		}
	}
	
	// Buy node 
	private void OnPressed()
	{
		if (isLocked || BoundNode == null || !BoundNode.available || BoundNode.bought)
			return;

		GameManager.Instance.Game.stormTree.buyNode(BoundNode);
		UpdateVisual();
		EmitSignal(SignalName.NodePurchased, BoundNode.name);
	}

}
