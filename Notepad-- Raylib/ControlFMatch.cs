namespace Notepad___Raylib {
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
