#version 330

// Input vertex attributes (from vertex shader)
in vec2 fragTexCoord;
in vec4 fragColor;

uniform float transparency;

out vec4 finalColor;

void main()
{
    finalColor = vec4(fragColor.xyz, transparency);
}