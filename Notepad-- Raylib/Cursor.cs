using Raylib_CsLo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Notepad___Raylib {
   internal class Cursor {
      /// <summary>
      /// position.x is how many characters from the left of the line the cursor is.<br />
      /// position.y is line number.
      /// </summary>
      public Int2 position;
      public int exXPosition;
      //Color color = new Color(150, 150, 150, 255); It is in config now.

      // TODO render cursor considering spacingBetweenLines
      public void Render(in List<Line> lines, int fontSize, int leftPadding, Font font, int spacingBetweenLines) {
         int distance = GetWorldSpacePosition(lines, fontSize, leftPadding, font).x;

         Raylib.DrawRectangle(distance, position.y * Line.Height, 1, Line.Height, Program.config.cursorColor);
      }

      public void Render(in List<Line> lines, int fontSize, int leftPadding, Font font, int spacingBetweenLines, Color color) {
         int distance = GetWorldSpacePosition(lines, fontSize, leftPadding, font).x;

         Raylib.DrawRectangle(distance, position.y * Line.Height, 1, Line.Height, color);
      }

      public void HandleArrowKeysNavigation(in List<Line> lines, ref Camera2D camera, int fontSize, int leftPadding, Font font, bool isControlKeyDown) {
         if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT)) {
            if (IsCursorAtEndOfFile(lines)) return;

            if (IsCursorAtEndOfLine(lines)) {
               position.x = 0;
               position.y++;
            } else {
               if (isControlKeyDown) {
                  position.x += CalculateHowManyCharactersToJump(lines, Direction.Right);
               } else {
                  position.x++;
               }
            }

            MakeSureCursorIsVisibleToCamera(lines, ref camera, fontSize, leftPadding, font);
            exXPosition = position.x;

         } else if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT)) {
            if (IsCursorAtBeginningOfFile()) return;

            if (IsCursorAtBeginningOfLine()) {
               position.x = lines[--position.y].Value.Length;
            } else {
               if (isControlKeyDown) {
                  position.x -= CalculateHowManyCharactersToJump(lines, Direction.Left);
               } else {
                  position.x--;
               }
            }

            MakeSureCursorIsVisibleToCamera(lines, ref camera, fontSize, leftPadding, font);
            exXPosition = position.x;

         } else if (Raylib.IsKeyDown(KeyboardKey.KEY_UP)) {
            if (isControlKeyDown) return;
            if (IsCursorAtFirstLine()) return;

            position.y--;
            position.x = Math.Min(exXPosition, lines[position.y].Value.Length); //Math.Min(position.x, lines[position.y].Value.Length);

            MakeSureCursorIsVisibleToCamera(lines, ref camera, fontSize, leftPadding, font);

         } else if (Raylib.IsKeyDown(KeyboardKey.KEY_DOWN)) {
            if (isControlKeyDown) return;
            if (IsCursorAtLastLine(lines)) return;

            position.y++;
            position.x = Math.Min(exXPosition, lines[position.y].Value.Length); //Math.Min(position.x, lines[position.y].Value.Length);

            MakeSureCursorIsVisibleToCamera(lines, ref camera, fontSize, leftPadding, font);
         }
      }

      public void MakeSureCursorIsVisibleToCamera(in List<Line> lines, ref Camera2D camera, int fontSize, int leftPadding, Font font) {
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
         int topEdgeWorldSpacePositionY = (int)Raylib.GetScreenToWorld2D(Vector2.Zero, camera).Y + Program.YMargin;
         int bottomEdgeWorldSpacePositionY = (int)Raylib.GetScreenToWorld2D(new Vector2(0, Raylib.GetScreenHeight()), camera).Y;
         bottomEdgeWorldSpacePositionY -= Line.Height;

         int a = cursorWorldSpacePosition.y - topEdgeWorldSpacePositionY; // Explanation: ./CursorScreenExplanation.png
         int b = cursorWorldSpacePosition.y - bottomEdgeWorldSpacePositionY; // Explanation: ./CursorScreenExplanation.png

         if (Math.Abs(a) + Math.Abs(b) <= Raylib.GetScreenHeight() - Program.YMargin) return;

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
         pos.y = position.y * Line.Height + Program.YMargin;

         return pos;
      }

      public Int2 CalculatePositionFromWorldSpaceCoordinates(in List<Line> lines, int fontSize, int leftPadding, Font font, Int2 worldSpaceCoordinates) {
         Int2 pos = new Int2();

         pos.y = (worldSpaceCoordinates.y - Program.YMargin) / Line.Height;

         if(pos.y > lines.Count - 1) pos.y = lines.Count - 1;

         Line line = lines[pos.y];
         string text = line.Value;
         int t = worldSpaceCoordinates.x - leftPadding;
         int errorTolerance = (int)Raylib.MeasureTextEx(font, ".", fontSize, 0).X / 2;

         pos.x = BinarySearch(0, line.Value.Length);

         return pos;

         int BinarySearch(int left, int right) {
            int m = (left + right) / 2;


            int r = (int)Raylib.MeasureTextEx(font, text.Substring(0, m), fontSize, 0).X;

            //Console.WriteLine($"L:{left}, R:{right}, M:{m}, R:{r}, T:{t}");

            if (Math.Abs(r - t) <= errorTolerance) return m;

            if (left >= right) return m;

            if (r < t) {
               return BinarySearch(m + 1, right);
            } else if (r > t) {
               return BinarySearch(left, m - 1);
            } else {
               return m;
            }
            Debug.Assert(false);
            return -1;
         }
      }

      public int CalculateHowManyCharactersToJump(in List<Line> lines, Direction direction) {
         switch (direction) {
            case Direction.Right: {
                  Line currentLine = lines[position.y];

                  try {
                     int j = 1;
                     for (int i = 1; currentLine.Value[position.x + i] != ' '; i++) {
                        j++;
                     }
                     return j;
                  }
                  catch (IndexOutOfRangeException) {
                     return currentLine.Value.Length - position.x;
                  }

                  break;
               }
            case Direction.Left: {
                  Line currentLine = lines[position.y];

                  try {
                     int j = 1;
                     for (int i = -1; currentLine.Value[position.x + i] != ' '; i--) {
                        j++;
                     }
                     return j;
                  }
                  catch (IndexOutOfRangeException) {
                     return position.x;
                  }

                  break;
               }
            default:
               Debug.Assert(false);
               return -1;
         }
      }

      public Int2 CalculatePositionFromWorldSpaceCoordinates(in List<Line> lines, int fontSize, int leftPadding, Font font, Vector2 worldSpaceCoordinates) {
         return CalculatePositionFromWorldSpaceCoordinates(lines, fontSize, leftPadding, font, (Int2)worldSpaceCoordinates);
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
