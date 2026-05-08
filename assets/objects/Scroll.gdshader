shader_type canvas_item;
uniform float time_scale = 1.0;
uniform float xspeed = .1;
uniform float yspeed = .1;

void vertex() {
	UV.x += TIME * xspeed;
	UV.y += TIME * yspeed;
}
