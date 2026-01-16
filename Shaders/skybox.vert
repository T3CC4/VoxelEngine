#version 330 core

layout(location = 0) in vec3 aPosition;

out vec3 fragPosition;

uniform mat4 projection;
uniform mat4 view;

void main()
{
    fragPosition = aPosition;

    // Remove translation from view matrix
    mat4 viewNoTranslation = mat4(mat3(view));

    vec4 pos = projection * viewNoTranslation * vec4(aPosition, 1.0);

    // Set z to w so that after perspective division, z will always be 1.0 (far plane)
    gl_Position = pos.xyww;
}
