extends Node3D

@export var num_particles: int = 750
@export var spiral_radius: float = 25.0
@export var spiral_turns: float = 18.0
@export var eye_radius: float = 4.0
@export var particle_scene: PackedScene
@export var rotation_speed: float = 2.0  # Radians per second

@export var earth_radius: float = 10.0
@export var globe_center: Vector3 = Vector3.ZERO

@export var degrees_per_unit_x: float = 0.1  # Longitude offset per local X unit
@export var degrees_per_unit_y: float = 0.1  # Latitude offset per local Z unit

@export var move_speed_deg_per_sec: float = 2.0
@export var move_direction: Vector2 = Vector2(1, 0)

var storm_origin_lat: float = 0.0
var storm_origin_lon: float = 0.0

var particles = []
var initial_offsets = []
var rotation_angle := 0.0
var storm_active := false

func _ready():
	move_direction = move_direction.normalized()
	# Do NOT spawn storm here

var storms = []

func _input(event):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		var camera = get_viewport().get_camera_3d()
		if camera == null:
			return
		var mouse_pos = event.position
		var from = camera.project_ray_origin(mouse_pos)
		var dir = camera.project_ray_normal(mouse_pos)
		var to = from + dir * 1000.0

		var result = Geometry3D.segment_intersects_sphere(from, to, globe_center, earth_radius)
		if result != null and result.size() > 0:
			var hit_point = result[0]
			var latlon = vector3_to_latlon(hit_point - globe_center)
			spawn_storm_at(latlon.x, latlon.y)

func spawn_storm_at(lat: float, lon: float):
	var storm_particles = []
	var initial_offsets = []
	var rotation_angle = 0.0

	var spawned := 0
	var attempts := 0
	while spawned < num_particles and attempts < num_particles * 3:
		var t = float(attempts) / float(num_particles)
		var angle = t * spiral_turns * TAU
		var radius = t * spiral_radius
		var x = cos(angle) * radius
		var z = sin(angle) * radius
		attempts += 1

		if sqrt(x * x + z * z) < eye_radius:
			continue

		var instance = particle_scene.instantiate()
		instance.visible = true
		instance.position = Vector3.ZERO
		add_child(instance)

		storm_particles.append(instance)
		initial_offsets.append(Vector2(x, z))
		spawned += 1

	storms.append({
		"origin_lat": lat,
		"origin_lon": lon,
		"rotation_angle": rotation_angle,
		"particles": storm_particles,
		"offsets": initial_offsets
	})

func _process(delta):
	get_window().title = "Weather Simulation - Storms: " + str(particles.size()) + " | FPS: " + str(Engine.get_frames_per_second())

	for storm in storms:
		storm.rotation_angle += rotation_speed * delta

		# Move storm's origin across the globe
		var move_delta = move_direction * move_speed_deg_per_sec * delta
		storm.origin_lon += move_delta.x
		storm.origin_lat += move_delta.y
		storm.origin_lat = clamp(storm.origin_lat, -89.9, 89.9)

		for i in storm.particles.size():
			var offset = storm.offsets[i].rotated(storm.rotation_angle)
			var lon = storm.origin_lon + offset.x * degrees_per_unit_x
			var lat = storm.origin_lat + offset.y * degrees_per_unit_y
			var globe_pos = latlon_to_vector3(lat, lon, earth_radius)
			storm.particles[i].global_position = globe_center + globe_pos

func latlon_to_vector3(lat_deg: float, lon_deg: float, radius: float) -> Vector3:
	var lat = deg_to_rad(lat_deg)
	var lon = deg_to_rad(lon_deg)
	var x = radius * cos(lat) * sin(lon)
	var y = radius * sin(lat)
	var z = radius * cos(lat) * cos(lon)
	return Vector3(x, y, z)

func vector3_to_latlon(pos: Vector3) -> Vector2:
	var r = pos.length()
	if r == 0:
		return Vector2(0, 0)
	var lat = asin(pos.y / r)
	var lon = atan2(pos.x, pos.z)
	return Vector2(rad_to_deg(lat), rad_to_deg(lon))
