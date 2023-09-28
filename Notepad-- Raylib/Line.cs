using Raylib_CsLo;

namespace Notepad___Raylib {
   internal class Line {
      // TODO maybe List<char> instead of string? cause it is easier to edit
      string value;
      /// <summary>
      /// Zero-based.
      /// </summary>
      uint lineNumber;
      public static int height = 20;

      public string Value => value;
      public uint LineNumber => lineNumber;

      public Line() {
         value = "";
      }

      public Line(string value, uint lineNumber) {
         this.value = value;
         this.lineNumber = lineNumber;
      }

      public void InsertTextAt(int index, string c) {
         value = value.Insert(index, c);
      }

      public void RemoveTextAt(int index, int count, Direction direction = Direction.Left) {
         switch (direction) {
            case Direction.Left:
               value = value.Remove(index - count, count);
               break;
            case Direction.Right:
               value = value.Remove(index, count);
               break;
         }
      }

      public static int MeasureTextHeight(Font font, string text, int fontSize) {
         return (int)Raylib.MeasureTextEx(font, text, fontSize, 0).Y;
      }
   }
}
