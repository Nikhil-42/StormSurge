using Godot;
using System;

public partial class Map3dFeatures : Node3D
{
	public string pinPath = "res://Assets/pin.gltf";
	public Vector3 pinScale = new Vector3(0.5f, 0.5f, 0.5f);
	
	public override void _Ready() {
		GD.Print("Running map 3D features script.");
		
		var prefab = ResourceLoader.Load<PackedScene>(pinPath);
		if (prefab == null)
		{
			GD.PrintErr($"Failed to load asset at: {pinPath}");
			return;
		}

		for (int i = 0; i < 5; i++)
		{
			Node3D instance = prefab.Instantiate<Node3D>();
			AddChild(instance);

			instance.Scale = pinScale;
			Vector3 position = new Vector3(i * 1, 0, 0); // Place along X-axis
			instance.Position = position;
		}
	}
	
	public override void _Process(double delta) {
		
	}
}
