shader_type canvas_item;

uniform float speed : hint_range(0.0, 10.0) = 3.0;
uniform float noise_scale : hint_range(0.5, 8.0) = 3.0;
uniform vec3 color_hot : source_color = vec3(1.0, 1.0, 0.7);
uniform vec3 color_warm : source_color = vec3(1.0, 0.45, 0.0);
uniform vec3 color_cool : source_color = vec3(0.7, 0.0, 0.0);

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
	p.y -= TIME * speed;
	float n = fbm(p);
	vec3 fire = mix(color_cool, color_warm, smoothstep(0.2, 0.55, n));
	fire = mix(fire, color_hot, smoothstep(0.55, 0.85, n));
	COLOR = vec4(fire, tex.a);
}
