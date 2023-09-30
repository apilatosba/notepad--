using Raylib_CsLo;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Notepad___Raylib {
   internal class Cursor {
      /// <summary>
      /// position.x is how many characters from the left of the line the cursor is.<br />
      /// position.y is line number.
      /// </summary>
      public Int2 position;
      Color color = new Color(150, 150, 150, 255);

      // TODO render cursor considering spacingBetweenLines
      public void Render(in List<Line> lines, int fontSize, int leftPadding, Font font, int spacingBetweenLines) {
         int distance = GetWorldSpacePosition(lines, fontSize, leftPadding, font).x;

         Raylib.DrawRectangle(distance, position.y * Line.Height, 1, Line.Height, color);
      }

      // TODO add modifiers
      public void HandleArrowKeysNavigation(in List<Line> lines, ref Camera2D camera, int fontSize, int leftPadding, Font font) {
         if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT)) {
            if (IsCursorAtEndOfFile(lines)) return;

            if (IsCursorAtEndOfLine(lines)) {
               position.x = 0;
               position.y++;
            } else {
               position.x++;
            }

            MakeSureCursorIsVisible(lines, ref camera, fontSize, leftPadding, font);

         } else if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT)) {
            if (IsCursorAtBeginningOfFile()) return;

            if (IsCursorAtBeginningOfLine()) {
               position.x = lines[--position.y].Value.Length;
            } else {
               position.x--;
            }
            
            MakeSureCursorIsVisible(lines, ref camera, fontSize, leftPadding, font);

         } else if (Raylib.IsKeyDown(KeyboardKey.KEY_UP)) {
            if (IsCursorAtFirstLine()) return;

            position.y--;
            position.x = Math.Min(position.x, lines[position.y].Value.Length);

            MakeSureCursorIsVisible(lines, ref camera, fontSize, leftPadding, font);

         } else if (Raylib.IsKeyDown(KeyboardKey.KEY_DOWN)) {
            if (IsCursorAtLastLine(lines)) return;

            position.y++;
            position.x = Math.Min(position.x, lines[position.y].Value.Length);

            MakeSureCursorIsVisible(lines, ref camera, fontSize, leftPadding, font);

         }
      }

      void MakeSureCursorIsVisible(in List<Line> lines, ref Camera2D camera, int fontSize, int leftPadding, Font font) {
         Int2 cursorWorldSpacePosition = GetWorldSpacePosition(lines, fontSize, leftPadding, font);

         MakeSureCursorIsVisibleVertical(lines, ref camera, fontSize, leftPadding, font, cursorWorldSpacePosition);
         MakeSureCursorIsVisibleHorizontal(lines, ref camera, fontSize, leftPadding, font, cursorWorldSpacePosition);
      }

      void MakeSureCursorIsVisibleHorizontal(in List<Line> lines, ref Camera2D camera, int fontSize, int leftPadding, Font font, Int2 cursorWorldSpacePosition) {
         int leftEdgeWorldSpacePositionX = (int)Raylib.GetScreenToWorld2D(Vector2.Zero, camera).X;
         int rightEdgeWorldSpacePositionX = (int)Raylib.GetScreenToWorld2D(new Vector2(Raylib.GetScreenWidth(), 0), camera).X;

         int a = cursorWorldSpacePosition.x - rightEdgeWorldSpacePositionX; // Explanation: ./CursorScreenExplanation.png
         int b = cursorWorldSpacePosition.x - leftEdgeWorldSpacePositionX; // Explanation: ./CursorScreenExplanation.png

         if (Math.Abs(a) + Math.Abs(b) <= Raylib.GetScreenWidth()) return;

         // Offset is applied so we can see the cursor otherwise it would be at the edge of the screen and not visible.
         int offset = (int)Raylib.MeasureTextEx(font, "A", fontSize, 0).X - 1;

         if (Math.Abs(a) < Math.Abs(b)) {
            camera.target.X += a + offset;
         } else if (Math.Abs(b) < Math.Abs(a)) {
            camera.target.X += b - offset;
         }
      }

      void MakeSureCursorIsVisibleVertical(in List<Line> lines, ref Camera2D camera, int fontSize, int leftPadding, Font font, Int2 cursorWorldSpacePosition) {
         int topEdgeWorldSpacePositionY = (int)Raylib.GetScreenToWorld2D(Vector2.Zero, camera).Y;
         int bottomEdgeWorldSpacePositionY = (int)Raylib.GetScreenToWorld2D(new Vector2(0, Raylib.GetScreenHeight()), camera).Y;
         bottomEdgeWorldSpacePositionY -= Line.Height;

         int a = cursorWorldSpacePosition.y - topEdgeWorldSpacePositionY; // Explanation: ./CursorScreenExplanation.png
         int b = cursorWorldSpacePosition.y - bottomEdgeWorldSpacePositionY; // Explanation: ./CursorScreenExplanation.png

         if (Math.Abs(a) + Math.Abs(b) <= Raylib.GetScreenHeight()) return;

         if (Math.Abs(a) < Math.Abs(b)) {
            camera.target.Y += a;
         } else if (Math.Abs(b) < Math.Abs(a)) {
            camera.target.Y += b;
         }
      }

      /// <summary>
      /// In pixels. Top left corner of the cursor.
      /// </summary>
      /// <param name="lines"></param>
      /// <param name="fontSize"></param>
      /// <param name="leftPadding"></param>
      /// <param name="font"></param>
      /// <returns></returns>
      public Int2 GetWorldSpacePosition(in List<Line> lines, int fontSize, int leftPadding, Font font) {
         Int2 pos = new Int2();

         Line line = lines[position.y];

         string textBeforeCursor = line.Value.Substring(0, position.x);

         pos.x = (int)Raylib.MeasureTextEx(font, textBeforeCursor, fontSize, 0).X + leftPadding;
         pos.y = position.y * Line.Height;

         return pos;
      }

      public bool IsCursorAtEndOfLine(in List<Line> lines) {
         return position.x == lines[position.y].Value.Length;
      }

      public bool IsCursorAtBeginningOfLine() {
         return position.x == 0;
      }

      public bool IsCursorAtBeginningOfFile() {
         return position.y == 0 && IsCursorAtBeginningOfLine();
      }

      public bool IsCursorAtEndOfFile(in List<Line> lines) {
         return (lines.Count - 1 == position.y) && IsCursorAtEndOfLine(lines);
      }

      public bool IsCursorAtFirstLine() {
         return position.y == 0;
      }

      public bool IsCursorAtLastLine(in List<Line> lines) {
         return position.y == lines.Count - 1;
      }
   }
}
