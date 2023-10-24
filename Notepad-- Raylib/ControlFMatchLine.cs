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

   // A single match
   internal class ControlFMatch {
      internal ControlFMatchLine line;
      internal int index;
      internal int indexOfLineInMatchBuffer;

      public ControlFMatch(ControlFMatchLine line, int index, int indexOfLineInMatchBuffer) {
         this.line = line;
         this.index = index;
         this.indexOfLineInMatchBuffer = indexOfLineInMatchBuffer;
      }
   }
}
