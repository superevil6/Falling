shader_type canvas_item;

uniform float speed : hint_range(0.0, 5.0) = 1.2;
uniform float noise_scale : hint_range(0.5, 16.0) = 4.0;
uniform float bubble_rate : hint_range(0.0, 10.0) = 3.0;
uniform vec3 color_core : source_color = vec3(0.85, 0.45, 1.0);
uniform vec3 color_mid : source_color = vec3(0.45, 0.1, 0.65);
uniform vec3 color_edge : source_color = vec3(0.25, 0.5, 0.2);

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
	p.y -= TIME * speed * 0.4;
	p.x += sin(TIME * 0.8 + UV.y * 6.0) * 0.3;
	float n = fbm(p);
	float bubble = 0.5 + 0.5 * sin(TIME * bubble_rate + n * 6.0);
	n = mix(n, n * 1.2, bubble * 0.4);
	vec3 col = mix(color_edge, color_mid, smoothstep(0.25, 0.55, n));
	col = mix(col, color_core, smoothstep(0.55, 0.85, n));
	COLOR = vec4(col, tex.a);
}
