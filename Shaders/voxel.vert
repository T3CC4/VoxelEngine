#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;
layout(location = 2) in vec3 aNormal;
layout(location = 3) in float aAmbientOcclusion;

out vec3 fragColor;
out vec3 fragNormal;
out vec3 fragPosition;
out float ambientOcclusion;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    fragPosition = vec3(model * vec4(aPosition, 1.0));
    fragNormal = mat3(transpose(inverse(model))) * aNormal;
    fragColor = aColor;
    ambientOcclusion = aAmbientOcclusion;

    gl_Position = projection * view * model * vec4(aPosition, 1.0);
}
