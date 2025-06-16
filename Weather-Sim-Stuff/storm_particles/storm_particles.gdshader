shader_type particles;

uniform sampler2D storm_data;

uniform float scale = 0.5;

	// Called every frame on existing particles (according to the Fixed FPS property).
void process() {

	// Pos_image_size is the size of the texture in pixels.
	ivec2 pos_image_size = textureSize(storm_data, 0);

	// Texel position is the position in the texture where the particle's data is stored.
	ivec2 texel_pos = ivec2(int(mod(float(INDEX), float(pos_image_size.x))),
							int(float(INDEX) / float(pos_image_size.x)));

	// Fetch the storm position and rotation from the texture.
	vec4 storm_pos_rot = texelFetch(storm_data, texel_pos, 0);

	// scale_rot_mat is a 2D rotation matrix that also scales the particle.
	mat2 scale_rot_mat = mat2(vec2(cos(storm_pos_rot.b), sin(storm_pos_rot.b)),
							  vec2(-sin(storm_pos_rot.b), cos(storm_pos_rot.b)));

	scale_rot_mat *= scale;

	TRANSFORM[0].xy = scale_rot_mat[0]; // first column is the Scale X * rotation
	TRANSFORM[1].xy = scale_rot_mat[1]; // second column is the Scale Y * rotation
	TRANSFORM[3].xy = vec2(storm_pos_rot.r, storm_pos_rot.g); // position of the particle in world space

	/*

	Scale X * Rotation | Scale Y * Rotation | 0 | Translate X 
	Scale X * Rotation | Scale Y * Rotation | 0 | Translate Y
	0                  | 0                  | 1 | 0
	0                  | 0                  | 0 | 1

	*/
}
