shader_type spatial;
uniform sampler2D transparency;

void vertex() {
	VERTEX *= 1.0 + texture(transparency, UV).x / 100.0;
}

void fragment() {
	// Called for every pixel the material is visible on.
	ALPHA = texture(transparency, UV).x;
	ALBEDO = vec3(1.0, 1.0, 1.0); // (1.0, 1.0, 1.0);
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
