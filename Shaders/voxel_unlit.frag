#version 330 core

in vec3 fragColor;
in vec3 fragNormal;
flat in int isWater;

out vec4 FragColor;

void main()
{
    // Simple unlit rendering - just output the base color
    vec3 result = fragColor;

    // Add very subtle normal-based shading for depth perception
    vec3 norm = normalize(fragNormal);
    float normalShade = abs(norm.y) * 0.2 + abs(norm.x) * 0.1 + abs(norm.z) * 0.1;
    result *= (0.8 + normalShade);

    // Water transparency
    float alpha = 1.0;
    if (isWater == 1) {
        alpha = 0.6;
    }

    FragColor = vec4(result, alpha);
}
