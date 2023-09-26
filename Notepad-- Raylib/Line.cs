using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notepad___Raylib {
   internal class Line {
      string value;
      /// <summary>
      /// Zero-based.
      /// </summary>
      uint lineNumber;
      public static int height = 20;

      public string Value => value;

      public Line() {
         value = "";
      }

      public Line(string value, uint lineNumber) {
         this.value = value;
         this.lineNumber = lineNumber;
      }
   }
}
