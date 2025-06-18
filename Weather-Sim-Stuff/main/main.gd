extends Node2D

const NUM_STORMS = 1000
const SEPARATION_RADIUS = 10.0
const SEPARATION_STRENGTH = 15.0
const MAX_VEL = 100

var storm_pos = []
var storm_vel = []

var IMAGE_SIZE = int(ceil(sqrt(NUM_STORMS)))
var storm_data : Image
var storm_data_texture : ImageTexture

var weather_data : Image
var weather_texture : ImageTexture

###################################################
### --- Initialization and Storm Generation --- ###
###################################################
func _ready():
	_generate_storms()
	_initialize_data_textures()
	
	$StormParticles.amount = storm_pos.size()
	$StormParticles.process_material.set_shader_parameter("storm_data", storm_data_texture)

func _generate_storms():
	var screen_size = get_viewport_rect().size
	for i in NUM_STORMS:
		storm_pos.append(Vector2(randf() * screen_size.x, randf() * screen_size.y))
		storm_vel.append(Vector2(randf_range(-1.0, 1.0) * MAX_VEL/10, randf_range(-1.0, 1.0) * MAX_VEL/10))
	

func _initialize_data_textures():
	storm_data = Image.create(IMAGE_SIZE, IMAGE_SIZE, false, Image.FORMAT_RGBAH)
	storm_data_texture = ImageTexture.create_from_image(storm_data)
	
	var screen_size = get_viewport_rect().size
	weather_data = Image.create(int(screen_size.x), int(screen_size.y), false, Image.FORMAT_RGBAH)

	var temp_base = 0.5
	var pressure_base = 0.7

	var temp_noise = FastNoiseLite.new()
	temp_noise.seed = randi()
	temp_noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	temp_noise.frequency = 1.0 / 128.0
	temp_noise.fractal_octaves = 4
	temp_noise.fractal_lacunarity = 2.0
	temp_noise.fractal_gain = 0.7

	var pressure_noise = FastNoiseLite.new()
	pressure_noise.seed = randi()
	pressure_noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
	pressure_noise.frequency = 1.0 / 200.0
	pressure_noise.fractal_octaves = 3
	pressure_noise.fractal_lacunarity = 2.0
	pressure_noise.fractal_gain = 0.6

	for y in weather_data.get_height():
		for x in weather_data.get_width():
			var temp_noise_val = temp_noise.get_noise_2d(x, y) * 0.2
			var temp = clamp(temp_base + temp_noise_val, 0.0, 1.0)

			var pressure_noise_val = pressure_noise.get_noise_2d(x, y) * 400.0
			var pressure = pressure_base + pressure_noise_val

			var humidity = clamp(0.4  + randf() * 0.3, 0.0, 1.0)
			weather_data.set_pixel(x, y, Color(temp, pressure, humidity))
	weather_texture = ImageTexture.create_from_image(weather_data)
	


##############################
### --- Input Handling --- ###
##############################
func _input(event):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		_spawn_hurricane(event.position, 200)  

func _spawn_hurricane(center: Vector2, num_particles: int):
	var spiral_radius = 25.0
	var spiral_turns = 4.0
	var affected_radius = 30

	_hurricane_spawn_weather_change(center, affected_radius)

	var storm_particles = _create_spiral(center, num_particles, spiral_radius, spiral_turns)
	for particle in storm_particles:
		storm_pos.append(particle["pos"])
		storm_vel.append(particle["vel"])

	$StormParticles.amount = storm_pos.size()

func _hurricane_spawn_weather_change(center: Vector2, radius: float):
	var w = weather_data.get_width()
	var h = weather_data.get_height()
	for dy in range(-radius, radius + 1):
		for dx in range(-radius, radius + 1):
			var x = int(clamp(center.x + dx, 0, w - 1))
			var y = int(clamp(center.y + dy, 0, h - 1))
			var weather = weather_data.get_pixel(x, y)
			weather.r = max(weather.r, 0.8)  # Warm
			weather.b = max(weather.b, 0.8)  # Humid
			weather_data.set_pixel(x, y, weather)

func _create_spiral(center: Vector2, num_particles: int, radius: float, turns: float) -> Array:
	var particles = []
	for i in range(num_particles):
		var t = float(i) / num_particles
		var angle = t * turns * TAU
		var r = t * radius
		var pos = center + Vector2(cos(angle), sin(angle)) * r
		var tangent = Vector2(-sin(angle), cos(angle))
		var speed = lerp(30.0, 80.0, t)
		var vel = tangent * speed
		particles.append({"pos": pos, "vel": vel})
	return particles

