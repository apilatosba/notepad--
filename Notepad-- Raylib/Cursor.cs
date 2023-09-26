
using Raylib_CsLo;
using System.Collections.Generic;

namespace Notepad___Raylib {
   internal class Cursor {
      /// <summary>
      /// position.x is how many characters from the left of the line the cursor is.<br />
      /// position.y is line number.
      /// </summary>
      public Int2 position;
      Color color = new Color(150, 150, 150, 255);

      public void Render(List<Line> lines, int fontSize, int leftPadding) {
         Line line = lines[position.y];
         string textBeforeCursor;
         try {
            textBeforeCursor = line.Value.Substring(0, position.x);
         } catch(System.ArgumentOutOfRangeException) {
            if(lines.Count <= position.y + 1) {
               position.y++;
               position.x = 0;
               textBeforeCursor = "";
            } else { // I have reached the end of the file.
               textBeforeCursor = line.Value;
            }
         }
         int distance = Raylib.MeasureText(textBeforeCursor, fontSize);
         Raylib.DrawRectangle(leftPadding + distance, (int)position.y * Line.height, 1, Line.height, color);
      }
   }
}
