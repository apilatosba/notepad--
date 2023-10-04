#version 330

//// Input vertex attributes (from vertex shader)
//in vec2 fragTexCoord;
//in vec4 fragColor;

//// Input uniform values
//uniform sampler2D texture0;
//uniform vec4 colDiffuse;

// Output fragment color
vec4 finalColor;

void main() {
    finalColor = vec4(1,0,1,1);
    gl_FragColor = finalColor;
}