namespace Notepad___Raylib {
   internal class UndoItem {
      public Line line;
      public int lineNumber;
      public UndeReason reason;
      public Int2 cursorPosition;

      public UndoItem(Line line, int lineNumber, UndeReason reason, Int2 cursorPosition) {
         this.line = line;
         this.lineNumber = lineNumber;
         this.reason = reason;
         this.cursorPosition = cursorPosition;
      }
   }
}
