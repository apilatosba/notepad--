namespace Notepad___Raylib {
   internal struct ControlFMatchLine {
      public int[] matchIndices;
      /// <summary>
      /// The line number in the Program.lines List<Line> buffer.
      /// </summary>
      public int lineNumber;

      public ControlFMatchLine(int lineNumber, int[] matchIndices) {
         this.matchIndices = matchIndices;
         this.lineNumber = lineNumber;
      }
   }
}
