using Raylib_CsLo;

namespace Notepad___Raylib {
   internal class Line {
      string value;
      /// <summary>
      /// Zero-based.
      /// </summary>
      uint lineNumber;
      public static int height = 20;

      public string Value => value;
      public uint LineNumber {
         get { return lineNumber; }
         set { lineNumber = value; }
      }

      public Line() {
         value = "";
      }

      public Line(string value, uint lineNumber) {
         this.value = value;
         this.lineNumber = lineNumber;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="line">a copy</param>
      public Line(Line line) {
         this.value = line.value;
         this.lineNumber = line.lineNumber;
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
