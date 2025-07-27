using Godot;
using System;

public partial class StormDirectionIndicator : Node3D
{
	[Export] public Globe Globe { get; set; }
	[Export] public float MaxIndicatorLength = 2.0f;
	
	private bool _isDragging = false;
	private Vector2 _dragStartPos;
	private Vector2 _dragCurrentPos;
	private Vector3 _dragStartWorldPos;
	private Vector3 _dragCurrentWorldPos;
	private Vector2 _dragStartLatLon;
	
	private Node3D _directionIndicator;
	
	[Signal] public delegate void DirectionSetEventHandler(Vector2 startLatLon, Vector2 direction);


	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Right)
			{
				if (mouseEvent.Pressed)
				{
					StartDrag(mouseEvent.Position);
				}
				else if (_isDragging)
				{
					EndDrag();
				}
			}
		}
		else if (@event is InputEventMouseMotion motionEvent && _isDragging)
		{
			UpdateDrag(motionEvent.Position);
		}
	}

	private void StartDrag(Vector2 screenPos)
	{
		// Check if Globe is available
		if (Globe == null)
		{
			return;
		}

		_isDragging = true;
		_dragStartPos = screenPos;
		_dragCurrentPos = screenPos;
		
		// Get world position of drag start
		var camera = GetViewport().GetCamera3D();
		if (camera == null)
		{
			return;
		}

		var from = camera.ProjectRayOrigin(_dragStartPos);
		var dir = camera.ProjectRayNormal(_dragStartPos);
		var to = from + dir * 1000.0f;

		var result = Geometry3D.SegmentIntersectsSphere(from, to, Globe.Position, Globe.Radius);
		if (result != null && result.Length > 0)
		{
			_dragStartWorldPos = result[0];
			_dragStartLatLon = Globe.GetLatLon(_dragStartWorldPos);
						
			/*
			if (GameManager.Instance.PrintDebug)
			{
				GD.Print("[StormDirectionIndicator] Drag started - Right-click and drag to set storm direction");
				GD.Print($"[StormDirectionIndicator] Start position lat/lon: ({_dragStartLatLon.X:F3}, {_dragStartLatLon.Y:F3})");
			}
			*/
		}
	}

	private void UpdateDrag(Vector2 screenPos)
	{
		if (Globe == null)
		{
			return;
		}

		_dragCurrentPos = screenPos;
		
		// Update world position of current drag
		var camera = GetViewport().GetCamera3D();
		if (camera == null)
		{
			return;
		}

		var from = camera.ProjectRayOrigin(_dragCurrentPos);
		var dir = camera.ProjectRayNormal(_dragCurrentPos);
		var to = from + dir * 1000.0f;

		var result = Geometry3D.SegmentIntersectsSphere(from, to, Globe.Position, Globe.Radius);
		if (result != null && result.Length > 0)
		{
			_dragCurrentWorldPos = result[0];
			UpdateDirectionIndicator();
		}
	}

	private void EndDrag()
	{
		if (!_isDragging) return;
		
		_isDragging = false;
		
		Vector2 direction = CalculateDragDirection();
		if (direction == Vector2.Zero)
		{
			if (GameManager.Instance.PrintDebug)
			{
				GD.Print("[StormDirectionIndicator] Drag ended - No significant direction set, cancelling.");
			}
		}
		else
		{
			EmitSignal(SignalName.DirectionSet, _dragStartLatLon, direction);
			if (GameManager.Instance.PrintDebug)
			{
				GD.Print($"[StormDirectionIndicator] Drag ended - Direction set to: ({direction.X:F3}, {direction.Y:F3})");
			}
		}
		
		ClearDirectionIndicator();
	}

	private Vector2 CalculateDragDirection()
	{
		if (Globe == null || _dragStartWorldPos == Vector3.Zero || _dragCurrentWorldPos == Vector3.Zero)
			return Vector2.Zero; // Default direction if no drag

		var startLatLon = Globe.GetLatLon(_dragStartWorldPos);
		var currentLatLon = Globe.GetLatLon(_dragCurrentWorldPos);
		
		Vector2 direction = currentLatLon - startLatLon;
		
		if (GameManager.Instance.PrintDebug)
		{
			GD.Print($"[StormDirectionIndicator] Drag start lat/lon: ({startLatLon.X:F3}, {startLatLon.Y:F3})");
			GD.Print($"[StormDirectionIndicator] Drag end lat/lon: ({currentLatLon.X:F3}, {currentLatLon.Y:F3})");
			GD.Print($"[StormDirectionIndicator] Raw direction: ({direction.X:F3}, {direction.Y:F3})");
		}
		
		// Normalize direction vector (mapping lat/lon to storm direction)
		Vector2 stormDirection = new(direction.X, direction.Y);
		
		// Adjust for longitude compression at higher latitudes
		if (Mathf.Abs(stormDirection.X) > 0.001f) // Avoid division by zero
		{
			stormDirection.X /= Mathf.Cos(startLatLon.X);
		}

		// Minimum drag distance threshold (in radians)
		const float minDragDistance = 0.01f;
		if (stormDirection.Length() < minDragDistance)
		{
			if (GameManager.Instance.PrintDebug)
			{
				GD.Print("[StormDirectionIndicator] Drag too small, using default direction");
			}
			return Vector2.Zero; // Default{Testing} (should cancel drag (no storm to spawn cause too small))
		}
		
		if (GameManager.Instance.PrintDebug)
		{
			GD.Print($"[StormDirectionIndicator] Final storm direction: ({stormDirection.X:F3}, {stormDirection.Y:F3})");
		}
			
		return stormDirection.Normalized();
	}

	private void UpdateDirectionIndicator()
	{
		ClearDirectionIndicator();

		// Create visual line from start to current position
		if (_dragStartWorldPos != Vector3.Zero && _dragCurrentWorldPos != Vector3.Zero)
		{
			var indicatorParent = new Node3D();
			AddChild(indicatorParent);
			_directionIndicator = indicatorParent;

			// Create the main line
			var line = new MeshInstance3D();
			var lineMesh = new ArrayMesh();
			var arrays = new Godot.Collections.Array();
			arrays.Resize((int)Mesh.ArrayType.Max);
			
			// Make the line slightly above the surface to ensure visibility
			var startPos = _dragStartWorldPos + (_dragStartWorldPos - Globe.Position).Normalized() * 0.1f;
			var rawEndPos = _dragCurrentWorldPos + (_dragCurrentWorldPos - Globe.Position).Normalized() * 0.1f;
			
			// Clamp the line length to maximum indicator length
			var lineDirection = (rawEndPos - startPos);
			var lineLength = lineDirection.Length();
			var endPos = startPos + lineDirection.Normalized() * Mathf.Min(lineLength, MaxIndicatorLength);
			
			var vertices = new Vector3[] { startPos, endPos };
			arrays[(int)Mesh.ArrayType.Vertex] = vertices;
			
			lineMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
			line.Mesh = lineMesh;
			
			var material = CreateIndicatorMaterial();
			line.MaterialOverride = material;
			
			indicatorParent.AddChild(line);

			CreateArrowHead(indicatorParent, startPos, endPos, material);
		}
	}


	// Basic material for the direction indicator
	private StandardMaterial3D CreateIndicatorMaterial()
	{
		var material = new StandardMaterial3D();
		material.AlbedoColor = Colors.White;
		material.EmissionEnabled = true;
		material.Emission = Colors.White;
		material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
		material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
		material.NoDepthTest = true; // Always visible
		return material;
	}

	private void CreateArrowHead(Node3D parent, Vector3 startPos, Vector3 endPos, StandardMaterial3D material)
	{
		var direction = (endPos - startPos).Normalized();
		var arrowLength = 0.5f;
		var arrowWidth = 0.2f;
		
		// Calculate perpendicular vectors for arrow head
		var up = Vector3.Up;
		var right = direction.Cross(up).Normalized();
		var actualUp = right.Cross(direction).Normalized();
		
		var arrowHead = new MeshInstance3D();
		var arrowMesh = new ArrayMesh();
		var arrowArrays = new Godot.Collections.Array();
		arrowArrays.Resize((int)Mesh.ArrayType.Max);
		
		// Create simple arrow head lines
		var arrowVertices = new Vector3[]
		{
			endPos, endPos - direction * arrowLength + actualUp * arrowWidth,
			endPos, endPos - direction * arrowLength - actualUp * arrowWidth
		};
		
		arrowArrays[(int)Mesh.ArrayType.Vertex] = arrowVertices;
		arrowMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrowArrays);
		arrowHead.Mesh = arrowMesh;
		arrowHead.MaterialOverride = material;
		
		parent.AddChild(arrowHead);
	}

	private void ClearDirectionIndicator()
	{
		if (_directionIndicator != null)
		{
			_directionIndicator.QueueFree();
			_directionIndicator = null;
		}
	}

	public bool IsDragging()
	{
		return _isDragging;
	}
}
