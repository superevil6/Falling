shader_type canvas_item;

uniform vec3 tint : source_color = vec3(0.4, 0.7, 1.0);
uniform float tint_strength : hint_range(0.0, 1.0) = 0.7;

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	vec3 col = mix(tex.rgb, tint, tint_strength);
	COLOR = vec4(col, tex.a);
}
