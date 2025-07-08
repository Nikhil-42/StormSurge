#[compute]
#version 450

// Invocations in the (x, y, z) dimension.
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

// Our textures.
// velocity texture is rg32f, which is a 2D texture with 2 channels (r and g) and 32 bits per channel.
// scalar fields are packed into a rgba32f texture, which is a 2D texture with 4 channels (r, g, b, and a) and 32 bits per channel.
layout(rg32f, set = 0, binding = 0) uniform restrict readonly image2D current_wind;
layout(rgba32f, set = 0, binding = 1) uniform restrict readonly image2D current_scalar_field;
layout(rg32f, set = 1, binding = 0) uniform restrict readonly image2D previous_wind;
layout(rgba32f, set = 1, binding = 1) uniform restrict readonly image2D previous_scalar_field;
layout(rg32f, set = 2, binding = 0) uniform restrict writeonly image2D next_wind;
layout(rgba32f, set = 2, binding = 1) uniform restrict writeonly image2D next_scalar_field;

// Our push PushConstant.
layout(push_constant, std430) uniform Params {
	vec4 add_wave_point;
	float damp;
	float res2;
	ivec2 texture_size;
} params;


ivec2 normalize_uv(ivec2 uv, ivec2 size) {
	// The UV coordinates corespond to latitude and longitude, so we need to normalize them to the texture size.
	ivec2 wrap = ivec2(size.x, size.y * 2);  // size.x represents 180deg latitude, size.y represents 360deg longitude
	uv = (uv + wrap) % wrap;  // Wrap the UV coordinates to the texture size
	uv.x = uv.y >= size.y ? size.x-1 - uv.x : uv.x;
	uv.y = uv.y >= size.y ? size.y*2-1 - uv.y : uv.y;
	return uv;
}

vec2 uv_to_rad(ivec2 uv, ivec2 size) {
	// Convert UV coordinates to radians.
	float lat = ((float(uv.y)+0.5) / float(size.y)) * 3.14159265358979323846;  // 180 degrees in radians
	float lon = ((float(uv.x)+0.5) / float(size.x)) * 6.28318530717958647692;  // 360 degrees in radians
	return vec2(lat, lon);
}

// The code we want to execute in each invocation.
void main() {
	ivec2 size = ivec2(params.texture_size.x, params.texture_size.y);
	ivec2 uv = ivec2(gl_GlobalInvocationID.xy);

	// Just in case the texture size is not divisable by 8.
	if ((uv.x > size.x) || (uv.y > size.y)) {
		return;
	}
	
	float v = 1.0;

	vec2 center = imageLoad(current_wind, uv).rg;
	vec2 up = imageLoad(current_wind, normalize_uv(uv - ivec2(0, 1), size)).rg;
	vec2 down = imageLoad(current_wind, normalize_uv(uv + ivec2(0, 1), size)).rg;
	vec2 left = imageLoad(current_wind, normalize_uv(uv - ivec2(1, 0), size)).rg;
	vec2 right = imageLoad(current_wind, normalize_uv(uv + ivec2(1, 0), size)).rg;

	float du_dx = (right.x - left.x) / 2.0;
	float du_dy = (down.x - up.x) / 2.0;
	float dv_dx = (right.y - left.y) / 2.0;
	float dv_dy = (down.y - up.y) / 2.0;

	float d2u_dx2 = (right.x - center.x) - (center.x - left.x);
	float d2u_dy2 = (down.x - center.x) - (center.x - up.x);
	float d2v_dx2 = (right.y - center.y) - (center.y - left.y);
	float d2v_dy2 = (down.y - center.y) - (center.y - up.y);

	float du_dt = v * (d2u_dx2 + d2u_dy2) + (center.x * du_dx + center.y * du_dy);
	float dv_dt = v * (d2v_dx2 + d2v_dy2) + (center.x * dv_dx + center.y * dv_dy);

	float new_u = center.x + du_dt * 1.0/60.0;
	float new_v = center.y + dv_dt * 1.0/60.0;

	// Rain 
	float current_s = imageLoad(current_scalar_field, uv).r;
	float up_s = imageLoad(current_scalar_field, normalize_uv(uv - ivec2(0, 1), size)).r;
	float down_s = imageLoad(current_scalar_field, normalize_uv(uv + ivec2(0, 1), size)).r;
	float left_s = imageLoad(current_scalar_field, normalize_uv(uv - ivec2(1, 0), size)).r;
	float right_s = imageLoad(current_scalar_field, normalize_uv(uv + ivec2(1, 0), size)).r;
	float previous_s = imageLoad(previous_scalar_field, uv).r;

	float new_s = 2.0 * current_s - previous_s + 0.25 * (up_s + down_s + left_s + right_s - 4.0 * current_s);
	new_s = new_s - (params.damp * new_s * 0.001);

	if (params.add_wave_point.z > 0.0 && uv.x == floor(params.add_wave_point.x) && uv.y == floor(params.add_wave_point.y)) {
		new_s = params.add_wave_point.z;
	}

	if (new_s < 0.0) {
		new_s = 0.0;
	}

	imageStore(next_scalar_field, uv, vec4(new_s, new_s, float(uv.x) / float(size.x), float(uv.y) / float(size.y)));
	imageStore(next_wind, uv, vec4(new_u, new_v, 1.0, 1.0));
}
