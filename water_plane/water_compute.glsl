#[compute]
#version 450

// Invocations in the (x, y, z) dimension.
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

// Our textures.
layout(r32f, set = 0, binding = 0) uniform restrict readonly image2D current_image;
layout(r32f, set = 1, binding = 0) uniform restrict readonly image2D previous_image;
layout(r32f, set = 2, binding = 0) uniform restrict writeonly image2D output_image;

// Our push PushConstant.
layout(push_constant, std430) uniform Params {
	vec4 add_wave_point;
	float damp;
	float res2;
	ivec2 texture_size;
} params;

float apply_3x3(ivec2 uv, float kernel[9]) {
	// This function is not used in this shader, but it can be useful for other shaders.
	// It applies a 3x3 kernel to the pixel at uv.
	float result = 0.0;
	for (int i = -1; i <= 1; i++) {
		for (int j = -1; j <= 1; j++) {
			ivec2 offset = ivec2(i, j);
			result += imageLoad(current_image, uv + offset).r * kernel[(i + 1) * 3 + (j + 1)];
		}
	}
	return result;
}

float apply_5x5(ivec2 uv, float kernel[25]) {
	// This function is not used in this shader, but it can be useful for other shaders.
	// It applies a 5x5 kernel to the pixel at uv.
	float result = 0.0;
	for (int i = -2; i <= 2; i++) {
		for (int j = -2; j <= 2; j++) {
			ivec2 offset = ivec2(i, j);
			result += imageLoad(current_image, uv + offset).r * kernel[(i + 2) * 5 + (j + 2)];
		}
	}
	return result;
}

#define imageLoad_periodic(current_image, uv, size) imageLoad(current_image, ((uv) + (size)) % (size))

// The code we want to execute in each invocation.
void main() {
	ivec2 size = ivec2(params.texture_size.x, params.texture_size.y);
	ivec2 uv = ivec2(gl_GlobalInvocationID.xy);

	// Just in case the texture size is not divisable by 8.
	if ((uv.x > size.x) || (uv.y > size.y)) {
		return;
	}

	float current_v = imageLoad(current_image, uv).r;
	float up_v = imageLoad_periodic(current_image, uv - ivec2(0, 1), size).r;
	float down_v = imageLoad_periodic(current_image, uv + ivec2(0, 1), size).r;
	float left_v = imageLoad_periodic(current_image, uv - ivec2(1, 0), size).r;
	float right_v = imageLoad_periodic(current_image, uv + ivec2(1, 0), size).r;
	float previous_v = imageLoad(previous_image, uv).r;

	float new_v = 2.0 * current_v - previous_v + 0.25 * (up_v + down_v + left_v + right_v - 4.0 * current_v);
	new_v = new_v - (params.damp * new_v * 0.001);

	if (params.add_wave_point.z > 0.0 && uv.x == floor(params.add_wave_point.x) && uv.y == floor(params.add_wave_point.y)) {
		new_v = params.add_wave_point.z;
	}

	if (new_v < 0.0) {
		new_v = 0.0;
	}
	vec4 result = vec4(new_v, new_v, new_v, 1.0);

	imageStore(output_image, uv, result);
}
