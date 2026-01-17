#version 330 core

in vec3 fragColor;
in vec3 fragNormal;
in vec3 fragPosition;
in float ambientOcclusion;
flat in int isWater;

out vec4 FragColor;

uniform vec3 sunDirection;
uniform vec3 moonDirection;
uniform vec3 viewPos;
uniform vec3 fogColor;
uniform float fogDensity;
uniform float dayNightCycle; // 0-1, 0=midnight, 0.5=noon, 1=midnight
uniform float time;

vec3 getSkyColor() {
    // Interpolate sky color based on time of day
    vec3 dayColor = vec3(0.53, 0.81, 0.92);
    vec3 sunsetColor = vec3(0.9, 0.6, 0.4);
    vec3 nightColor = vec3(0.05, 0.05, 0.15);

    float sunHeight = sin(dayNightCycle * 3.14159265359 * 2.0);

    if (sunHeight > 0.0) {
        // Daytime
        if (sunHeight > 0.8) {
            return dayColor;
        } else {
            return mix(sunsetColor, dayColor, (sunHeight - 0.0) / 0.8);
        }
    } else {
        // Nighttime
        if (sunHeight < -0.8) {
            return nightColor;
        } else {
            return mix(nightColor, sunsetColor, (sunHeight + 0.8) / 0.8);
        }
    }
}

vec3 calculateLighting(vec3 normal, vec3 baseColor) {
    // Sun lighting
    vec3 sunDir = normalize(-sunDirection);
    float sunDiffuse = max(dot(normal, sunDir), 0.0);

    // Sun intensity based on time of day
    float sunHeight = sin(dayNightCycle * 3.14159265359 * 2.0);
    float sunIntensity = max(sunHeight, 0.0);

    // Strong directional sun light for clear shadows
    vec3 sunColor = vec3(1.0, 0.95, 0.8);
    vec3 sunLight = sunColor * sunDiffuse * sunIntensity * 1.3;

    // Moon lighting (weaker)
    vec3 moonDir = normalize(-moonDirection);
    float moonDiffuse = max(dot(normal, moonDir), 0.0);
    float moonIntensity = max(-sunHeight, 0.0) * 0.25;

    vec3 moonColor = vec3(0.6, 0.7, 1.0);
    vec3 moonLight = moonColor * moonDiffuse * moonIntensity;

    // Much lower ambient light to make shadows clearly visible
    float ambientStrength = 0.12 + sunIntensity * 0.18;
    vec3 ambientColor = mix(vec3(0.02, 0.02, 0.08), vec3(0.25, 0.3, 0.35), sunIntensity);
    vec3 ambient = ambientColor * ambientStrength;

    // Combine all lighting
    vec3 result = (ambient + sunLight + moonLight) * baseColor;

    // Apply simple ambient occlusion
    result *= ambientOcclusion;

    return result;
}

void main()
{
    vec3 norm = normalize(fragNormal);
    vec3 result = calculateLighting(norm, fragColor);

    // Water specific effects
    float alpha = 1.0;
    if (isWater == 1) {
        // Increased water transparency for better underwater visibility
        alpha = 0.5;

        // Animated flow texture effect - creates flowing appearance
        float flowSpeed = 0.3;
        float flowPattern1 = sin(fragPosition.x * 3.0 - time * flowSpeed) *
                            cos(fragPosition.z * 3.0 - time * flowSpeed * 0.7);
        float flowPattern2 = sin(fragPosition.x * 2.0 + fragPosition.z * 2.0 - time * flowSpeed * 1.3);

        float flowEffect = (flowPattern1 * 0.5 + flowPattern2 * 0.3) * 0.15;

        // Water wave animation with flow
        float wave = sin(fragPosition.x * 2.0 + time * 2.0) * 0.02 +
                     cos(fragPosition.z * 2.0 + time * 1.5) * 0.02;

        // Add shimmer to water with flow effect
        vec3 viewDir = normalize(viewPos - fragPosition);
        float fresnel = pow(1.0 - max(dot(norm, viewDir), 0.0), 3.0);
        result += vec3(0.3, 0.4, 0.5) * fresnel * 0.3;

        // Apply flow effect to brightness
        result *= (1.2 + flowEffect);
    }

    // Fog calculation
    float distance = length(fragPosition - viewPos);
    vec3 skyColor = getSkyColor();
    float fogFactor = 1.0 - exp(-fogDensity * distance * distance);
    fogFactor = clamp(fogFactor, 0.0, 1.0);

    result = mix(result, skyColor, fogFactor);

    FragColor = vec4(result, alpha);
}
