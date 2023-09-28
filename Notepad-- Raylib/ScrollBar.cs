using Raylib_CsLo;
using System;
using System.Numerics;

namespace Notepad___Raylib {
   internal class ScrollBar {
      Color backgroundColor = new Color(46, 46, 46, 255);
      Color scrollBarColor = new Color(77, 77, 77, 255);
      Color scrollBarHighlightColor = new Color(153, 153, 153, 255);
      /// <summary>
      /// in pixels
      /// </summary>
      int margin = 4;
      int scrollBarWidth = 17;

      public void RenderHorizontal(int length, in Camera2D camera, int distanceToRightMostChar) {
         Raylib.DrawRectangle(0, Raylib.GetScreenHeight() - scrollBarWidth, length, scrollBarWidth, backgroundColor);
         
         int scrollBarLength = (int)Math.Min((float)Raylib.GetScreenWidth() / distanceToRightMostChar * length, length);
         int scrollBarRenderStartPos = (int)Math.Min(camera.target.X / distanceToRightMostChar * length, Raylib.GetScreenToWorld2D(new Vector2(Raylib.GetScreenWidth(),0),camera).X);
         Raylib.DrawRectangle(scrollBarRenderStartPos, Raylib.GetScreenHeight() - scrollBarWidth + margin, scrollBarLength, scrollBarWidth - 2 * margin, scrollBarColor);
      }

      public void RenderVertical(int length) {
         throw new System.NotImplementedException();
      }
   }
}
