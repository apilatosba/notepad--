using Raylib_CsLo;

namespace Notepad___Raylib {
   internal class Line {
      string value;
      public static int Height {
         get {
            return GetLineHeight(Program.font, Program.config.fontSize);
         }
      }

      public string Value => value;

      public Line() {
         value = "";
      }

      public Line(string value) {
         this.value = value;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="line">a copy</param>
      public Line(Line line) {
         this.value = line.value;
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

      public static int GetLineHeight(Font font, int fontSize) {
         return MeasureTextHeight(font, "A", fontSize);
      }
   }
}
