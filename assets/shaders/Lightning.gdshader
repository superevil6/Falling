shader_type canvas_item;

uniform float speed : hint_range(0.0, 20.0) = 8.0;
uniform float noise_scale : hint_range(0.5, 16.0) = 6.0;
uniform float flicker_rate : hint_range(0.0, 30.0) = 12.0;
uniform vec3 color_core : source_color = vec3(1.0, 1.0, 0.95);
uniform vec3 color_mid : source_color = vec3(0.7, 0.8, 1.0);
uniform vec3 color_edge : source_color = vec3(0.3, 0.2, 0.8);

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

float vnoise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x),
	           mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x),
	           u.y);
}

float fbm(vec2 p) {
	float v = 0.0;
	float amp = 0.5;
	for (int i = 0; i < 4; i++) {
		v += vnoise(p) * amp;
		p *= 2.02;
		amp *= 0.5;
	}
	return v;
}

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	vec2 p = UV * noise_scale;
	p += vec2(TIME * speed, TIME * speed * 0.7);
	float n = fbm(p);
	n = pow(n, 2.0);
	float pulse = 0.5 + 0.5 * sin(TIME * flicker_rate);
	n = mix(n, 1.0, 0.3 * pulse);
	vec3 col = mix(color_edge, color_mid, smoothstep(0.2, 0.5, n));
	col = mix(col, color_core, smoothstep(0.5, 0.85, n));
	COLOR = vec4(col, tex.a);
}
