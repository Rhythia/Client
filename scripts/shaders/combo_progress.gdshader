shader_type canvas_item;

uniform float progress : hint_range(0, 1) = 0;
uniform vec4 colour : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 background_colour : source_color = vec4(0.155, 0.155, 0.155, 1.0);
uniform int sides : hint_range(3, 32, 0.1) = 8;
uniform float thickness : hint_range(0, 50) = 3.0;
uniform float size : hint_range(0, 2) = 1.0;

void fragment() {
	vec2 points[32];
	vec2 center = vec2(0.5, 0.5);
	vec2 center_diff = UV - vec2(0.5, 0.5);
	float frag_angle = atan(-center_diff.x, center_diff.y) * (180.0 / PI) + 180.0;
	float side_angle = 360.0 / float(sides);
	
	for (int i = 0; i < sides; i++)
	{
		float angle = (360.0 / float(sides) * float(i)) * PI / 180.0;
		vec2 pos = vec2(0.0, -0.25 * size);
		vec2 rotated = vec2(cos(angle) * pos.x - sin(angle) * pos.y, sin(angle) * pos.x + cos(angle) * pos.y) + center;
		
		points[i] = rotated;
	}
	
	int a = int(frag_angle / side_angle);
	int b = a < sides - 1 ? a + 1 : 0;
	vec2 point_diff = points[a] - points[b];
	vec2 middle = (points[a] + points[b]) / 2.0;
	float dotp = dot(normalize(point_diff), UV - middle);
	
	float dist_to_line = distance(UV, middle + dotp * normalize(point_diff));
	float line_alpha = smoothstep((thickness / 256.0) + fwidth(dist_to_line), (thickness / 256.0) - fwidth(dist_to_line), dist_to_line);
	
	if (abs(dotp) <= distance(point_diff, vec2(0.0, 0.0)) / 2.0 + thickness) {
		COLOR = mix(COLOR, vec4(background_colour.rgb, 1.0), line_alpha);
	}
	
	if (frag_angle <= progress * 360.0)
	{
		COLOR = vec4(colour.rgb, COLOR.a);
	}
}