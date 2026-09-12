shader_type canvas_item;

uniform float offset : hint_range(0.0, 50.0) = 16.0;
uniform float strength : hint_range(1.0, 10.0) = 2.6;

varying vec4 v_modulate;

void vertex() {
    v_modulate = COLOR;
}

void fragment() {
    vec2 texel = TEXTURE_PIXEL_SIZE;

    vec4 sum = texture(TEXTURE, UV) * 4.0;
    float totalWeight = 4.0;

    int iterations = int(strength);

    for (int i = 1; i <= 10; i++) {
        if (i > iterations) {
            break;
        }

        float o = offset * float(i);

        sum += texture(TEXTURE, UV + vec2( o,  o) * texel);
        sum += texture(TEXTURE, UV + vec2(-o,  o) * texel);
        sum += texture(TEXTURE, UV + vec2( o, -o) * texel);
        sum += texture(TEXTURE, UV + vec2(-o, -o) * texel);

        totalWeight += 4.0;
    }

    COLOR = (sum / totalWeight) * v_modulate;
}