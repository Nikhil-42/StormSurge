shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx;

uniform vec3 albedo : source_color;
uniform float metalic : hint_range(0.0, 1.0, 0.1) = 0.8;
uniform float roughness : hint_range(0.0, 1.0, 0.1) = 0.2;
uniform sampler2D scalar_texture;
uniform sampler2D wind_texture;
uniform vec2 texture_size;

varying vec2 uv_tangent;
varying vec2 uv_binormal;

void vertex() {
	vec2 pixel_size = vec2(1.0, 1.0) / texture_size;

	uv_tangent = UV + vec2(pixel_size.x, 0.0);
	uv_binormal = UV + vec2(0.0, pixel_size.y);
}

void fragment() {
	vec4 f = texture(scalar_texture, UV).rgba;
	float f1 = f.r;
	float f2 = texture(scalar_texture, uv_tangent).r;
	float f3 = texture(scalar_texture, uv_binormal).r;

	vec3 tangent = normalize(vec3(1.0, 0.0, f2 - f1));
	vec3 binormal = normalize(vec3(0.0, 1.0, f3 - f1));
	NORMAL_MAP = normalize(cross(binormal, tangent)) * 0.5 + 0.5;

	//ALBEDO = albedo.rgb;
	ALBEDO = vec3(f.b, f.a, f.g);
	METALLIC = 0.0; //metalic;
	ROUGHNESS = 1.0; //roughness;
	SPECULAR = 0.5;
}
