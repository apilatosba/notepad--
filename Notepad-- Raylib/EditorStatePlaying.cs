#define VISUAL_STUDIO
using Raylib_CsLo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;

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
      readonly Stopwatch flashShaderTimer = new Stopwatch();
      string missedKeyPressesBetweenFrames = string.Empty;

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
         string pressedKeys;
         if (Program.ShouldAcceptKeyboardInput(out pressedKeys, out KeyboardKey specialKey)) {
            List<KeyboardKey> modifiers = new List<KeyboardKey>();
            if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL)) modifiers.Add(KeyboardKey.KEY_LEFT_CONTROL);
            if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT)) modifiers.Add(KeyboardKey.KEY_LEFT_SHIFT);
            if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_ALT)) modifiers.Add(KeyboardKey.KEY_LEFT_ALT);
            if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SUPER)) modifiers.Add(KeyboardKey.KEY_LEFT_SUPER);
            if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL)) modifiers.Add(KeyboardKey.KEY_RIGHT_CONTROL);
            if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SHIFT)) modifiers.Add(KeyboardKey.KEY_RIGHT_SHIFT);
            if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_ALT)) modifiers.Add(KeyboardKey.KEY_RIGHT_ALT);
            if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SUPER)) modifiers.Add(KeyboardKey.KEY_RIGHT_SUPER);

            pressedKeys = missedKeyPressesBetweenFrames + pressedKeys;
            pressedKeys = pressedKeys == string.Empty ? null : pressedKeys;

            ///////////////////////////////////////////
            // Handling key presses that have modifiers
            ///////////////////////////////////////////
            {
               if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL)) {
                  if (Raylib.IsKeyPressed(KeyboardKey.KEY_A)) {
                     cursor.position = new Int2(Program.lines[Program.lines.Count - 1].Value.Length, Program.lines.Count - 1);
                     shiftSelection = new Selection(new Int2(0, 0), cursor.position);

                     cursor.MakeSureCursorIsVisibleToCamera(Program.lines, ref camera, Program.config.fontSize, Program.config.leftPadding, Program.font);
                  }

                  if (Raylib.IsKeyPressed(KeyboardKey.KEY_S)) {
                     int previousfontSize = Program.config.fontSize;

                     flashShaderTimer.Restart();

                     Program.WriteLinesToFile(Program.filePath, Program.lines);

                     if (Path.GetFileName(Program.filePath) == Program.CONFIG_FILE_NAME) {
                        try {
                           Program.config.Deserialize(Program.GetConfigPath());

                           if (Program.config.fontSize != previousfontSize) {
                              Raylib.UnloadFont(Program.font);
                              Program.font = Program.LoadFontWithAllUnicodeCharacters(Program.GetFontFilePath(), Program.config.fontSize);
                           }
                        }
                        catch (InvalidOperationException) {
                           Console.WriteLine("Your config file was corrupt. Reverting it to previous settings now.");
                           Program.config.Serialize(Program.GetConfigPath());
                           Program.lines = Program.ReadLinesFromFile(Program.filePath);
                        }
                     }
                  }

                  if (Raylib.IsKeyPressed(KeyboardKey.KEY_C)) {
                     mouseSelection?.Copy(Program.lines);
                     shiftSelection?.Copy(Program.lines);
                  }

                  // No mouseSelection. It causes lots of issues. So I chose the easy path and I don't allow user to ctrl+x while he is holding down mouse1. User needs to release mouse1 and then press ctrl+x.
                  if (Raylib.IsKeyPressed(KeyboardKey.KEY_X)) {
                     shiftSelection?.Cut(Program.lines, cursor);
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

                  if (Raylib.IsKeyPressed(KeyboardKey.KEY_L)) {
                     if (!cursor.IsCursorAtLastLine(Program.lines)) {
                        shiftSelection = null;
                        cursor.position.x = 0;
                        shiftSelection = new Selection() {
                           StartPosition = cursor.position
                        };
                        cursor.position.y++;
                        shiftSelection.EndPosition = cursor.position;
                        shiftSelection.Cut(Program.lines, cursor);
                        shiftSelection = null;
                     } else {
                        cursor.position.x = 0;
                        shiftSelection = new Selection() {
                           StartPosition = cursor.position
                        };
                        cursor.position.x = Program.lines[cursor.position.y].Value.Length;
                        shiftSelection.EndPosition = cursor.position;
                        shiftSelection.CopyAndAppend(Program.lines, "\n");
                        shiftSelection.Delete(Program.lines, cursor);
                        shiftSelection = null;
                        Program.lines.RemoveAt(cursor.position.y);
                        cursor.position.y--;
                        cursor.position.x = 0;
                     }
                  }

                  if (Raylib.IsKeyPressed(KeyboardKey.KEY_M)) {
                     unsafe {
                        Image image = Raylib.LoadImageFromTexture(Program.textMask.texture);
                        Image* imagePtr = &image;
                        Raylib.ImageFlipVertical(imagePtr);
                        Raylib.ExportImage(*imagePtr, "text mask.png");
                     }
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

                           if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL)) {
                              cursor.position.y = Program.lines.Count - 1;
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
                           if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL)) {
                              int howManyCharactersToJump = cursor.CalculateHowManyCharactersToJump(Program.lines, Direction.Right);
                              Program.RemoveTextAtCursor(Program.lines, cursor, howManyCharactersToJump, Direction.Right);
                           } else {
                              Program.RemoveTextAtCursor(Program.lines, cursor, 1, Direction.Right);
                           }
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
                        if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL)) {
                           camera.target.Y -= Line.Height;
                        }
                        //camera.target.Y -= 10;
                        break;
                     case KeyboardKey.KEY_DOWN:
                        if (Raylib.IsKeyUp(KeyboardKey.KEY_LEFT_SHIFT) && Raylib.IsKeyUp(KeyboardKey.KEY_RIGHT_SHIFT)) shiftSelection = null;
                        if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL)) {
                           camera.target.Y += Line.Height;
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

            missedKeyPressesBetweenFrames = string.Empty;
         } else {
            missedKeyPressesBetweenFrames += pressedKeys;
         } // End of keyboard input handling

         mouseWheelInput = Raylib.GetMouseWheelMove();
         camera.target.Y -= mouseWheelInput * Line.Height;

         {
            int heightOfAllLines = Program.lines.Count * Line.Height;
            int cameraThreshold = heightOfAllLines - Line.Height;

            if (camera.target.Y > cameraThreshold) {
               camera.target.Y = cameraThreshold;
            }
         }

         if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT)) {
            Vector2 mousePosition = Raylib.GetMousePosition();
            Int2 mousePositionInWorldSpace = (Int2)Raylib.GetScreenToWorld2D(mousePosition, camera);

            cursor.position = cursor.CalculatePositionFromWorldSpaceCoordinates(Program.lines,
                                                                                Program.config.fontSize,
                                                                                Program.config.leftPadding,
                                                                                Program.font,
                                                                                mousePositionInWorldSpace);

            shiftSelection = null;
            mouseSelection = new Selection(cursor.position, cursor.position);
         }

         if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT)) {
            Debug.Assert(mouseSelection != null);

            Vector2 mousePosition = Raylib.GetMousePosition();
            Int2 mousePositionInWorldSpace = (Int2)Raylib.GetScreenToWorld2D(mousePosition, camera);

            mouseSelection.EndPosition = cursor.CalculatePositionFromWorldSpaceCoordinates(Program.lines,
                                                                                           Program.config.fontSize,
                                                                                           Program.config.leftPadding,
                                                                                           Program.font,
                                                                                           mousePositionInWorldSpace);
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

            Program.textMask = Raylib.LoadRenderTexture(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
         }

         if (flashShaderTimer.IsRunning) {
            flashShaderTransparency = MathF.Exp(-1 * 6 * (flashShaderTimer.ElapsedMilliseconds / 1000.0f));
         }

         fixed (float* value = &flashShaderTransparency) {
            Raylib.SetShaderValue(Program.flashShader, Program.flashShaderTransparencyLoc, value, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);
         }

         //////////////////////////////////////
         // Screen space rendering (background)
         //////////////////////////////////////
         //{
         //   if (Raylib.IsWindowResized()) {
         //      int w = Program.background.width;
         //      int h = Program.background.height;
         //      int sw = Raylib.GetScreenWidth();
         //      int sh = Raylib.GetScreenHeight();
         //
         //      if (h * ((float)sw / w) >= sh) {
         //         Program.backgroundScale = (float)sw / w;
         //         Program.backgroundPosition = new Vector2(0, -((h * Program.backgroundScale - sh) / 2));
         //      } else {
         //         Program.backgroundScale = (float)sh / h;
         //         Program.backgroundPosition = new Vector2(-((w * Program.backgroundScale - sw) / 2), 0);
         //      }
         //   }
         //
         //   int lucidity = (int)(Math.Clamp(Program.config.backgroundLucidity, 0, 1) * 255);
         //   Raylib.DrawTextureEx(Program.background, Program.backgroundPosition, 0, Program.backgroundScale, new Color(lucidity, lucidity, lucidity, 255));
         //}

         ////////////////////////
         // World space rendering
         ////////////////////////
         //Raylib.BeginMode2D(camera);
         //{
         //
         //   Raylib.BeginBlendMode(BlendMode.BLEND_ADDITIVE);
         //   Rectangle highlightLineRect = new Rectangle(0, Line.Height * cursor.position.y, float.MaxValue, Line.Height);
         //   Raylib.DrawRectangleRec(highlightLineRect, new Color(13, 13, 13, 255));
         //   Raylib.EndBlendMode();
         //
         //   Program.RenderLines(Program.lines, Program.font);
         //   shiftSelection?.Render(Program.lines, Program.config.fontSize, Program.config.leftPadding, Program.font);
         //   mouseSelection?.Render(Program.lines, Program.config.fontSize, Program.config.leftPadding, Program.font);
         //   cursor.Render(Program.lines, Program.config.fontSize, Program.config.leftPadding, Program.font, Program.config.spacingBetweenLines);
         //}
         //Raylib.EndMode2D();

         //////////////////////////
         // RenderTexture rendering
         //////////////////////////
         Raylib.BeginTextureMode(Program.textMask);
         {
            Raylib.BeginMode2D(camera);
            Raylib.ClearBackground(Raylib.BLANK);
            Program.RenderLines(Program.lines, Program.font, Raylib.WHITE);
            shiftSelection?.Render(Program.lines, Program.config.fontSize, Program.config.leftPadding, Program.font, Raylib.WHITE);
            mouseSelection?.Render(Program.lines, Program.config.fontSize, Program.config.leftPadding, Program.font, Raylib.WHITE);
            cursor.Render(Program.lines, Program.config.fontSize, Program.config.leftPadding, Program.font, Program.config.spacingBetweenLines, Raylib.WHITE);
            Raylib.EndMode2D();
         }
         Raylib.EndTextureMode();

         fixed(Texture* value = &Program.textMask.texture) {
            Raylib.SetShaderValue(Program.bloomShader, Program.bloomShaderTextMaskLoc, value, ShaderUniformDataType.SHADER_UNIFORM_SAMPLER2D);
         }
         //Raylib.SetShaderValueTexture(Program.bloomShader, Program.bloomShaderTextMaskLoc, Program.textMask.texture);
         Raylib.BeginShaderMode(Program.bloomShader);
         {
            unsafe {
               //Image image = Raylib.LoadImageFromTexture(Program.textMask.texture);
               //Image* imagePtr = &image;
               //Raylib.ImageFlipVertical(imagePtr);
               //Texture texture = Raylib.LoadTextureFromImage(*imagePtr);
               //Raylib.DrawTextureRec(Program.textMask.texture,
               //                      new Rectangle(0, 0, Program.textMask.texture.width, -Program.textMask.texture.height),
               //                      new Vector2(0, 0),
               //                      Raylib.WHITE);

               Raylib.DrawTexture(Program.textMask.texture, 0, 0, Raylib.WHITE);
            }
         }
         Raylib.EndShaderMode();

         ////////////////////////////////
         // Screen space rendering ie. UI
         ////////////////////////////////
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

         try {
            Program.config.Deserialize(Program.GetConfigPath());
         }
         catch (InvalidOperationException) {
            Console.WriteLine("Your config file was corrupt. Reverting it to previous settings now.");
            Program.config.Serialize(Program.GetConfigPath());
         }

         Program.font = Program.LoadFontWithAllUnicodeCharacters(Program.GetFontFilePath(), Program.config.fontSize);

         cursor.position = lastKnownCursorPosition ?? new Int2(0, 0);
      }
   }
}
