shader_type canvas_item;

uniform float speed : hint_range(0.0, 5.0) = 0.6;
uniform float noise_scale : hint_range(0.5, 16.0) = 5.0;
uniform vec3 color_core : source_color = vec3(0.95, 0.98, 1.0);
uniform vec3 color_mid : source_color = vec3(0.5, 0.8, 1.0);
uniform vec3 color_edge : source_color = vec3(0.15, 0.4, 0.8);

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
	p += vec2(TIME * speed * 0.6, TIME * speed * 0.3);
	float n = fbm(p);
	n = smoothstep(0.3, 0.75, n);
	vec3 ice = mix(color_edge, color_mid, smoothstep(0.0, 0.5, n));
	ice = mix(ice, color_core, smoothstep(0.55, 0.9, n));
	COLOR = vec4(ice, tex.a);
}
