namespace Notepad___Raylib {
   internal class UndoItem {
      public Line line;
      public int lineNumber;
      // Possible useless field.
      // todo get rid of it if so
      public Int2 cursorPosition;
      /// <summary>
      /// This action is what should happen when you pop
      /// </summary>
      public UndoAction action;

      public UndoItem(Line line, int lineNumber, Int2 cursorPosition, UndoAction action) {
         this.line = line;
         this.lineNumber = lineNumber;
         this.cursorPosition = cursorPosition;
         this.action = action;
      }
   }
}
