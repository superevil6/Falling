shader_type canvas_item;

uniform float darken : hint_range(0.0, 1.0) = 0.4;

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	COLOR = vec4(tex.rgb * darken, tex.a);
}
