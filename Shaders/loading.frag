#version 330 core

uniform vec2 uResolution; // screen or target texture resolution
uniform float uTime;      // time in seconds

in vec2 fragCoord;        // fragment coordinates
out vec4 FragColor;

#define PI 3.1415926

// SDF for a box
float sdfBox(vec2 p, vec2 size) {
    return length(max(abs(p) - size, 0.0));
}

void main() {
    // Convert fragCoord to normalized coordinates centered at 0
    vec2 uv = (fragCoord.xy - 0.5 * uResolution.xy) / uResolution.y;

    vec2 p = uv;
    vec3 col = vec3(0.0);

    float angle = atan(p.y, p.x) / PI;

    // Animation time (looping 0-2)
    float ttime = mod(uTime * 2.2, 2.0);

    // Polar coordinates
    float r = length(p);
    p = vec2(r, angle);

    // Offset
    p = (1.0 * p) - vec2(0.0, 0.122);
    p = p.yx;

    float boxHeight = 0.014;
    float boxYOffset = 0.092;
    float d;

    // Draw right side segments
    float t = 1.0;
    float t1 = 1.0;
    for(int i=1; i<=5; i++) {
        d = smoothstep(0.004, 0.01, sdfBox(p - vec2(t, boxYOffset), vec2(0.104, boxHeight)));
        if(ttime > t1 && ttime < t1 + 0.3)
            col = mix(col, vec3(0.7), 1.0 - d);
        else
            col = mix(col, vec3(0.1), 1.0 - d);
        t -= 0.25;
        t1 += 0.25;
    }

    // Draw left side segments
    t = 0.0;
    for(int i=1; i<=5; i++) {
        d = smoothstep(0.004, 0.01, sdfBox(p - vec2(-t, boxYOffset), vec2(0.104, boxHeight)));
        if(ttime > t && ttime < t + 0.3)
            col = mix(col, vec3(0.7), 1.0 - d);
        else
            col = mix(col, vec3(0.1), 1.0 - d);
        t += 0.25;
    }

    // Gamma correction
    col = pow(col, vec3(1.0 / 2.2));

    FragColor = vec4(col, 1.0);
}
