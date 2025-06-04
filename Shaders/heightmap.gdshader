shader_type spatial;
render_mode diffuse_toon;
uniform sampler2D heightmap;
uniform sampler2D normalmap;

vec3 unpack_norm(vec4 rgba) {
	vec3 n = 2.0 * rgba.xzy - 1.0;
	n.x *= -1.0;
	n.y *= 1.0;
	n.z *= -1.0;
	return n; 
}

void vertex() {
	float height = texture(heightmap, UV).x;
	NORMAL = unpack_norm(texture(normalmap, UV));
	VERTEX *= 1.0 + height / 50.0;
	//VERTEX.y += height / 5.0;
}

void fragment() {
	NORMAL = (VIEW_MATRIX * vec4(unpack_norm(texture(normalmap, UV)), 0.0)).xyz;
	// Called for every pixel the material is visible on.
	ALBEDO = texture(heightmap, UV.xy).yyy;
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
