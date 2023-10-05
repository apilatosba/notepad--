﻿#define VISUAL_STUDIO
using Raylib_CsLo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Linq;
using System.IO;

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
      static Int2? lastKnownCursorPosition = null;
      float flashShaderTransparency = 0.0f;
      Stopwatch flashShaderTimer = new Stopwatch();

      // this code causes problems. Searched the web and it is probably related to loading a different asssembly. In this case it is raylib.
      // if you have static variables of classes that belongs other assemblies it becomes problematic.
      // using Program.font other than in constructor didnt cause any issue. the target architecture of the assemblies must be the same i think.
      // I don't know how to fix this. I will just comment out this code for now.
      // https://stackoverflow.com/questions/4398334/the-type-initializer-for-myclass-threw-an-exception
      // https://learn.microsoft.com/en-us/dotnet/api/system.typeinitializationexception?view=net-7.0

      //      public EditorStatePlaying() {
      //         Raylib.UnloadFont(Program.font);
      //#if VISUAL_STUDIO
      //         Program.config.Deserialize(Program.CONFIG_FILE_NAME);
      //#else
      //         Program.config.Deserialize(Program.GetConfigPath());
      //#endif
      //         //Program.font = Program.LoadFontWithAllUnicodeCharacters("Fonts/Inconsolata-Medium.ttf", Program.config.fontSize);

      //         cursor.position = lastKnownCursorPosition ?? new Int2(0, 0);
      //      }

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

            ///////////////////////////////////////////
            // Handling key presses that have modifiers
            ///////////////////////////////////////////
            {
               if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL)) {
                  if (Raylib.IsKeyPressed(KeyboardKey.KEY_S)) {
                     flashShaderTimer.Restart();

                     Program.WriteLinesToFile(Program.filePath, Program.lines);

                     if (Path.GetFileName(Program.filePath) == Program.CONFIG_FILE_NAME) {
                        Program.config.Deserialize(Program.GetConfigPath());
                     }
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
                     shiftSelection = new Selection() {
                        StartPosition = cursor.position
                     };

                     string clipboardText = Raylib.GetClipboardText_();
                     List<Line> clipboard = Program.ReadLinesFromString(clipboardText);
                     Program.InsertLinesAtCursor(Program.lines, cursor, clipboard);

                     shiftSelection.EndPosition = cursor.position;

                     cursor.MakeSureCursorIsVisibleToCamera(Program.lines, ref camera, Program.config.fontSize, Program.config.leftPadding, Program.font);
                  }
               }

               if (Raylib.IsKeyPressed(KeyboardKey.KEY_LEFT_SHIFT) || Raylib.IsKeyPressed(KeyboardKey.KEY_RIGHT_SHIFT)) {
                  shiftSelection ??= new Selection(cursor.position, cursor.position);
               }
            }

            ////////////////////////////////////////////////////////////////////////
            // Handling key presses that don't have modifiers ie. normal key presses
            ////////////////////////////////////////////////////////////////////////
            {
               if (pressedKeys != null) {
#if VISUAL_STUDIO
                  Program.PrintPressedKeys(pressedKeys);
#endif
                  shiftSelection?.Delete(Program.lines, cursor);
                  shiftSelection = null;

                  Program.InsertTextAtCursor(Program.lines, cursor, pressedKeys);
                  cursor.MakeSureCursorIsVisibleToCamera(Program.lines, ref camera, Program.config.fontSize, Program.config.leftPadding, Program.font);
               }
            }

            /////////////////////////////////////////////////////////
            // Handling special keys, both with and without modifiers
            /////////////////////////////////////////////////////////
            {
               if (specialKey != KeyboardKey.KEY_NULL) {
#if VISUAL_STUDIO
                  Console.WriteLine(specialKey);
#endif
                  switch (specialKey) {
                     case KeyboardKey.KEY_HOME:
                        if (!(modifiers.Contains(KeyboardKey.KEY_LEFT_SHIFT) || modifiers.Contains(KeyboardKey.KEY_RIGHT_SHIFT))) {
                           if (shiftSelection?.StartPosition == shiftSelection?.EndPosition) shiftSelection = null;
                        }

                        if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL)) {
                           cursor.position.y = 0;
                        }

                        cursor.position.x = 0;
                        cursor.MakeSureCursorIsVisibleToCamera(Program.lines, ref camera, Program.config.fontSize, Program.config.leftPadding, Program.font);
                        break;
                     case KeyboardKey.KEY_END: {
                           if (!(modifiers.Contains(KeyboardKey.KEY_LEFT_SHIFT) || modifiers.Contains(KeyboardKey.KEY_RIGHT_SHIFT))) {
                              if (shiftSelection?.StartPosition == shiftSelection?.EndPosition) shiftSelection = null;
                           }

                           Line currentLine = Program.lines[cursor.position.y];

                           cursor.position.x = currentLine.Value.Length;
                           cursor.MakeSureCursorIsVisibleToCamera(Program.lines, ref camera, Program.config.fontSize, Program.config.leftPadding, Program.font);
                        }
                        break;
                     case KeyboardKey.KEY_ESCAPE:
                        lastKnownCursorPosition = cursor.position;
                        SetStateTo(new EditorStatePaused());
                        break;
                     case KeyboardKey.KEY_BACKSPACE:
                        if (shiftSelection != null) {
                           shiftSelection.Delete(Program.lines, cursor);
                           shiftSelection = null;
                           cursor.MakeSureCursorIsVisibleToCamera(Program.lines, ref camera, Program.config.fontSize, Program.config.leftPadding, Program.font);
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

                        } else {
                           if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL)) {
                              int howManyCharactersToJump = cursor.CalculateHowManyCharactersToJump(Program.lines, Direction.Left);
                              Program.RemoveTextAtCursor(Program.lines, cursor, howManyCharactersToJump);
                           } else {
                              Program.RemoveTextAtCursor(Program.lines, cursor, 1);
                           }
                        }

                        cursor.MakeSureCursorIsVisibleToCamera(Program.lines, ref camera, Program.config.fontSize, Program.config.leftPadding, Program.font);
                        break;
                     case KeyboardKey.KEY_ENTER: {
                           shiftSelection?.Delete(Program.lines, cursor);
                           shiftSelection = null;

                           Line currentLine = Program.lines[cursor.position.y];
                           string textAfterCursor = currentLine.Value.Substring(cursor.position.x);

                           Line newLine = new Line(textAfterCursor);

                           if (cursor.IsCursorAtEndOfFile(Program.lines)) {
                              Program.lines.Add(newLine);
                           } else {
                              Program.lines.Insert(Program.lines.IndexOf(currentLine) + 1, newLine);
                           }

                           currentLine.RemoveTextAt(cursor.position.x, currentLine.Value.Length - cursor.position.x, Direction.Right);

                           cursor.position.x = 0;
                           cursor.position.y++;
                        }

                        cursor.MakeSureCursorIsVisibleToCamera(Program.lines, ref camera, Program.config.fontSize, Program.config.leftPadding, Program.font);
                        break;
                     case KeyboardKey.KEY_TAB:
                        if (shiftSelection != null) {
                           Line[] linesInRange = shiftSelection.GetLinesInRange(Program.lines).ToArray();

                           foreach (Line line in linesInRange) {
                              line.InsertTextAt(0, new string(' ', Program.config.tabSize));
                           }

                           shiftSelection.StartPosition = new Int2(shiftSelection.StartPosition.x + Program.config.tabSize, shiftSelection.StartPosition.y);
                           cursor.position.x += Program.config.tabSize;

                           cursor.MakeSureCursorIsVisibleToCamera(Program.lines, ref camera, Program.config.fontSize, Program.config.leftPadding, Program.font);

                           break;
                        }

                        Program.InsertTextAtCursor(Program.lines, cursor, new string(' ', Program.config.tabSize));

                        cursor.MakeSureCursorIsVisibleToCamera(Program.lines, ref camera, Program.config.fontSize, Program.config.leftPadding, Program.font);
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
                           Program.config.spacingBetweenLines++;
                        }
                        //camera.target.Y -= 10;
                        break;
                     case KeyboardKey.KEY_DOWN:
                        if (Raylib.IsKeyUp(KeyboardKey.KEY_LEFT_SHIFT) && Raylib.IsKeyUp(KeyboardKey.KEY_RIGHT_SHIFT)) shiftSelection = null;
                        if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL)) {
                           Program.config.spacingBetweenLines--;
                        }
                        //camera.target.Y += 10;
                        break;
                  }
               }
            }

            cursor.HandleArrowKeysNavigation(Program.lines,
                                             ref camera,
                                             Program.config.fontSize,
                                             Program.config.leftPadding,
                                             Program.font,
                                             modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL));
         } // End of keyboard input handling

         mouseWheelInput = Raylib.GetMouseWheelMove();
         camera.target.Y -= mouseWheelInput * Line.Height;

         if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT)) {
            Vector2 mousePosition = Raylib.GetMousePosition();
            Int2 mousePositionInWorldSpace = (Int2)Raylib.GetScreenToWorld2D(mousePosition, camera);

            cursor.position = cursor.CalculatePositionFromWorldSpaceCoordinates(Program.lines, Program.config.fontSize, Program.config.leftPadding, Program.font, mousePositionInWorldSpace);

            shiftSelection = null;
            mouseSelection = new Selection(cursor.position, cursor.position);
         }

         if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT)) {
            Debug.Assert(mouseSelection != null);

            Vector2 mousePosition = Raylib.GetMousePosition();
            Int2 mousePositionInWorldSpace = (Int2)Raylib.GetScreenToWorld2D(mousePosition, camera);

            mouseSelection.EndPosition = cursor.CalculatePositionFromWorldSpaceCoordinates(Program.lines, Program.config.fontSize, Program.config.leftPadding, Program.font, mousePositionInWorldSpace);
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

      public unsafe void Render() {
         if (Raylib.IsWindowResized()) {
            Program.windowCoverImage = Raylib.GenImageColor(Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), new Color(255, 255, 255, 255));
            Program.windowCoverTexture = Raylib.LoadTextureFromImage(Program.windowCoverImage);
         }

         if (flashShaderTimer.IsRunning) {
            flashShaderTransparency = MathF.Exp(-1 * 6 * (flashShaderTimer.ElapsedMilliseconds / 1000.0f));
         }

         fixed (float* value = &flashShaderTransparency) {
            Raylib.SetShaderValue(Program.flashShader, Program.flashShaderTransparencyLoc, value, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
         }

         // World space rendering
         Raylib.BeginMode2D(camera);
         {
            Raylib.ClearBackground(Program.config.backgroundColor);

            Program.RenderLines(Program.lines, Program.font);
            shiftSelection?.Render(Program.lines, Program.config.fontSize, Program.config.leftPadding, Program.font);
            mouseSelection?.Render(Program.lines, Program.config.fontSize, Program.config.leftPadding, Program.font);
            cursor.Render(Program.lines, Program.config.fontSize, Program.config.leftPadding, Program.font, Program.config.spacingBetweenLines);
         }
         Raylib.EndMode2D();

         // Screen space rendering ie. UI
         {
            //horizontalScrollBar.RenderHorizontal(Raylib.GetScreenWidth());

            Raylib.BeginShaderMode(Program.flashShader);

            //Raylib.DrawRectangleRec(windowCover, new Color(255, 255, 255, 255));
            Raylib.DrawTexture(Program.windowCoverTexture, 0, 0, Raylib.WHITE);

            Raylib.EndShaderMode();
         }
      }

      public void Update() {
         HandleInput();
         PostHandleInput();
         Render();
      }

      public void SetStateTo(IEditorState state) {
         Program.editorState = state;
         state.EnterState();
      }

      public void EnterState() {
         Raylib.UnloadFont(Program.font);
         Program.config.Deserialize(Program.GetConfigPath());

         Program.font = Program.LoadFontWithAllUnicodeCharacters(Program.GetFontFilePath(), Program.config.fontSize);

         cursor.position = lastKnownCursorPosition ?? new Int2(0, 0);
      }
   }
}
