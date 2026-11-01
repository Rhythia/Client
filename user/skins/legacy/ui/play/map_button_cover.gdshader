shader_type canvas_item;

void fragment() {
	COLOR.a = min(0.8, UV.x * 1.5);
}
