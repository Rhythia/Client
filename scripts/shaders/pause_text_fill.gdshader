shader_type canvas_item;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 empty_color : source_color = vec4(0.50, 0.50, 0.50, 1.0);
uniform vec4 fill_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	float filled = step(UV.x, progress);
	vec4 mixed = mix(empty_color, fill_color, filled);
	COLOR = vec4(mixed.rgb, tex.a * COLOR.a * mixed.a);
}
