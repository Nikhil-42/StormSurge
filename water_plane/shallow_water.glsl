#[compute]
#version 450

// Invocations in the (x, y, z) dimension.
layout(local_size_x = 16, local_size_y = 16, local_size_z = 1) in;

// Our textures.
// velocity texture is rg32f, which is a 2D texture with 2 channels (r and g) and 32 bits per channel.
// scalar fields are packed into a rgba32f texture, which is a 2D texture with 4 channels (r, g, b, and a) and 32 bits per channel.
layout(set = 0, binding = 0) uniform sampler2D heightmap;
layout(rgba32f, set = 1, binding = 0) uniform restrict readonly image2D current_wind;
layout(rgba32f, set = 1, binding = 1) uniform restrict readonly image2D current_scalar_field;
layout(rgba32f, set = 2, binding = 0) uniform restrict writeonly image2D next_wind;
layout(rgba32f, set = 2, binding = 1) uniform restrict writeonly image2D next_scalar_field;

// Our push PushConstant.
layout(push_constant, std430) uniform Params {
	vec4 add_wave_point;
	float delta_time;
	float viscosity;
	float diffusion;
	float padding;
} params;

const float g = 9.8067; // m/s^2
const float H = 100.0; 
const float PI = 3.14159265358979323846;
const float EARTH_RADIUS = 6371000.0; // m
const float OMEGA = 7.2921159e-5; // rad/s
const float TIME_SCALE = 5.0 * 60.0; // 1 second = 1 hour

ivec2 normalize_uv(ivec2 uv, ivec2 size) {
	// The UV coordinates corespond to latitude and longitude, so we need to normalize them to the texture size.
	ivec2 wrap = ivec2(size.x, size.y * 2);  // size.x represents 180deg latitude, size.y represents 360deg longitude
	uv = (uv + wrap) % wrap;  // Wrap the UV coordinates to the texture size
	uv.x = uv.y >= size.y ? size.x-1 - uv.x : uv.x;
	uv.y = uv.y >= size.y ? size.y*2-1 - uv.y : uv.y;
	return uv;
}

vec2 uv_to_rad(ivec2 uv, ivec2 size) {
	// Convert UV coordinates to radians (based on a equirectangular projection and pixel centers).
	float lat = ((float(uv.y)+0.5) / float(size.y)) * PI;  // 180 degrees in radians
	float lon = ((float(uv.x)+0.5) / float(size.x)) * 2 * PI;  // 360 degrees in radians
	return vec2(lat, lon);
}

float calc_distance(vec2 p1, vec2 p2, float radius) {
	// Haversine Formula
	// a = sin²(Δφ/2) + cos φ1 ⋅ cos φ2 ⋅ sin²(Δλ/2)
	// c = 2 ⋅ atan2( √a, √(1−a) )
	// d = R ⋅ c
	float sindlat = sin((p1.x - p2.x) / 2.0);
	float sindlon = sin((p1.y - p2.y) / 2.0);

	float a = sindlat * sindlat + cos(p1.x) * cos(p2.x) * sindlon*sindlon;
	float c = 2.0 * atan(sqrt(a), sqrt(1.0 - a));
	return radius * c;
}

