
namespace Notepad___Raylib {
   internal struct Int2 {
      public int x;
      public int y;
      
      public int Column {
         get => x;
         set => x = value;
      }

      public int Row {
         get => y;
         set => y = value;
      }

      public Int2() { }

      public Int2(int x, int y) {
         this.x = x;
         this.y = y;
      }
   }
}
