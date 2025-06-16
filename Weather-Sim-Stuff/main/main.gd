extends Node2D

var NUM_STORMS = 5000
var storm_pos = []
var storm_vel = []

var max_vel = 100.0

var IMAGE_SIZE = int(ceil(sqrt(NUM_STORMS)))
var storm_data : Image
var storm_data_texture : ImageTexture

var weather_data : Image
var weather_texture : ImageTexture

# called when the node enters the scene tree for the first time.
func _ready():
	_generate_storms()
	_initialize_data_textures()
	
	$StormParticles.amount = NUM_STORMS
	$StormParticles.process_material.set_shader_parameter("storm_data", storm_data_texture)

func _generate_storms():
	for i in NUM_STORMS:
		storm_pos.append(Vector2(randf() * get_viewport_rect().size.x, randf() * get_viewport_rect().size.y))
		storm_vel.append(Vector2(randf_range(-1.0, 1.0) * max_vel/10, randf_range(-1.0, 1.0) * max_vel/10))
	

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
	
func _update_data_texture():
	for i in NUM_STORMS:
		var pixel_pos = Vector2(float(i % IMAGE_SIZE), float(i) / float(IMAGE_SIZE))
		storm_data.set_pixelv(pixel_pos, Color(storm_pos[i].x, storm_pos[i].y, storm_vel[i].angle(), 0))
		
	storm_data_texture.update(storm_data)
	
	# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	for i in NUM_STORMS:
		var pos = storm_pos[i]
		var weather_sample = _get_weather_at(pos)

		var temp = weather_sample.r
		var pressure = weather_sample.g
		var humidity = weather_sample.b

		var pressure_force = _get_pressure_gradient(pos) * -50.0
		var humidity_boost = humidity * 5.0

		storm_vel[i] += pressure_force * delta
		storm_vel[i] = storm_vel[i].limit_length(max_vel + humidity_boost)

		storm_pos[i] += storm_vel[i] * delta

		var screen_size = get_viewport_rect().size
		if storm_pos[i].x < 0:
			storm_pos[i].x += screen_size.x
		elif storm_pos[i].x >= screen_size.x:
			storm_pos[i].x -= screen_size.x
		if storm_pos[i].y < 0:
			storm_pos[i].y += screen_size.y
		elif storm_pos[i].y >= screen_size.y:
			storm_pos[i].y -= screen_size.y

	_update_data_texture()



# GPT Draw function to visualize the wind direction
func _draw():
	var spacing = 10# Larger spacing to reduce overlap

	var w = weather_data.get_width()
	var h = weather_data.get_height()

	# Draw temperature heatmap (optional, can comment out if too busy)
	for y in range(0, h, spacing):
		for x in range(0, w, spacing):
			var temp = weather_data.get_pixel(x, y).r
			var color = Color(temp, 0, 1.0 - temp, 0.3)
			draw_rect(Rect2(Vector2(x, y), Vector2(spacing, spacing)), color, true)
"""
	# Draw curved wind arrows
	for y in range(int(spacing / 2), h, spacing):
		for x in range(int(spacing / 2), w, spacing):
			var points = []
			var pos = Vector2(x, y)
			points.append(pos)
			var cur_pos = pos
			for i in range(arrow_steps):
				var grad = _get_pressure_gradient(cur_pos)
				if grad.length() < 0.001:
					break
				var dir = grad.normalized()
				cur_pos += dir * arrow_step_len
				points.append(cur_pos)
			# Only draw if arrow is long enough
			if points.size() > 1:
				# Draw the curved arrow
				for i in range(points.size() - 1):
					draw_line(points[i], points[i + 1], Color.WHITE, 1.5)
				# Draw arrowhead at the end
				_draw_arrowhead(points[points.size() - 2], points[points.size() - 1], Color.WHITE)

func _draw_arrowhead(start: Vector2, end: Vector2, color: Color):
	var dir = (end - start).normalized()
	var perp = Vector2(-dir.y, dir.x)
	var size = 6.0
	var p1 = end
	var p2 = end - dir * size + perp * size * 0.5
	var p3 = end - dir * size - perp * size * 0.5
	draw_polygon(PackedVector2Array([p1, p2, p3]), [color])

"""


# Helpers to grab weather data and pressure gradients
func _get_weather_at(pos: Vector2) -> Color:
	var img_size = Vector2(weather_data.get_width(), weather_data.get_height())
	var clamped_x = clamp(pos.x, 0, img_size.x - 1)
	var clamped_y = clamp(pos.y, 0, img_size.y - 1)
	return weather_data.get_pixel(clamped_x, clamped_y)

func _get_pressure_gradient(pos: Vector2) -> Vector2:
	var dx = _get_weather_at(pos + Vector2(1, 0)).g - _get_weather_at(pos - Vector2(1, 0)).g
	var dy = _get_weather_at(pos + Vector2(0, 1)).g - _get_weather_at(pos - Vector2(0, 1)).g
	return Vector2(dx, dy)

	
