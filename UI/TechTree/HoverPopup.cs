using Godot;
using System;
using System.Collections.Generic;

public partial class HoverPopup : Panel
{
	[Export] public Label TitleLabel;
	[Export] public RichTextLabel DescriptionLabel;
	[Export] public VBoxContainer EffectsContainer;

	public override void _Ready()
	{
		Hide(); // Start hidden
	}

	public void SetInfo<T>(TechNode<T> node, string description) where T : IVars<T>
	{
		TitleLabel.Text = node.Name;
		DescriptionLabel.Text = description;

		// Clear old effect labels
		foreach (Node child in EffectsContainer.GetChildren())
		{
			child.QueueFree();
		}

		// Add one label per effect
		foreach (var (name, value) in node.Vars.ToJson())
		{
			var label = new Label();
			label.Text = $"[{node.Category}] {node.Name}: {value}";
			
			// font size override
			label.AddThemeFontSizeOverride("font_size", 10);
			
			EffectsContainer.AddChild(label);
		}
	}

	public void ShowAt(Vector2 position)
	{
		GlobalPosition = position;
		Show();
	}

}
