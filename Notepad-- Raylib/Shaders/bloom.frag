//#version 330

// Input vertex attributes (from vertex shader)
in vec2 fragTexCoord;
in vec4 fragColor;

uniform sampler2D textMask;

out vec4 finalColor;

void main()
{
//   finalColor = vec4(fragTexCoord,0,1);
   finalColor = texture(textMask, fragTexCoord);
//   finalColor = fragColor;
}