shader_type canvas_item;

uniform vec3 tint : source_color = vec3(0.65, 0.35, 1.0);
uniform float pulse_rate : hint_range(0.0, 30.0) = 12.0;
uniform float glitch_rate : hint_range(0.0, 30.0) = 18.0;

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	float pulse = 0.35 + 0.45 * sin(TIME * pulse_rate);
	float glitch = step(0.55, fract(UV.y * 24.0 + TIME * glitch_rate));
	vec3 col = mix(tex.rgb, tint, 0.6);
	col *= 0.7 + 0.4 * glitch;
	COLOR = vec4(col, tex.a * pulse);
}
