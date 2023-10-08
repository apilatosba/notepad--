#version 330

// Input vertex attributes (from vertex shader)
in vec2 fragTexCoord;
in vec4 fragColor;

uniform sampler2D textMask;
uniform vec2 resolution;
uniform float strength;

out vec4 finalColor;

void main()
{
   float distanceToClosestText = 9999;
   int a = 27; // better if this is odd
   vec4 threshold = vec4(0.2, 0.2, 0.2, 0.2);

   for(int i = -a / 2; i < a / 2; i++) {
      for(int j = -a / 2; j < a / 2; j++) {
         vec2 currentPixel = vec2(gl_FragCoord.x + i, gl_FragCoord.y + j);
         vec4 col = texture(textMask, currentPixel / resolution);
         if(all(greaterThan(col, threshold))) {
            distanceToClosestText = min(distanceToClosestText, distance(currentPixel, gl_FragCoord.xy));
         }
      }
   }

   // clip() ?. wp glsl
   if(distanceToClosestText > a) {
      discard;
   }

   float normalizedDistance = clamp(distanceToClosestText / a * sqrt(2), 0, 1);
   vec4 bloomColor;
   vec4 bottom = vec4(1, 0, 0, 0.7);
   vec4 mid = vec4(0, 1, 0, 0.7);
   vec4 top = vec4(0, 0, 1, 0.7);

   if(fragTexCoord.y < 0.5) {
      bloomColor = mix(bottom, mid, fragTexCoord.y * 2);
   } else {
      bloomColor = mix(mid, top, (fragTexCoord.y - 0.5) * 2);
   }

   finalColor = bloomColor * (1 - smoothstep(0, 1, normalizedDistance)) * strength;

//   finalColor = texture(textMask, gl_FragCoord.xy / resolution);
//   finalColor = texture(textMask, fragTexCoord);
}