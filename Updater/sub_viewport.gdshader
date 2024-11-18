shader_type canvas_item;
render_mode blend_premul_alpha,unshaded;
// render_mode blend_mul;


void fragment()
{
	    vec3 texCol = texture( TEXTURE, UV ).rgb;
	    COLOR.rgb = texCol;
	    COLOR.a = 0.0;
}
