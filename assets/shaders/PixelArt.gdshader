shader_type canvas_item;

uniform float pixel_size : hint_range(1.0, 32.0) = 4.0;
uniform int color_steps : hint_range(2, 256) = 8;
uniform float hue_shift : hint_range(-1.0, 1.0) = 0.0;

vec3 rgb2hsv(vec3 c) {
	vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
	vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));
	float d = q.x - min(q.w, q.y);
	float e = 1.0e-10;
	return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

vec3 hsv2rgb(vec3 c) {
	vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
	return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void fragment() {
	vec2 tex_size = vec2(textureSize(TEXTURE, 0));
	vec2 snapped_uv = floor(UV * tex_size / pixel_size) * pixel_size / tex_size;
	vec4 color = texture(TEXTURE, snapped_uv);
	if (hue_shift != 0.0) {
		vec3 hsv = rgb2hsv(color.rgb);
		hsv.x = fract(hsv.x + hue_shift);
		color.rgb = hsv2rgb(hsv);
	}
	color.rgb = floor(color.rgb * float(color_steps)) / float(color_steps);
	COLOR = color;
}
