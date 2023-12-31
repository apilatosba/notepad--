using Raylib_CsLo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;

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

      [Obsolete("Use the other this misses some key presses if they are too quick")]
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

         }
         if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT)) {
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

         }
         if (Raylib.IsKeyDown(KeyboardKey.KEY_UP)) {
            if (isControlKeyDown) return;
            if (IsCursorAtFirstLine()) return;

            position.y--;
            position.x = Math.Min(exXPosition, lines[position.y].Value.Length); //Math.Min(position.x, lines[position.y].Value.Length);

            MakeSureCursorIsVisibleToCamera(lines, ref camera, fontSize, leftPadding, font);

         }
         if (Raylib.IsKeyDown(KeyboardKey.KEY_DOWN)) {
            if (isControlKeyDown) return;
            if (IsCursorAtLastLine(lines)) return;

            position.y++;
            position.x = Math.Min(exXPosition, lines[position.y].Value.Length); //Math.Min(position.x, lines[position.y].Value.Length);

            MakeSureCursorIsVisibleToCamera(lines, ref camera, fontSize, leftPadding, font);
         }
      }

      public void HandleArrowKeysNavigation(in List<Line> lines, ref Camera2D camera, int fontSize, int leftPadding, Font font, bool isControlKeyDown, KeyboardKey specialKey) {
         switch (specialKey) {
            case KeyboardKey.KEY_RIGHT:
               if (IsCursorAtEndOfFile(lines)) return;

               if (IsCursorAtEndOfLine(lines)) {
                  position.x = 0;
                  position.y++;
               } else {
                  if (isControlKeyDown) {
                     position.x = CalculateNextJumpLocationX(lines, Direction.Right);
                  } else {
                     position.x++;
                  }
               }

               MakeSureCursorIsVisibleToCamera(lines, ref camera, fontSize, leftPadding, font);
               exXPosition = position.x;

               break;
            case KeyboardKey.KEY_LEFT:
               if (IsCursorAtBeginningOfFile()) return;

               if (IsCursorAtBeginningOfLine()) {
                  position.x = lines[--position.y].Value.Length;
               } else {
                  if (isControlKeyDown) {
                     position.x = CalculateNextJumpLocationX(lines, Direction.Left);
                  } else {
                     position.x--;
                  }
               }

               MakeSureCursorIsVisibleToCamera(lines, ref camera, fontSize, leftPadding, font);
               exXPosition = position.x;

               break;
            case KeyboardKey.KEY_UP:
               if (IsCursorAtFirstLine()) return;

               if (isControlKeyDown) {
                  position.y = ComputeNextJumpLocation(lines, position.y, Direction.Up);
               } else {
                  position.y--;
               }

               position.x = Math.Min(exXPosition, lines[position.y].Value.Length); //Math.Min(position.x, lines[position.y].Value.Length);

               MakeSureCursorIsVisibleToCamera(lines, ref camera, fontSize, leftPadding, font);

               break;
            case KeyboardKey.KEY_DOWN:
               if (IsCursorAtLastLine(lines)) return;

               if (isControlKeyDown) {
                  position.y = ComputeNextJumpLocation(lines, position.y, Direction.Down);
               } else {
                  position.y++;
               }

               position.x = Math.Min(exXPosition, lines[position.y].Value.Length); //Math.Min(position.x, lines[position.y].Value.Length);

               MakeSureCursorIsVisibleToCamera(lines, ref camera, fontSize, leftPadding, font);

               break;
         }
      }

      public CameraMoveDirection MakeSureCursorIsVisibleToCamera(in List<Line> lines, ref Camera2D camera, int fontSize, int leftPadding, Font font) {
         Int2 cursorWorldSpacePosition = GetWorldSpacePosition(lines, fontSize, leftPadding, font);

         bool verticalMove = MakeSureCursorIsVisibleVertical(lines, ref camera, fontSize, leftPadding, font, cursorWorldSpacePosition);
         bool horizontalMove = MakeSureCursorIsVisibleHorizontal(lines, ref camera, fontSize, leftPadding, font, cursorWorldSpacePosition);

         return (verticalMove ? CameraMoveDirection.Down : 0) | (horizontalMove ? CameraMoveDirection.Right : 0);
      }

      /// <returns>true if the camera was moved to right</returns>
      bool MakeSureCursorIsVisibleHorizontal(in List<Line> lines, ref Camera2D camera, int fontSize, int leftPadding, Font font, Int2 cursorWorldSpacePosition) {
         int leftEdgeWorldSpacePositionX = (int)Raylib.GetScreenToWorld2D(Vector2.Zero, camera).X;
         int rightEdgeWorldSpacePositionX = (int)Raylib.GetScreenToWorld2D(new Vector2(Raylib.GetScreenWidth(), 0), camera).X;

         int a = cursorWorldSpacePosition.x - rightEdgeWorldSpacePositionX; // Explanation: ./CursorScreenExplanation.png
         int b = cursorWorldSpacePosition.x - leftEdgeWorldSpacePositionX; // Explanation: ./CursorScreenExplanation.png

         if (Math.Abs(a) + Math.Abs(b) <= Raylib.GetScreenWidth()) return false;

         // Offset is applied so we can see the cursor otherwise it would be at the edge of the screen and not visible.
         int offset = (int)Raylib.MeasureTextEx(font, "A", fontSize, 0).X - 1;

         if (Math.Abs(a) < Math.Abs(b)) {
            camera.target.X += a + offset;
            return true;
         } else if (Math.Abs(b) < Math.Abs(a)) {
            camera.target.X += b - offset;
            return false;
         }

         return false;
      }

      /// <returns>true if camera was moved to down</returns>
      bool MakeSureCursorIsVisibleVertical(in List<Line> lines, ref Camera2D camera, int fontSize, int leftPadding, Font font, Int2 cursorWorldSpacePosition) {
         int topEdgeWorldSpacePositionY = (int)Raylib.GetScreenToWorld2D(Vector2.Zero, camera).Y + Program.YMargin;
         int bottomEdgeWorldSpacePositionY = (int)Raylib.GetScreenToWorld2D(new Vector2(0, Raylib.GetScreenHeight()), camera).Y;
         bottomEdgeWorldSpacePositionY -= Line.Height;

         int a = cursorWorldSpacePosition.y - topEdgeWorldSpacePositionY; // Explanation: ./CursorScreenExplanation.png
         int b = cursorWorldSpacePosition.y - bottomEdgeWorldSpacePositionY; // Explanation: ./CursorScreenExplanation.png

         if (Math.Abs(a) + Math.Abs(b) <= Raylib.GetScreenHeight() - Program.YMargin) return false;

         if (Math.Abs(a) < Math.Abs(b)) {
            camera.target.Y += a;
            return false;
         } else if (Math.Abs(b) < Math.Abs(a)) {
            camera.target.Y += b;
            return true;
         }

         return false;
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

         if (pos.y > lines.Count - 1) pos.y = lines.Count - 1;

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

      [Obsolete("Use CalculateNextJumpLocationX() instead")]
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

      int FindIndexOfNextBlankLine(List<Line> lines, int startIndex, Direction direction) {
         Regex blankLineRegex = new Regex(@"^\s*$");

         switch (direction) {
            case Direction.Up: {
               for (int i = startIndex - 1; i >= 0; i--) {
                  if (blankLineRegex.IsMatch(lines[i].Value)) {
                     return i;
                  }
               }

               return -1;
            }

            case Direction.Down: {
               for (int i = startIndex + 1; i < lines.Count; i++) {
                  if (blankLineRegex.IsMatch(lines[i].Value)) {
                     return i;
                  }
               }

               return -1;
            }

            default: throw new Exception("Invalid direction");
         }
      }

      public int CalculateNextJumpLocationX(in List<Line> lines, Direction direction) {
         switch (direction) {
            case Direction.Right: {
               bool isStartingCharSpace = lines[position.y].Value[position.x + 1] == ' ';
               bool isCurrentCharSpace;

               for (int i = position.x + 2; i < lines[position.y].Value.Length; i++) {
                  isCurrentCharSpace = lines[position.y].Value[i] == ' ';
                  if (isCurrentCharSpace != isStartingCharSpace) {
                     return isStartingCharSpace ? i : i;
                  }
               }

               return lines[position.y].Value.Length;
            }

            case Direction.Left: {
               bool isStartingCharSpace = lines[position.y].Value[position.x - 2] == ' ';
               bool isCurrentCharSpace;

               for (int i = position.x - 2; i >= 0; i--) {
                  isCurrentCharSpace = lines[position.y].Value[i] == ' ';
                  if (isCurrentCharSpace != isStartingCharSpace) {
                     return isStartingCharSpace ? i + 1 : i + 1;
                  }
               }

               return 0;
            }

            default: throw new Exception("Invalid direction");
         }
      }

      int ComputeNextJumpLocation(List<Line> lines, int currentLocation, Direction direction) {
         Regex blankLineRegex = new Regex(@"^\s*$");

         switch (direction) {
            case Direction.Up: {
               bool isStartingLineBlank = blankLineRegex.IsMatch(lines[currentLocation - 1].Value);
               bool isCurrentLineBlank;

               for (int i = currentLocation - 2; i >= 0; i--) {
                  isCurrentLineBlank = blankLineRegex.IsMatch(lines[i].Value);
                  if (isCurrentLineBlank != isStartingLineBlank) {
                     return isStartingLineBlank ? i + 1 : i;
                  }
               }

               return 0;
            }

            case Direction.Down: {
               bool isStartingLineBlank = blankLineRegex.IsMatch(lines[currentLocation + 1].Value);
               bool isCurrentLineBlank;

               for (int i = currentLocation + 2; i < lines.Count; i++) {
                  isCurrentLineBlank = blankLineRegex.IsMatch(lines[i].Value);
                  if (isCurrentLineBlank != isStartingLineBlank) {
                     return isStartingLineBlank ? i - 1 : i;
                  }
               }

               return lines.Count - 1;
            }

            default: throw new Exception("Invalid direction");
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
