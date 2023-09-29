using Raylib_CsLo;
using System;
using System.Numerics;

namespace Notepad___Raylib {
   internal class ScrollBar {
      Color backgroundColor = new Color(46, 46, 46, 255);
      Color scrollBarColor = new Color(77, 77, 77, 255);
      Color scrollBarHoverColor = new Color(130, 130, 130, 255);
      Color scrollBarHighlightColor = new Color(153, 153, 153, 255);
      /// <summary>
      /// in pixels
      /// </summary>
      int margin = 4;
      int scrollBarWidth = 17;
      Rectangle scrollBarRect;
      bool isScrollBarHeld = false;

      public bool IsScrollBarHeld => isScrollBarHeld;

      public void RenderHorizontal(int length) {
         Raylib.DrawRectangle(0, Raylib.GetScreenHeight() - scrollBarWidth, length, scrollBarWidth, backgroundColor);

         if (isScrollBarHeld) {
            Raylib.DrawRectangleRec(scrollBarRect, scrollBarHighlightColor);
         } else if (IsMouseOverScrollBar()) {
            Raylib.DrawRectangleRec(scrollBarRect, scrollBarHoverColor);
         } else {
            Raylib.DrawRectangleRec(scrollBarRect, scrollBarColor);
         }
      }

      public void RenderVertical(int length) {
         throw new NotImplementedException();
      }

      public bool IsMouseOverScrollBar() {
         return Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), scrollBarRect);
      }

      public bool IsScrollBarPressed() {
         return IsMouseOverScrollBar() && Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT);
      }

      /// <summary>
      /// Call this every frame.
      /// </summary>
      public void UpdateHorizontal(ref Camera2D camera, int distanceToRightMostChar, int length) {
         int scrollBarLength = (int)Math.Min((float)Raylib.GetScreenWidth() / distanceToRightMostChar * length, length);
         int scrollBarRenderStartPos = (int)Math.Min(camera.target.X / distanceToRightMostChar * length, Raylib.GetScreenToWorld2D(new Vector2(Raylib.GetScreenWidth(), 0), camera).X);
         scrollBarRect = new Rectangle(scrollBarRenderStartPos, Raylib.GetScreenHeight() - scrollBarWidth + margin, scrollBarLength, scrollBarWidth - 2 * margin);

         if (IsScrollBarPressed()) {
            isScrollBarHeld = true;
         }

         if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT)) {
            isScrollBarHeld = false;
         }

         if (isScrollBarHeld) {
            Vector2 mouseDelta = Raylib.GetMouseDelta();

            scrollBarRect.x += (int)mouseDelta.X;

            camera.target.X = GetCameraOffsetFromScrollBarHorizontal();
         }

         LateUpdateHorizontal(ref camera);
      }

      void LateUpdateHorizontal(ref Camera2D camera) {
         if(scrollBarRect.x < 0) {
            scrollBarRect.x = 0;
         }

         if(scrollBarRect.x + scrollBarRect.width > Raylib.GetScreenWidth()) {
            scrollBarRect.x = Raylib.GetScreenWidth() - scrollBarRect.width;
         }
      }

      float GetCameraOffsetFromScrollBarHorizontal() {
         int horizontalSpan = Program.FindDistanceToRightMostChar(Program.lines, Program.font);
         return scrollBarRect.x / Raylib.GetScreenWidth() * horizontalSpan;
      }
   }
}
