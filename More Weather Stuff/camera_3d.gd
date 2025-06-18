extends Camera3D

@export var move_speed: float = 10.0
@export var mouse_sensitivity: float = 0.003

var yaw := 0.0
var pitch := 0.0

func _ready():
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
	yaw = rotation.y
	pitch = rotation.x

func _unhandled_input(event):
	if event is InputEventMouseMotion:
		yaw -= event.relative.x * mouse_sensitivity
		pitch -= event.relative.y * mouse_sensitivity
		pitch = clamp(pitch, -PI/2, PI/2)
		rotation = Vector3(pitch, yaw, 0)

	if event is InputEventKey and event.pressed and event.keycode == KEY_ESCAPE:
		Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)

func _process(delta):
	var dir = Vector3.ZERO
	if Input.is_action_pressed("move_forward"):
		dir -= global_transform.basis.z
	if Input.is_action_pressed("move_back"):
		dir += global_transform.basis.z
	if Input.is_action_pressed("move_left"):
		dir -= global_transform.basis.x
	if Input.is_action_pressed("move_right"):
		dir += global_transform.basis.x
	if Input.is_action_pressed("move_up"):
		dir += global_transform.basis.y
	if Input.is_action_pressed("move_down"):
		dir -= global_transform.basis.y

	if dir != Vector3.ZERO:
		dir = dir.normalized()
		global_translate(dir * move_speed * delta)
