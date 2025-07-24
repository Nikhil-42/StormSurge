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

	public void SetInfo(string title, string description, List<(string category, string name, int value)> effects)
	{
		TitleLabel.Text = title;
		DescriptionLabel.Text = description;

		// Clear old effect labels
		foreach (Node child in EffectsContainer.GetChildren())
		{
			child.QueueFree();
		}

		// Add one label per effect
		foreach (var (category, name, value) in effects)
		{
			var label = new Label();
			label.Text = $"[{category}] {name}: {(value >= 0 ? "+" : "")}{value}";
			
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
