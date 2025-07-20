shader_type spatial;
render_mode diffuse_toon;
uniform float vertical_scale;
uniform sampler2D heightmap;
uniform sampler2D normalmap: hint_normal;
uniform sampler2D regionmap;
uniform sampler2D colormap;

uniform uint selected_region = 0u;
uniform uint hovered_region = 0u;
uniform float water_level = 0.0;

varying mat3 TBN;
varying float height;

vec3 unpack_normal(vec4 rgba) {
	vec3 n = 2.0 * rgba.xyz - 1.0;
	n.x *= -1.0;
	n.y *= 1.0;
	n.z *= -1.0;
	return n;
}

void vertex() {
	height = texture(heightmap, UV).x;
	VERTEX *= 1.0 + vertical_scale * (height >= water_level ? height : water_level);
}

void fragment() {
	// Called for every pixel the material is visible on.
	TBN = mat3(TANGENT, BINORMAL, NORMAL);
	vec3 albedo = texture(colormap, UV).rgb;
	NORMAL = height >= water_level ? TBN * unpack_normal(texture(normalmap, UV)).xyz : NORMAL;

	float mix = max(0.0, min(1.0, (water_level - height) * 20.0));
	uint region_id = uint(texelFetch(regionmap, ivec2(UV * vec2(10800.0, 5400.0)), 0).x * 256.0 + 0.5);
	ALBEDO = (1.0 - mix) * albedo * (region_id > 0u && region_id == selected_region ? 2.5f : 1.0f) +  mix * vec3(0.0784313725490196, 0.25882352941176473, 0.34901960784313724);
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
