#version 330

// Input vertex attributes (from vertex shader)
in vec2 fragTexCoord;
in vec4 fragColor;

uniform float time;
uniform sampler2D highlightedLineMask;

vec4 threshold = vec4(0.05, 0.05, 0.05, 0.05);

out vec4 finalColor;

////// https://stackoverflow.com/questions/15095909/from-rgb-to-hsv-in-opengl-glsl
//////////////////////////// WTFPL license ///////////////////////////////////////
// All components are in the range [0…1], including hue.
vec3 rgb2hsv(vec3 c)
{
      vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
      vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
      vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));

      float d = q.x - min(q.w, q.y);
      float e = 1.0e-10;
      return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

////// https://stackoverflow.com/questions/15095909/from-rgb-to-hsv-in-opengl-glsl
//////////////////////////// WTFPL license ///////////////////////////////////////
// All components are in the range [0…1], including hue.
vec3 hsv2rgb(vec3 c)
{
      vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
      vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
      return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

/// https://www.shadertoy.com/view/XljGzV
//////////// No license /////////////////
vec3 hsl2rgb( in vec3 c )
{
    vec3 rgb = clamp( abs(mod(c.x*6.0+vec3(0.0,4.0,2.0),6.0)-3.0)-1.0, 0.0, 1.0 );

    return c.z + c.y * (rgb-0.5)*(1.0-abs(2.0*c.z-1.0));
}

/// https://www.shadertoy.com/view/XljGzV
//////////// No license /////////////////
vec3 rgb2hsl( in vec3 c ){
  float h = 0.0;
	float s = 0.0;
	float l = 0.0;
	float r = c.r;
	float g = c.g;
	float b = c.b;
	float cMin = min( r, min( g, b ) );
	float cMax = max( r, max( g, b ) );

	l = ( cMax + cMin ) / 2.0;
	if ( cMax > cMin ) {
		float cDelta = cMax - cMin;
        
        //s = l < .05 ? cDelta / ( cMax + cMin ) : cDelta / ( 2.0 - ( cMax + cMin ) ); Original
		s = l < .0 ? cDelta / ( cMax + cMin ) : cDelta / ( 2.0 - ( cMax + cMin ) );
        
		if ( r == cMax ) {
			h = ( g - b ) / cDelta;
		} else if ( g == cMax ) {
			h = 2.0 + ( b - r ) / cDelta;
		} else {
			h = 4.0 + ( r - g ) / cDelta;
		}

		if ( h < 0.0) {
			h += 6.0;
		}
		h = h / 6.0;
	}
	return vec3( h, s, l );
}

void main()
{
	vec4 maskValue = texture(highlightedLineMask, fragTexCoord);

	if(!all(greaterThan(maskValue, threshold))) {
		discard;
	}

   vec3 xColor = hsv2rgb(vec3(fragTexCoord.x + sin(time), 1.0, 1.0));
   vec3 yColor = hsv2rgb(vec3(fragTexCoord.y + cos(time), 1.0, 1.0));
	
//   vec3 xColor = hsl2rgb(vec3(fragTexCoord.x, 1.0, 0.5));
//   vec3 yColor = hsl2rgb(vec3(fragTexCoord.y, 1.0, 0.5));


   finalColor = vec4((xColor + yColor) / 2.0, maskValue.a);
}