// The code we want to execute in each invocation.
void main() {
	ivec2 size = imageSize(current_wind);
	ivec2 uv = ivec2(gl_GlobalInvocationID.xy);
	vec2 latlon = uv_to_rad(uv, size);

	float coriolis_coefficient = 2 * OMEGA * sin(latlon.x);
	coriolis_coefficient *= 0.0;

	// Just in case the texture size is not divisable by 16.
	if ((uv.x > size.x) || (uv.y > size.y)) {
		return;
	}

	ivec2 up_uv = normalize_uv(uv + ivec2(0, -1), size);
	ivec2 down_uv = normalize_uv(uv + ivec2(0, 1), size);
	ivec2 left_uv = normalize_uv(uv + ivec2(-1, 0), size);
	ivec2 right_uv = normalize_uv(uv + ivec2(1, 0), size);

	vec2 up_latlon = uv_to_rad(up_uv, size);
	vec2 down_latlon = uv_to_rad(down_uv, size);
	vec2 left_latlon = uv_to_rad(left_uv, size);
	vec2 right_latlon = uv_to_rad(right_uv, size);

	// Spatial delta
	float dx = calc_distance(left_latlon, right_latlon, EARTH_RADIUS);
	float dy = calc_distance(up_latlon, down_latlon, EARTH_RADIUS);

	vec4 center_scalars = imageLoad(current_scalar_field, uv); // height at the current pixel
	vec4 up_scalars = imageLoad(current_scalar_field, up_uv);
	vec4 down_scalars = imageLoad(current_scalar_field, down_uv);
	vec4 left_scalars = imageLoad(current_scalar_field, left_uv);
	vec4 right_scalars = imageLoad(current_scalar_field, right_uv);
	
	vec2 center_wind = imageLoad(current_wind, uv).rg;
	vec2 up_wind = imageLoad(current_wind, up_uv).rg;
	vec2 down_wind = imageLoad(current_wind, down_uv).rg;
	vec2 left_wind = imageLoad(current_wind, left_uv).rg;
	vec2 right_wind = imageLoad(current_wind, right_uv).rg;

	float center_height = texture(heightmap, vec2(uv) / vec2(size)).r;
	float up_height = texture(heightmap, vec2(up_uv) / vec2(size)).r;
	float down_height = texture(heightmap, vec2(down_uv) / vec2(size)).r;
	float left_height = texture(heightmap, vec2(left_uv) / vec2(size)).r;
	float right_height = texture(heightmap, vec2(right_uv) / vec2(size)).r;

	// Mass flux
	vec2 center_mass_flux = center_scalars.r * center_wind;
	vec2 up_mass_flux = up_scalars.r * up_wind;
	vec2 down_mass_flux = down_scalars.r * down_wind;
	vec2 left_mass_flux = left_scalars.r * left_wind;
	vec2 right_mass_flux = right_scalars.r * right_wind;

	// Finite-Difference Derivative Approximation
	vec4 dsdx = (right_scalars - left_scalars) / dx;
	vec4 dsdy = (down_scalars - up_scalars) / dy;

	float dheightdx = (right_height - left_height) / dx;
	float dheightdy = (down_height - up_height) / dy;
	dheightdx *= 0.0;
	dheightdy *= 0.0;

	float dwinddx = (right_wind.x - left_wind.x) / dx;
	float dwinddy = (down_wind.y - up_wind.y) / dy;

	float dmassdx = (right_mass_flux.x - left_mass_flux.x) / dx;
	float dmassdy = (down_mass_flux.y - up_mass_flux.y) / dy;

	// Pressure gradient 
	float dpressuredx = dsdx.r + dheightdx;
	float dpressuredy = dsdy.r + dheightdy;
	vec2 dpressure = vec2(dpressuredx, dpressuredy);

	// Wind update
	float dt = params.delta_time * TIME_SCALE;

	vec2 new_wind = center_wind -
			g * dpressure * dt +
			vec2(1, -1) * coriolis_coefficient * center_wind.yx * dt;
	
	float new_pressure = center_scalars.r - (dmassdx + dmassdy) * dt;

	if (abs(uv.x - floor(params.add_wave_point.x)) < 2 && abs(uv.y - floor(params.add_wave_point.y)) < 3) {
		if (params.add_wave_point.z > 0.0) {
			new_pressure += params.add_wave_point.z * dt / 1000;
		}
	}

	imageStore(next_scalar_field, uv, vec4(new_pressure, 0.0, 0.0, 0.0));
	if (isinf(new_wind.x) || isinf(new_wind.y) || isnan(new_wind.x) || isnan(new_wind.y)) {
		new_wind = center_wind;
	}

	imageStore(next_wind, uv, vec4(new_wind.x, new_wind.y, 1.0, 1.0));
}