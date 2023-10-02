using Raylib_CsLo;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Linq;

namespace Notepad___Raylib {
   internal class EditorStatePlaying : IEditorState {
      Selection shiftSelection = null;
      Selection mouseSelection = null;
      Cursor cursor = new Cursor();
      Camera2D camera = new Camera2D() {
         zoom = 1.0f,
         target = new Vector2(0, 0),
         rotation = 0.0f,
         offset = new Vector2(0, 0),
      };
      float mouseWheelInput = 0;

      public void HandleInput() {
         Debug.Assert(!(mouseSelection != null && shiftSelection != null));

         // Keyboard input handling
         if (Program.ShouldAcceptKeyboardInput(out string pressedKeys, out KeyboardKey specialKey)) {
            List<KeyboardKey> modifiers = new List<KeyboardKey>();
            if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL)) modifiers.Add(KeyboardKey.KEY_LEFT_CONTROL);
            if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT)) modifiers.Add(KeyboardKey.KEY_LEFT_SHIFT);
            if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_ALT)) modifiers.Add(KeyboardKey.KEY_LEFT_ALT);
            if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SUPER)) modifiers.Add(KeyboardKey.KEY_LEFT_SUPER);
            if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL)) modifiers.Add(KeyboardKey.KEY_RIGHT_CONTROL);
            if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SHIFT)) modifiers.Add(KeyboardKey.KEY_RIGHT_SHIFT);
            if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_ALT)) modifiers.Add(KeyboardKey.KEY_RIGHT_ALT);
            if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SUPER)) modifiers.Add(KeyboardKey.KEY_RIGHT_SUPER);

            // Handling key presses that have modifiers
            {
               if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL)) {
                  if (Raylib.IsKeyPressed(KeyboardKey.KEY_S)) {
                     Program.WriteLinesToFile(Program.filePath, Program.lines);
                  }

                  if (Raylib.IsKeyPressed(KeyboardKey.KEY_C)) {
                     mouseSelection?.Copy(Program.lines);
                     shiftSelection?.Copy(Program.lines);
                  }

                  // No mouseSelection. It causes lots of issues. So I chose the easy path and I don't allow user to ctrl+x while he is holding down mouse1. User needs to release mouse1 and then press ctrl+x.
                  if (Raylib.IsKeyPressed(KeyboardKey.KEY_X)) {
                     shiftSelection?.Copy(Program.lines);
                     shiftSelection?.Delete(Program.lines, cursor);
                     shiftSelection = null;
                  }

                  if (Raylib.IsKeyPressed(KeyboardKey.KEY_V)) {
                     mouseSelection?.Delete(Program.lines, cursor);
                     shiftSelection?.Delete(Program.lines, cursor);
                     shiftSelection = null;

                     string clipboardText = Raylib.GetClipboardText_();
                     List<Line> clipboard = Program.ReadLinesFromString(clipboardText);
                     Program.InsertLinesAtCursor(Program.lines, cursor, clipboard);

                     cursor.MakeSureCursorIsVisibleToCamera(Program.lines, ref camera, Program.fontSize, Program.leftPadding, Program.font);
                  }
               }

               if (Raylib.IsKeyPressed(KeyboardKey.KEY_LEFT_SHIFT) || Raylib.IsKeyPressed(KeyboardKey.KEY_RIGHT_SHIFT)) {
                  shiftSelection ??= new Selection(cursor.position, cursor.position);
               }
            }

            // Handling key presses that don't have modifiers ie. normal key presses
            {
               if (pressedKeys != null) {
#if VISUAL_STUDIO
                        PrintPressedKeys(pressedKeys);
#endif
                  shiftSelection?.Delete(Program.lines, cursor);
                  shiftSelection = null;

                  Program.InsertTextAtCursor(Program.lines, cursor, pressedKeys);
                  cursor.MakeSureCursorIsVisibleToCamera(Program.lines, ref camera, Program.fontSize, Program.leftPadding, Program.font);
               }
            }

            // Handling special keys, both with and without modifiers
            {
               if (specialKey != KeyboardKey.KEY_NULL) {
#if VISUAL_STUDIO
                        Console.WriteLine(specialKey);
#endif
                  switch (specialKey) {
                     case KeyboardKey.KEY_ESCAPE:

                        break;
                     case KeyboardKey.KEY_BACKSPACE:
                        if (shiftSelection != null) {
                           shiftSelection.Delete(Program.lines, cursor);
                           shiftSelection = null;
                           break;
                        }

                        if (cursor.IsCursorAtBeginningOfFile()) break;

                        if (cursor.IsCursorAtBeginningOfLine()) {
                           Line currentLine = Program.lines[cursor.position.y];
                           Line lineAbove = Program.lines[cursor.position.y - 1];

                           cursor.position.x = lineAbove.Value.Length;

                           lineAbove.InsertTextAt(lineAbove.Value.Length, currentLine.Value);
                           Program.lines.RemoveAt(cursor.position.y);

                           cursor.position.y--;

                           for (int i = cursor.position.y + 1; i < Program.lines.Count; i++) {
                              Program.lines[i].LineNumber--;
                           }
                        } else {
                           Program.RemoveTextAtCursor(Program.lines, cursor, 1);
                        }

                        break;
                     case KeyboardKey.KEY_ENTER: {
                           shiftSelection?.Delete(Program.lines, cursor);
                           shiftSelection = null;

                           Line currentLine = Program.lines[cursor.position.y];
                           string textAfterCursor = currentLine.Value.Substring(cursor.position.x);

                           Line newLine = new Line(textAfterCursor, currentLine.LineNumber + 1);

                           if (cursor.IsCursorAtEndOfFile(Program.lines)) {
                              Program.lines.Add(newLine);
                           } else {
                              Program.lines.Insert((int)currentLine.LineNumber + 1, newLine);
                           }

                           currentLine.RemoveTextAt(cursor.position.x, currentLine.Value.Length - cursor.position.x, Direction.Right);

                           cursor.position.x = 0;
                           cursor.position.y++;

                           for (int i = cursor.position.y + 1; i < Program.lines.Count; i++) {
                              Program.lines[i].LineNumber++;
                           }
                        }
                        break;
                     case KeyboardKey.KEY_TAB:
                        if (shiftSelection != null) {
                           Line[] linesInRange = shiftSelection.GetLinesInRange(Program.lines).ToArray();

                           foreach (Line line in linesInRange) {
                              line.InsertTextAt(0, new string(' ', Program.tabSize));
                           }

                           shiftSelection.StartPosition = new Int2(shiftSelection.StartPosition.x + Program.tabSize, shiftSelection.StartPosition.y);
                           cursor.position.x += Program.tabSize;

                           break;
                        }

                        Program.InsertTextAtCursor(Program.lines, cursor, new string(' ', Program.tabSize));
                        break;
                     case KeyboardKey.KEY_DELETE:
                        if (shiftSelection != null) {
                           shiftSelection.Delete(Program.lines, cursor);
                           shiftSelection = null;
                           break;
                        }

                        if (cursor.IsCursorAtEndOfLine(Program.lines)) {
                           Line currentLine = Program.lines[cursor.position.y];
                           Line lineBelow = Program.lines[cursor.position.y + 1];

                           currentLine.InsertTextAt(currentLine.Value.Length, lineBelow.Value);

                           Program.lines.RemoveAt(cursor.position.y + 1);

                           for (int i = cursor.position.y + 1; i < Program.lines.Count; i++) {
                              Program.lines[i].LineNumber--;
                           }
                        } else {
                           Program.RemoveTextAtCursor(Program.lines, cursor, 1, Direction.Right);
                        }

                        break;
                     case KeyboardKey.KEY_RIGHT:
                        if (Raylib.IsKeyUp(KeyboardKey.KEY_LEFT_SHIFT) && Raylib.IsKeyUp(KeyboardKey.KEY_RIGHT_SHIFT)) shiftSelection = null;
                        //camera.target.X += 10;
                        break;
                     case KeyboardKey.KEY_LEFT:
                        if (Raylib.IsKeyUp(KeyboardKey.KEY_LEFT_SHIFT) && Raylib.IsKeyUp(KeyboardKey.KEY_RIGHT_SHIFT)) shiftSelection = null;
                        //camera.target.X -= 10;
                        break;
                     case KeyboardKey.KEY_UP:
                        if (Raylib.IsKeyUp(KeyboardKey.KEY_LEFT_SHIFT) && Raylib.IsKeyUp(KeyboardKey.KEY_RIGHT_SHIFT)) shiftSelection = null;
                        if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL)) {
                           Program.spacingBetweenLines++;
                        }
                        //camera.target.Y -= 10;
                        break;
                     case KeyboardKey.KEY_DOWN:
                        if (Raylib.IsKeyUp(KeyboardKey.KEY_LEFT_SHIFT) && Raylib.IsKeyUp(KeyboardKey.KEY_RIGHT_SHIFT)) shiftSelection = null;
                        if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL)) {
                           Program.spacingBetweenLines--;
                        }
                        //camera.target.Y += 10;
                        break;
                  }
               }
            }

            cursor.HandleArrowKeysNavigation(Program.lines, ref camera, Program.fontSize, Program.leftPadding, Program.font);
         } // End of keyboard input handling

         mouseWheelInput = Raylib.GetMouseWheelMove();
         camera.target.Y -= mouseWheelInput * Line.Height;

         if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT)) {
            Vector2 mousePosition = Raylib.GetMousePosition();
            Int2 mousePositionInWorldSpace = (Int2)Raylib.GetScreenToWorld2D(mousePosition, camera);

            cursor.position = cursor.CalculatePositionFromWorldSpaceCoordinates(Program.lines, Program.fontSize, Program.leftPadding, Program.font, mousePositionInWorldSpace);

            shiftSelection = null;
            mouseSelection = new Selection(cursor.position, cursor.position);
         }

         if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT)) {
            Debug.Assert(mouseSelection != null);

            Vector2 mousePosition = Raylib.GetMousePosition();
            Int2 mousePositionInWorldSpace = (Int2)Raylib.GetScreenToWorld2D(mousePosition, camera);

            mouseSelection.EndPosition = cursor.CalculatePositionFromWorldSpaceCoordinates(Program.lines, Program.fontSize, Program.leftPadding, Program.font, mousePositionInWorldSpace);
            cursor.position = mouseSelection.EndPosition;
         }

         if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT)) {
            shiftSelection = mouseSelection;
            mouseSelection = null;
         }

         //horizontalScrollBar.UpdateHorizontal(ref camera, FindDistanceToRightMostChar(Program.lines, font), Raylib.GetScreenWidth());
      }

      public void PostHandleInput() {
         if (shiftSelection != null) shiftSelection.EndPosition = cursor.position;
         Program.MakeSureCameraNotBelowZeroInBothAxes(ref camera);
      }

      public void Render() {
         Raylib.BeginDrawing();

         // World space rendering
         Raylib.BeginMode2D(camera);
         {
            Raylib.ClearBackground(Program.BACKGROUND_COLOR);

            Program.RenderLines(Program.lines, Program.font);
            shiftSelection?.Render(Program.lines, Program.fontSize, Program.leftPadding, Program.font);
            mouseSelection?.Render(Program.lines, Program.fontSize, Program.leftPadding, Program.font);
            cursor.Render(Program.lines, Program.fontSize, Program.leftPadding, Program.font, Program.spacingBetweenLines);
         }
         Raylib.EndMode2D();

         // Screen space rendering ie. UI
         {
            //horizontalScrollBar.RenderHorizontal(Raylib.GetScreenWidth());
         }

         Raylib.EndDrawing();
      }
   }
}
