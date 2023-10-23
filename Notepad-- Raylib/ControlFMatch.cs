namespace Notepad___Raylib {
   internal struct ControlFMatch {
      public int lineNumber;
      public int[] matchIndices;

      public ControlFMatch(int lineNumber, int[] matchIndices) {
         this.lineNumber = lineNumber;
         this.matchIndices = matchIndices;
      }
   }
}
