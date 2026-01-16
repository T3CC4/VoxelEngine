#version 330 core

in vec3 fragPosition;
out vec4 FragColor;

uniform vec3 sunDirection;
uniform vec3 moonDirection;
uniform float dayNightCycle; // 0-1, 0=midnight, 0.5=noon, 1=midnight

vec3 getSkyGradient(vec3 viewDir) {
    // Sky gradient based on vertical direction
    float height = viewDir.y;

    // Time-based colors
    float sunHeight = sin(dayNightCycle * 3.14159265359 * 2.0);

    // Day colors
    vec3 dayTop = vec3(0.1, 0.3, 0.8);         // Deep blue
    vec3 dayHorizon = vec3(0.5, 0.7, 0.95);    // Light blue

    // Sunset colors
    vec3 sunsetTop = vec3(0.3, 0.1, 0.4);      // Purple
    vec3 sunsetHorizon = vec3(0.9, 0.5, 0.2);  // Orange

    // Night colors
    vec3 nightTop = vec3(0.0, 0.0, 0.05);      // Almost black
    vec3 nightHorizon = vec3(0.05, 0.05, 0.15); // Dark blue

    // Interpolate based on time of day
    vec3 topColor, horizonColor;

    if (sunHeight > 0.0) {
        // Day or sunset
        if (sunHeight > 0.5) {
            // Full day
            topColor = dayTop;
            horizonColor = dayHorizon;
        } else {
            // Transition to sunset
            float t = sunHeight / 0.5;
            topColor = mix(sunsetTop, dayTop, t);
            horizonColor = mix(sunsetHorizon, dayHorizon, t);
        }
    } else {
        // Night or dawn
        if (sunHeight < -0.5) {
            // Full night
            topColor = nightTop;
            horizonColor = nightHorizon;
        } else {
            // Transition from night to dawn
            float t = (sunHeight + 0.5) / 0.5;
            topColor = mix(nightTop, sunsetTop, t);
            horizonColor = mix(nightHorizon, sunsetHorizon, t);
        }
    }

    // Interpolate between horizon and top based on view direction
    float gradientFactor = smoothstep(-0.2, 0.8, height);
    return mix(horizonColor, topColor, gradientFactor);
}

vec3 renderSun(vec3 viewDir) {
    vec3 sunDir = normalize(-sunDirection);
    float sunDot = dot(viewDir, sunDir);

    // Sun disk
    float sunDisk = smoothstep(0.9995, 0.9998, sunDot);

    // Sun glow
    float sunGlow = pow(max(sunDot, 0.0), 10.0) * 0.5;
    sunGlow += pow(max(sunDot, 0.0), 5.0) * 0.3;

    // Sun intensity based on time of day
    float sunHeight = sin(dayNightCycle * 3.14159265359 * 2.0);
    float sunIntensity = max(sunHeight, 0.0);

    vec3 sunColor = vec3(1.0, 0.95, 0.8);
    vec3 sunGlowColor = vec3(1.0, 0.8, 0.4);

    return (sunDisk * sunColor + sunGlow * sunGlowColor) * sunIntensity;
}

vec3 renderMoon(vec3 viewDir) {
    vec3 moonDir = normalize(-moonDirection);
    float moonDot = dot(viewDir, moonDir);

    // Moon disk
    float moonDisk = smoothstep(0.9992, 0.9996, moonDot);

    // Moon glow (subtle)
    float moonGlow = pow(max(moonDot, 0.0), 8.0) * 0.2;

    // Moon intensity based on time of day (opposite of sun)
    float sunHeight = sin(dayNightCycle * 3.14159265359 * 2.0);
    float moonIntensity = max(-sunHeight, 0.0);

    vec3 moonColor = vec3(0.9, 0.9, 1.0);

    return (moonDisk + moonGlow) * moonColor * moonIntensity * 0.8;
}

vec3 renderStars(vec3 viewDir) {
    // Simple procedural stars
    float sunHeight = sin(dayNightCycle * 3.14159265359 * 2.0);
    float starIntensity = max(-sunHeight - 0.3, 0.0);

    if (starIntensity <= 0.0) {
        return vec3(0.0);
    }

    // Use view direction to create star pattern
    vec3 stars = vec3(0.0);
    float starField = 0.0;

    // Create star pattern using noise-like function
    vec3 p = normalize(viewDir) * 100.0;
    for (int i = 0; i < 3; i++) {
        vec3 q = fract(p * 0.3 + float(i) * 1.234) - 0.5;
        float d = length(q);
        float brightness = smoothstep(0.45, 0.25, d);
        starField += brightness;
        p = p * 2.0 + 0.1;
    }

    stars = vec3(starField) * starIntensity;
    return stars * 0.5;
}

void main()
{
    vec3 viewDir = normalize(fragPosition);

    // Get base sky color
    vec3 skyColor = getSkyGradient(viewDir);

    // Add celestial bodies
    skyColor += renderSun(viewDir);
    skyColor += renderMoon(viewDir);
    skyColor += renderStars(viewDir);

    FragColor = vec4(skyColor, 1.0);
}
