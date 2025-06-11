shader_type spatial;
render_mode diffuse_toon;
uniform float vertical_scale;
uniform sampler2D heightmap;
uniform sampler2D normalmap: hint_normal;
uniform sampler2D albedo;
varying mat3 TBN;

vec3 unpack_normal(vec4 rgba) {
	vec3 n = 2.0 * rgba.xyz - 1.0;
	n.x *= -1.0;
	n.y *= 1.0;
	n.z *= -1.0;
	return n;
}

void vertex() {
	float height = texture(heightmap, UV).x;
	VERTEX *= 1.0 + vertical_scale * height;
}

void fragment() {
	// Called for every pixel the material is visible on.
	TBN = mat3(TANGENT, BINORMAL, NORMAL);
	NORMAL = TBN * unpack_normal(texture(normalmap, UV)).xyz;
	ALBEDO = texture(albedo, UV).xyz;

}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
