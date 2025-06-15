using Godot;
using System;

public partial class CameraRig : Node3D
{
	[Export] public Node3D Globe;
	[Export] public float RotationSpeed = 0.01f;	// "Mouse Sensitivity" for rotation
	[Export] public float Radius = 18.0f;			// Controls zoomm level
	[Export] public float MinPitch = -80f;			// Min/Max avoids flipping upside down
	[Export] public float MaxPitch = 80f;

	private Camera3D _camera;
	private float _yaw = 0f;
	private float _pitch = 0f;
	private bool _dragging = false;

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("Camera3D");
		UpdateCameraPosition();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left)
				_dragging = mouseButton.Pressed;
		}

		if (_dragging && @event is InputEventMouseMotion motion)
		{
			Vector2 delta = motion.Relative;

			_yaw -= delta.X * RotationSpeed;
			_pitch -= delta.Y * RotationSpeed;
			_pitch = Mathf.Clamp(_pitch, Mathf.DegToRad(MinPitch), Mathf.DegToRad(MaxPitch));

			UpdateCameraPosition();
		}
	}

	private void UpdateCameraPosition()
	{
		if (Globe == null || _camera == null)
			return;

		Vector3 center = Globe.GlobalPosition;

		float x = Radius * Mathf.Cos(_pitch) * Mathf.Sin(_yaw);
		float y = Radius * Mathf.Sin(_pitch);
		float z = Radius * Mathf.Cos(_pitch) * Mathf.Cos(_yaw);

		_camera.GlobalPosition = center + new Vector3(x, y, z);
		_camera.LookAt(center, Vector3.Up);
	}
}
