using Godot;
using System;

public partial class MenuGlobe : Node3D
{
	public float RotationSpeed = 4f;
	
	public override void _Process(double delta){
		RotateY(Mathf.DegToRad(RotationSpeed * (float)delta));
	}
}
