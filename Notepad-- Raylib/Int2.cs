using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Notepad___Raylib {
   public struct Int2 {
      public int x;
      public int y;

      public int Column {
         get => x;
         set => x = value;
      }

      public int Row {
         get => y;
         set => y = value;
      }

      public Int2() { }

      public Int2(int x, int y) {
         this.x = x;
         this.y = y;
      }

      public override string ToString() {
         return $"({x}, {y})";
      }

      public override bool Equals([NotNullWhen(true)] object? obj) {
         Int2 int2;

         try {
            int2 = (Int2)obj;
         }
         catch {
            return false;
         }
         
         return this == int2;
      }

      public override int GetHashCode() {
         return x ^ y;
      }

      public static explicit operator Int2(Vector2 vector2) {
         return new Int2((int)vector2.X, (int)vector2.Y);
      }

      public static bool operator ==(Int2 left, Int2 right) {
         return left.x == right.x && left.y == right.y;
      }

      public static bool operator !=(Int2 left, Int2 right) {
         return !(left == right);
      }
   }
}
