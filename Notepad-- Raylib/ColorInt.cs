using System;

namespace Notepad___Raylib {
   internal struct ColorInt {
      internal int r;
      internal int g;
      internal int b;
      internal int a;

      public ColorInt(int r, int g, int b, int a) {
         this.r = r;
         this.g = g;
         this.b = b;
         this.a = a;
      }

      public void Clamp0To255() {
         r = Math.Clamp(r, 0, 255);
         g = Math.Clamp(g, 0, 255);
         b = Math.Clamp(b, 0, 255);
         a = Math.Clamp(a, 0, 255);
      }

      public static ColorInt operator +(ColorInt a, ColorInt b) {
         return new ColorInt(a.r + b.r,
                             a.g + b.g,
                             a.b + b.b,
                             a.a + b.a);
      }

      public static ColorInt operator -(ColorInt left, ColorInt right) {
         return new ColorInt(left.r - right.r,
                             left.g - right.g,
                             left.b - right.b,
                             left.a - right.a);
      }

      public static explicit operator Raylib_CsLo.Color(ColorInt a) {
         a.Clamp0To255();
         return new Raylib_CsLo.Color((byte)a.r,
                                      (byte)a.g,
                                      (byte)a.b,
                                      (byte)a.a);
      }

      public static implicit operator ColorInt(Raylib_CsLo.Color a) {
         return new ColorInt(a.r, a.g, a.b, a.a);
      }
   }
}

