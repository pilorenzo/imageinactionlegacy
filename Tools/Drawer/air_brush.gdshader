shader_type canvas_item;

uniform float blur_radius : hint_range(0.0, 1.0) = 0.5;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

void vertex() {
    // Called for every vertex the material is visible on.
}

void fragment() {
    float distance_to_edge = 0.5 - length(UV - vec2(0.5));
    float blur = smoothstep(0.0, blur_radius, distance_to_edge);
    COLOR.a *= blur;
}

void light() {
	// Called for every pixel for every light affecting the CanvasItem.
}
