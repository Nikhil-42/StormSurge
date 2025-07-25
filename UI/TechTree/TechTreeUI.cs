using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TechTreeUI : Control
{
	[Export] public TextureButton CloseButton;
	[Export] public Control techTreeContent;
	private Dictionary<string, TechNodeButton> nameToButton = new(); // Maps node names to their buttons
	
	private HoverPopup hoverPopup;
	private PackedScene popupScene = GD.Load<PackedScene>("res://UI/TechTree/HoverPopup.tscn");

	public override void _Ready()
	{
		// Close button
		CloseButton.Pressed += OnClosePressed;

		// Node details hover popup
		hoverPopup = (HoverPopup)popupScene.Instantiate();
		AddChild(hoverPopup);
		hoverPopup.Hide();

		// Set listeners for buttons
		foreach (Node child in techTreeContent.GetChildren())
		{
			if (child is TechNodeButton btn)
			{
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
		foreach (Node child in techTreeContent.GetChildren())
		{
			if (child is TechNodeButton btn)
				nameToButton[btn.NodeName] = btn;
		}
		
		// Save parents
		foreach (var parentNode in tree.locked.Concat(tree.available).Concat(tree.bought))
		{
			foreach (var child in parentNode.children)
			{
				if (nameToButton.TryGetValue(child.name, out var childBtn))
				{
					childBtn.parentNames.Add(parentNode.name);
				}
			}
		}

		UpdateAllNodeButtons();
		DrawConnectionLines();
	}
	
	private void OnClosePressed()
	{
		QueueFree(); // Remove UI from the scene
	}
	
	private void DrawConnectionLines()
	{
		var linesNode = techTreeContent.GetNodeOrNull<Control>("LinesLayer");
		if (linesNode == null)
		{
			GD.PrintErr("LinesLayer not found in TreeArea.");
			return;
		}

		foreach (var child in linesNode.GetChildren())
			child.QueueFree();

		foreach (var btn in nameToButton.Values)
		{
			foreach (string parentName in btn.parentNames)
			{
				if (!nameToButton.TryGetValue(parentName, out var parentBtn))
					continue;

				var line = new Line2D
				{
					Width = 2,
					DefaultColor = new Color(1, 1, 1, 1),
					Antialiased = true,
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
	private void OnNodePurchased(string nodeName)
	{
		GD.Print($"[TechTreeUi] Node purchased: {nodeName}");

		var tree = GameManager.Instance?.Game?.stormTree;
		if (tree == null) return;
		
		// Rebind to updated state and update visuals
		foreach (Node child in techTreeContent.GetChildren())
		{
			if (child is TechNodeButton btn)
			{	
				var node = tree.getNode(btn.NodeName);
				if (node != null)
				{
					btn.BoundNode = node;
					btn.UpdateVisual();
				}
			}
		}
	}
	
	// pdate all node visuals 
	public void UpdateAllNodeButtons()
	{
		//GD.Print("[TechTreeUi] UpdateAllNodeButtons called");

		foreach (Node child in techTreeContent.GetChildren())
		{
			if (child is TechNodeButton btn)
			{
				//GD.Print($"[TechTreeUi] Updating {btn.Name} → Node: {btn.NodeName}");
				btn.UpdateVisual();
			}
		}
	}
	
	// On hover method
	private void OnNodeHovered(string nodeName, Vector2 position)
	{
		var tree = GameManager.Instance?.Game?.stormTree;
		if (tree == null) return;

		var node = tree.getNode(nodeName);
		if (node == null) return;

		string desc = $"Cost: {node.cost}";
		var effects = new List<(string, string, int)>();

		for (int i = 0; i < node.weather.vars.Length; i++)
		{
			if (node.weather.vars[i] != 0)
				effects.Add(("Weather", node.weather.var_names[i], node.weather.vars[i]));
		}

		for (int i = 0; i < node.geo.vars.Length; i++)
		{
			if (node.geo.vars[i] != 0)
				effects.Add(("Geo", node.geo.var_names[i], node.geo.vars[i]));
		}

		if (!node.storm && node.human != null)
		{
			for (int i = 0; i < node.human.vars.Length; i++)
			{
				if (node.human.vars[i] != 0)
					effects.Add(("Human", node.human.var_names[i], node.human.vars[i]));
			}
		}

		hoverPopup.SetInfo(node.name, desc, effects);
		hoverPopup.ShowAt(position + new Vector2(24, 12));
	}
	
	// Exit hover
	private void OnNodeUnhovered()
	{
		hoverPopup.Hide();
	}

}
