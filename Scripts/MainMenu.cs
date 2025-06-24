using Godot;
using System;

public partial class MainMenu : Control
{
	private void _on_start_button_pressed()
	{
		var error = GetTree().ChangeSceneToFile("res://Scenes/default.tscn");

		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to load scene: {error}");
		}
	}
}
