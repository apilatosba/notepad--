namespace Notepad___Raylib {
   // A single match
   internal class ControlFMatch {
      internal ControlFMatchLine line;
      /// <summary>
      /// Index in the matchIndices array of the ControlFMatchLine.
      /// </summary>
      internal int index;
      internal int indexOfLineInMatchBuffer;
      /// <summary>
      /// Overall index. If there are 5 matches in total and this is the 3rd match, this will be 2.
      /// This can be calculated by the values i have already but i think storing it is better.
      /// </summary>
      internal int overallIndex;

      public ControlFMatch(ControlFMatchLine line, int index, int indexOfLineInMatchBuffer, int overallIndex) {
         this.line = line;
         this.index = index;
         this.indexOfLineInMatchBuffer = indexOfLineInMatchBuffer;
         this.overallIndex = overallIndex;
      }
   }
}
