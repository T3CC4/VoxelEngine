#version 330 core

in vec3 fragColor;
in vec3 fragNormal;
in vec3 fragPosition;
in float ambientOcclusion;

out vec4 FragColor;

uniform vec3 lightDir;
uniform vec3 viewPos;
uniform vec3 fogColor;
uniform float fogDensity;

void main()
{
    // Normalize the normal vector
    vec3 norm = normalize(fragNormal);
    vec3 lightDirection = normalize(-lightDir);

    // Ambient lighting
    float ambient = 0.4;

    // Diffuse lighting (Trove-style, simple and bright)
    float diff = max(dot(norm, lightDirection), 0.0);
    diff = diff * 0.6 + 0.3; // Brighten everything, Trove style

    // Combine lighting
    float lighting = ambient + diff;

    // Apply ambient occlusion
    lighting *= (0.7 + ambientOcclusion * 0.3);

    vec3 result = fragColor * lighting;

    // Fog calculation
    float distance = length(fragPosition - viewPos);
    float fogFactor = 1.0 - exp(-fogDensity * distance * distance);
    fogFactor = clamp(fogFactor, 0.0, 1.0);

    result = mix(result, fogColor, fogFactor);

    FragColor = vec4(result, 1.0);
}