#################################
### --- Main Process Loop --- ###
#################################
func _process(delta):
	get_window().title = "Weather Simulation - Storms: " + str(storm_pos.size()) + " | FPS: " + str(Engine.get_frames_per_second())

	var count = storm_pos.size()

	var cluster_radius = 40.0
	var clusters = _cluster(storm_pos, cluster_radius)


	for i in count:
		var pos = storm_pos[i]
		var vel = storm_vel[i]

		# --- Get Weather influence ---
		var weather_sample = _get_weather_at(pos)
		var temp = weather_sample.r
		var pressure = weather_sample.g
		var humidity = weather_sample.b

		# --- Pressure gradient movement ---
		var pressure_force = _get_pressure_gradient(pos) * -100.0
		vel += pressure_force * delta

		# --- Spiral/circular structure preservation if in a cluster ---
		# --- Currently some gpt logic to try and create a spiral effect ---
		var in_cluster = false
		for cluster in clusters:
			if i in cluster:
				in_cluster = true
				var eye = Vector2.ZERO
				for idx in cluster:
					eye += storm_pos[idx]
				eye /= cluster.size()

				# Sort cluster indices by angle for spiral order
				var cluster_angles = []
				for idx in cluster:
					var to_eye = storm_pos[idx] - eye
					cluster_angles.append([idx, atan2(to_eye.y, to_eye.x)])
				cluster_angles.sort_custom(func(a, b): return a[1] < b[1])

				# Find this particle's order in the spiral
				var spiral_index = 0
				for k in cluster_angles.size():
					if cluster_angles[k][0] == i:
						spiral_index = k
						break

				var to_eye = pos - eye
				var angle = atan2(to_eye.y, to_eye.x)
				var base_radius = 10.0
				var spiral_growth = 1.08
				var target_radius = base_radius * pow(spiral_growth, float(spiral_index))
				var target_pos = eye + Vector2(cos(angle), sin(angle)) * target_radius

				# Restoring force toward spiral
				var restoring_force = (target_pos - pos).normalized() * 30.0
				vel += restoring_force * delta

				# Swirl (tangential) velocity
				var swirl_dir = Vector2(-to_eye.y, to_eye.x).normalized()
				var swirl_strength = 40.0
				vel += swirl_dir * swirl_strength * delta

				# Calm near the eye
				if to_eye.length() < 15.0:
					vel *= 0.92

				break  # Only apply one cluster's logic

		# --- Hurricane logic in warm, humid areas if not in a cluster ---
		if not in_cluster and temp > 0.6 and humidity > 0.6:
			# Coriolis effect
			var coriolis_strength = 0.5
			var coriolis_force = Vector2(-vel.y, vel.x).normalized() * coriolis_strength
			vel += coriolis_force * delta

		_affect_weather_map(pos, vel, (in_cluster or (temp > 0.6 and humidity > 0.6)))

		vel = vel.limit_length(MAX_VEL)
		pos += vel * delta

		_screen_wrap(pos)

		storm_pos[i] = pos
		storm_vel[i] = vel

	_update_data_texture()
	queue_redraw()


func _cluster(particles: Array, cluster_radius: float) -> Array:
	var clusters = []
	var assigned = {}
	for i in particles.size():
		if i in assigned:
			continue
		var cluster = [i]
		assigned[i] = true
		for j in particles.size():
			if i == j or j in assigned:
				continue
			if particles[i].distance_to(particles[j]) < cluster_radius:
				cluster.append(j)
				assigned[j] = true
		if cluster.size() > 5:
			clusters.append(cluster)
	return clusters

func _screen_wrap(pos: Vector2) -> Vector2:
	var screen_size = get_viewport_rect().size
	if pos.x < 0:
		pos.x += screen_size.x
	elif pos.x >= screen_size.x:
		pos.x -= screen_size.x
	if pos.y < 0:
		pos.y += screen_size.y
	elif pos.y >= screen_size.y:
		pos.y -= screen_size.y
	return pos

func _update_data_texture():
	var count = storm_pos.size()
	for i in count:
		var pixel_pos = Vector2(float(i % IMAGE_SIZE), float(i) / float(IMAGE_SIZE))
		storm_data.set_pixelv(pixel_pos, Color(storm_pos[i].x, storm_pos[i].y, storm_vel[i].angle(), 0))
	storm_data_texture.update(storm_data)

func _draw():
	var spacing = 10
	var w = weather_data.get_width()
	var h = weather_data.get_height()

	# Draw temperature heatmap with full color spectrum (commented out for performance)
	#for y in range(0, h, spacing):
	#	for x in range(0, w, spacing):
	#		var temp = weather_data.get_pixel(x, y).r
	#		var hue = lerp(0.66, 0.0, temp)
	#		var color = Color.from_hsv(hue, 1.0, 1.0, 0.7)
	#		draw_rect(Rect2(Vector2(x, y), Vector2(spacing, spacing)), color, true)

	for pos in storm_pos:
		draw_circle(pos, 2, Color(1, 1, 1, 1))


#######################
### --- Helpers --- ###
#######################
func _get_weather_at(pos: Vector2) -> Color:
	var img_size = Vector2(weather_data.get_width(), weather_data.get_height())
	var clamped_x = int(clamp(pos.x, 0, img_size.x - 1))
	var clamped_y = int(clamp(pos.y, 0, img_size.y - 1))
	return weather_data.get_pixel(clamped_x, clamped_y)

func _get_pressure_gradient(pos: Vector2) -> Vector2:
	var dx = _get_weather_at(pos + Vector2(1, 0)).g - _get_weather_at(pos - Vector2(1, 0)).g
	var dy = _get_weather_at(pos + Vector2(0, 1)).g - _get_weather_at(pos - Vector2(0, 1)).g
	return Vector2(dx, dy)

func _affect_weather_map(pos: Vector2, vel: Vector2, feedback=false):
	var w = weather_data.get_width()
	var h = weather_data.get_height()
	var radius = 2
	for dy in range(-radius, radius + 1):
		for dx in range(-radius, radius + 1):
			var x = int(clamp(pos.x + dx, 0, w - 1))
			var y = int(clamp(pos.y + dy, 0, h - 1))
			var weather = weather_data.get_pixel(x, y)
			if feedback:
				weather.r = min(weather.r + 0.002, 1.0)   # Slight temp increase
				weather.g = max(weather.g - 0.004, 0.0)   # Pressure drop
				weather.b = min(weather.b + 0.004, 1.0)   # Humidity up
			else:
				weather.g = max(weather.g - 0.002, 0.0)
				weather.b = min(weather.b + 0.002, 1.0)
			weather_data.set_pixel(x, y, weather)
