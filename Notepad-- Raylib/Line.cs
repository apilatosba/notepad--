using Raylib_CsLo;
using System;
using System.Text.RegularExpressions;

namespace Notepad___Raylib {
   internal class Line {
      string value;
      public static int Height {
         get {
            return GetLineHeight(Program.font, Program.config.fontSize);
         }
      }

      public string Value {
         get => value;
         set => this.value = value;
      }

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

      [Obsolete("Use the other overload that takes a Cursor as a parameter if you want to push it to the undo stack")]
      public void InsertTextAt(int index, string c) {
         value = value.Insert(index, c);
      }

      public void InsertTextAt(int index, string c, Cursor cursor) {
         Program.undoHistory.Push(new System.Collections.Generic.List<UndoItem>() {
            new UndoItem(new Line(this), Program.lines.IndexOf(this), cursor.position, UndoAction.Replace)
         });

         value = value.Insert(index, c);
      }

      public void RemoveTextAt(int index, int count, Cursor cursor, Direction direction = Direction.Left) {
         int yPosition = Program.lines.IndexOf(this);

         switch (direction) {
            case Direction.Left:
               new Selection(new Int2(index, yPosition), new Int2(index - count, yPosition)).Delete(Program.lines, cursor);
               break;
            case Direction.Right:
               new Selection(new Int2(index, yPosition), new Int2(index + count, yPosition)).Delete(Program.lines, cursor);
               break;
         }
      }

      [Obsolete("Use the other overload that takes a Cursor as a parameter if you want to push it to the undo stack")]
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

      /// <summary>
      /// 
      /// </summary>
      /// <param name="regex"></param>
      /// <returns>Empty array if no match is found</returns>
      public int[] Find(Regex regex) {
         regex = new Regex(regex.ToString(), RegexOptions.IgnoreCase);

         MatchCollection matches = regex.Matches(value);
         int[] indices = new int[matches.Count];

         for (int i = 0; i < matches.Count; i++) {
            indices[i] = matches[i].Index;
         }

         return indices;
      }
   }
}
