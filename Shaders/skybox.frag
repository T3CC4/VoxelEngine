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

float hash31(vec3 p) {
    p = fract(p * 0.1031);
    p += dot(p, p.yzx + 33.33);
    return fract((p.x + p.y) * p.z);
}

vec3 renderStars(vec3 viewDir, vec3 moonDir) {
    float sunHeight = sin(dayNightCycle * 6.28318530718);
    float starIntensity = max(-sunHeight - 0.2, 0.0);

    if (starIntensity <= 0.0) return vec3(0.0);

    // ---- Moon occlusion (MATCH moon disk exactly) ----
    float moonDot = dot(viewDir, moonDir);

    // These values MUST match renderMoon()
    float moonMask = smoothstep(
        0.9996,   // fully blocked
        0.9992,   // fully visible
        moonDot
    );

    // ---- Star field ----
    vec3 dir = normalize(viewDir + moonDir * -0.02);
    vec3 p = dir * 600.0;

    vec3 cell = floor(p);
    vec3 local = fract(p);

    float rnd = hash31(cell);

    float starChance = step(0.9975, rnd);
    float starSharp = step(0.02, length(local - 0.5));

    float twinkle = sin(dayNightCycle * 20.0 + rnd * 6.2831) * 0.25 + 0.75;

    float star = starChance * starSharp * twinkle;
    star *= mix(0.5, 1.5, rnd);

    return vec3(star) * starIntensity * moonMask;
}

void main()
{
    vec3 viewDir = normalize(fragPosition);

    // Get base sky color
    vec3 skyColor = getSkyGradient(viewDir);

    // Add celestial bodies
    skyColor += renderSun(viewDir);
    skyColor += renderMoon(viewDir);

    // Stars rotate with moon
    vec3 moonDir = normalize(-moonDirection);
    skyColor += renderStars(viewDir, moonDir);

    FragColor = vec4(skyColor, 1.0);
}
