#define VISUAL_STUDIO
using Raylib_CsLo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Notepad___Raylib {
   internal class EditorStateDirectoryView : IEditorState {
      List<string> directories = new List<string>();
      List<string> files;
      List<Line> lines = new List<Line>();
      internal Cursor cursor; // I have this cursor to use HandleArrowKeysNavigation method. I wont be rendering it.
      Camera2D camera = new Camera2D() {
         zoom = 1.0f,
         target = new Vector2(0, 0),
         rotation = 0.0f,
         offset = new Vector2(0, 0),
      };

      Image? windowCoverImage; // IsImageReady() function doesnt exist in this nuget package so i manually set it to null 
      Texture? windowCoverTexture; // IsTextureReady() function doesnt exist in this nuget package so i manually set it to null
      RenderTexture? highlightedLineRenderTexture; // IsRenderTextureReady() function doesnt exist in this nuget package so i manually set it to null
      bool isHighlightedLineRenderTextureChanged;
      ColorInt directoryColor;
      ColorInt fileColor;
      readonly Stopwatch lastKeyboardInputTimer = new Stopwatch();
      readonly Stopwatch windowResizeTimer = new Stopwatch();
      readonly Stopwatch controlFHighlightMatchTimer = new Stopwatch();
      readonly Stopwatch keyPressHighlightMatchTimer = new Stopwatch();
      InternalState internalState = InternalState.Normal;
      List<ControlFMatchLine> controlFMatches = new List<ControlFMatchLine>();
      ControlFMatch currentControlFMatch;
      string controlFBuffer = "";
      string submittedControlFBuffer = "";
      Rectangle controlFHighlightMatchRect;
      Rectangle keyPressHighlightMatchRect;
      int totalControlFMatches;
      char lastPressedChar = '\0';
      List<int> keyPressMatches = new List<int>();

      public void EnterState(IEditorState previousState) {
         Program.YMargin = Line.Height;
         lastKeyboardInputTimer.Start();
         windowResizeTimer.Start();

         directories.Clear();
         lines.Clear();
         string previousDirectoryPath = null;
         string theFileUserWasEditing = null;

         if (previousState is EditorStateDirectoryView) {
            previousDirectoryPath = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Program.directoryPath);
         } else if (previousState is EditorStatePaused) {
            theFileUserWasEditing = Program.filePath;
            Program.undoHistory.Clear();
            Program.redoHistory.Clear();
         } else if (previousState is EditorStatePlaying) {
            theFileUserWasEditing = Program.filePath;
            Program.undoHistory.Clear();
            Program.redoHistory.Clear();
         }

         directories.Add("..");
         directories.AddRange(Directory.GetDirectories(".").ToList());
         files = Directory.GetFiles(".").ToList();

         {
            foreach (string directory in directories) {
               lines.Add(new Line(directory));
            }

            foreach (string file in files) {
               lines.Add(new Line(file));
            }

            foreach (Line line in lines) {
               if (line.Value.StartsWith($".{Path.DirectorySeparatorChar}"))
                  line.RemoveTextAt(0, 2, Direction.Right);
            }

            int lengthOfMostLongLine = FindLengthOfMostLongLine(files);

            for (int i = directories.Count; i < lines.Count; i++) {
               FileInfo fileInfo = new FileInfo(lines[i].Value);
               string fileSizeText = GetFileSizeText(fileInfo.Length);
               lines[i].Value = lines[i].Value.PadRight(lengthOfMostLongLine + 1);
               lines[i].InsertTextAt(lines[i].Value.Length, fileSizeText);
            }
         }

         MoveCursorToPreviousDirectoryIfPreviousDirectoryExists(previousDirectoryPath);
         MoveCursorToLastEditedFileIfPreviousFileExists(theFileUserWasEditing);

         cursor ??= new Cursor();

         windowCoverImage ??= Raylib.GenImageColor(Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), Raylib.WHITE);
         windowCoverTexture ??= Raylib.LoadTextureFromImage((Image)windowCoverImage);
         highlightedLineRenderTexture ??= Raylib.LoadRenderTexture(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
         isHighlightedLineRenderTextureChanged = true; // Safe to assume that it is changed since it is called once but not every frame.

         directoryColor = Program.config.textColor - new ColorInt(40, 20, 20, 0);
         fileColor = Program.config.textColor + new ColorInt(-10, 30, -10, 0);

         directoryColor.Clamp0To255();
         fileColor.Clamp0To255();
      }

      public void HandleInput() {
         List<KeyboardKey> modifiers = new List<KeyboardKey>();
         if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL)) modifiers.Add(KeyboardKey.KEY_LEFT_CONTROL);
         if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT)) modifiers.Add(KeyboardKey.KEY_LEFT_SHIFT);
         if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_ALT)) modifiers.Add(KeyboardKey.KEY_LEFT_ALT);
         if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SUPER)) modifiers.Add(KeyboardKey.KEY_LEFT_SUPER);
         if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL)) modifiers.Add(KeyboardKey.KEY_RIGHT_CONTROL);
         if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SHIFT)) modifiers.Add(KeyboardKey.KEY_RIGHT_SHIFT);
         if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_ALT)) modifiers.Add(KeyboardKey.KEY_RIGHT_ALT);
         if (Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SUPER)) modifiers.Add(KeyboardKey.KEY_RIGHT_SUPER);


         if (Program.ShouldAcceptKeyboardInput(out string pressedKeys, out KeyboardKey specialKey)) {

            switch (internalState) {
               case InternalState.Normal:

                  if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL)) {
                     if (Raylib.IsKeyPressed(KeyboardKey.KEY_KP_ADD)) {
                        Program.config.fontSize++;

                        Program.config.Serialize(Program.GetConfigPath());
                        Raylib.UnloadFont(Program.font);
                        Program.font = Program.LoadFontWithAllUnicodeCharacters(Program.GetFontFilePath(), Program.config.fontSize);

                        Program.YMargin = Line.Height;
                     }

                     if (Raylib.IsKeyPressed(KeyboardKey.KEY_KP_SUBTRACT)) {
                        Program.config.fontSize--;

                        Program.config.Serialize(Program.GetConfigPath());
                        Raylib.UnloadFont(Program.font);
                        Program.font = Program.LoadFontWithAllUnicodeCharacters(Program.GetFontFilePath(), Program.config.fontSize);

                        Program.YMargin = Line.Height;
                     }

                     if (Raylib.IsKeyPressed(KeyboardKey.KEY_F)) {
                        internalState = InternalState.ControlF;
                     }

                     if (Raylib.IsKeyPressed(KeyboardKey.KEY_ENTER) || Raylib.IsKeyPressed(KeyboardKey.KEY_KP_ENTER)) {
                        try {
                           Program.OpenDirectoryOrFileInDefaultProgram(Directory.GetCurrentDirectory());
                           Raylib.MinimizeWindow();

                           break;
                        }
                        catch (Exception e) {
                           // todo some effect. screen shake? an info popup box
#if VISUAL_STUDIO
                           Console.WriteLine($"ERROR: Couldn't open directory. Exception message: {e.Message}");
#endif
                        }
                     }
                  }

                  if (pressedKeys != null) {
#if VISUAL_STUDIO
                     Program.PrintPressedKeys(pressedKeys);
#endif
                     foreach (char c in pressedKeys) {
                        if (c.ToString().ToLower()[0] == lastPressedChar.ToString().ToLower()[0]) {
                           int currentMatchIndex = keyPressMatches.IndexOf(cursor.position.y);

                           if (modifiers.Contains(KeyboardKey.KEY_LEFT_SHIFT) || modifiers.Contains(KeyboardKey.KEY_RIGHT_SHIFT)) {
                              if (currentMatchIndex == -1) {
                                 cursor.position.y = keyPressMatches[0];

                                 keyPressHighlightMatchTimer.Restart();

                                 keyPressHighlightMatchRect = new Rectangle(0,
                                                                            cursor.position.y * Line.Height + Program.YMargin,
                                                                            float.MaxValue,
                                                                            Line.Height);
                              } else {
                                 try {
                                    cursor.position.y = keyPressMatches[currentMatchIndex - 1];

                                    keyPressHighlightMatchTimer.Restart();

                                    keyPressHighlightMatchRect = new Rectangle(0,
                                                                               cursor.position.y * Line.Height + Program.YMargin,
                                                                               float.MaxValue,
                                                                               Line.Height);
                                 }
                                 catch (ArgumentOutOfRangeException) {
                                    try {
                                       cursor.position.y = keyPressMatches[keyPressMatches.Count - 1];

                                       keyPressHighlightMatchTimer.Restart();

                                       keyPressHighlightMatchRect = new Rectangle(0,
                                                                                  cursor.position.y * Line.Height + Program.YMargin,
                                                                                  float.MaxValue,
                                                                                  Line.Height);
                                    }
                                    catch (ArgumentOutOfRangeException) {
                                       // user keep pressing the same key but there is no match
                                    }
                                 }
                              }
                           } else {
                              if (currentMatchIndex == -1) {
                                 // reset back to the first match if there is a match
                                 // actually and luckily i dont need to do anything here. because the math below will do the job (-1 + 1 is zero which corresponds to first element)
                              }

                              try {
                                 cursor.position.y = keyPressMatches[currentMatchIndex + 1];

                                 keyPressHighlightMatchTimer.Restart();

                                 keyPressHighlightMatchRect = new Rectangle(0,
                                                                            cursor.position.y * Line.Height + Program.YMargin,
                                                                            float.MaxValue,
                                                                            Line.Height);
                              }
                              catch (ArgumentOutOfRangeException) {
                                 try {
                                    cursor.position.y = keyPressMatches[0];

                                    keyPressHighlightMatchTimer.Restart();

                                    keyPressHighlightMatchRect = new Rectangle(0,
                                                                               cursor.position.y * Line.Height + Program.YMargin,
                                                                               float.MaxValue,
                                                                               Line.Height);
                                 }
                                 catch (ArgumentOutOfRangeException) {
                                    // user keep pressing the same key but there is no match
                                 }
                              }
                           }
                        } else {
                           keyPressMatches.Clear();

                           for (int i = 0; i < lines.Count; i++) {
                              int[] indices = lines[i].Find(new Regex($"^{Regex.Escape(c.ToString())}", RegexOptions.IgnoreCase));

                              if (indices.Length > 0) {
                                 keyPressMatches.Add(i);
                              }
                           }

                           if (keyPressMatches.Count > 0) {
                              cursor.position.y = keyPressMatches[0];

                              keyPressHighlightMatchRect = new Rectangle(0,
                                                                         cursor.position.y * Line.Height + Program.YMargin,
                                                                         float.MaxValue,
                                                                         Line.Height);

                              keyPressHighlightMatchTimer.Restart();
                           } else {
                              // there is no match.
                              // todo shader effect. screen shake?
                           }
                        }

                        lastPressedChar = c;
                     }

                     CameraMoveDirection cameraMoveDirection = cursor.MakeSureCursorIsVisibleToCamera(lines,
                                                                                                      ref camera,
                                                                                                      Program.config.fontSize,
                                                                                                      Program.config.leftPadding,
                                                                                                      Program.font);

                     Program.MoveCameraIfNecessary(ref camera, cameraMoveDirection);
                  }

                  if (specialKey != KeyboardKey.KEY_NULL) {
#if VISUAL_STUDIO
                     Console.WriteLine(specialKey);
#endif
                     lastKeyboardInputTimer.Restart();

                     switch (specialKey) {
                        case KeyboardKey.KEY_HOME:
                           cursor.position.y = 0;

                           cursor.MakeSureCursorIsVisibleToCamera(lines,
                                                                  ref camera,
                                                                  Program.config.fontSize,
                                                                  Program.config.leftPadding,
                                                                  Program.font);

                           break;
                        case KeyboardKey.KEY_END:
                           cursor.position.y = lines.Count - 1;

                           cursor.MakeSureCursorIsVisibleToCamera(lines,
                                                                  ref camera,
                                                                  Program.config.fontSize,
                                                                  Program.config.leftPadding,
                                                                  Program.font);

                           break;
                        case KeyboardKey.KEY_KP_ENTER:
                        case KeyboardKey.KEY_ENTER:
                           bool isFile;
                           string pressedLineValue;

                           if (cursor.position.y < directories.Count) {
                              pressedLineValue = directories[cursor.position.y];
                           } else {
                              pressedLineValue = files[cursor.position.y - directories.Count];
                           }

                           // I can do that since directories listed first.
                           if (cursor.position.y < directories.Count) {
                              isFile = false;
                           } else if (cursor.position.y < lines.Count && cursor.position.y >= directories.Count) {
                              isFile = true;
                           } else {
                              Debug.Assert(false, "out of range");
                              throw new IndexOutOfRangeException();
                           }

                           if (isFile) {
                              if (Program.IsTextFile(pressedLineValue)) {
                                 Program.filePath = pressedLineValue;
                                 Program.lines = Program.ReadLinesFromFile(Program.filePath);
                                 Program.longestLine = Program.FindLongestLine(Program.lines);
                                 IEditorState.SetStateTo(new EditorStatePlaying());
                              } else {
                                 Program.OpenDirectoryOrFileInDefaultProgram(pressedLineValue);
                              }
                           } else {
                              if (CheckIfHasPermissionToOpenDirectory(pressedLineValue)) {
                                 Program.directoryPath = pressedLineValue;
                                 IEditorState.SetStateTo(new EditorStateDirectoryView());
                              } else {
                                 // todo some effect. screen shake? an info popup box
                              }
                           }

                           break;
                        case KeyboardKey.KEY_ESCAPE:
                           IEditorState.SetStateTo(new EditorStatePaused());
                           break;
                        case KeyboardKey.KEY_UP:
                           if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL)) {
                              camera.target.Y -= Line.Height;
                           }
                           break;
                        case KeyboardKey.KEY_DOWN:
                           if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL)) {
                              camera.target.Y += Line.Height;
                           }
                           break;
                     }
                  }

                  cursor.HandleArrowKeysNavigation(lines,
                                                   ref camera,
                                                   Program.config.fontSize,
                                                   Program.config.leftPadding,
                                                   Program.font,
                                                   modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL),
                                                   specialKey);
                  break;

               case InternalState.ControlF:
                  if (modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL)) {
                     if (Raylib.IsKeyPressed(KeyboardKey.KEY_F)) {
                        internalState = InternalState.Normal;
                        break;
                     }
                  }

                  if (pressedKeys != null) {
#if VISUAL_STUDIO
                     Program.PrintPressedKeys($"{pressedKeys} (ctrl+f)");
#endif

                     controlFBuffer += pressedKeys;
                  }

                  if (specialKey != KeyboardKey.KEY_NULL) {
#if VISUAL_STUDIO
                     Console.WriteLine($"{specialKey} (ctrl+f)");
#endif
                     switch (specialKey) {
                        case KeyboardKey.KEY_UP:
                           if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL)) {
                              camera.target.Y -= Line.Height;
                           }
                           break;
                        case KeyboardKey.KEY_DOWN:
                           if (modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL)) {
                              camera.target.Y += Line.Height;
                           }
                           break;
                        case KeyboardKey.KEY_ESCAPE:
                           internalState = InternalState.Normal;
                           break;
                        case KeyboardKey.KEY_BACKSPACE:
                           if (controlFBuffer.Length > 0) {
                              controlFBuffer = controlFBuffer.Remove(controlFBuffer.Length - 1);
                           }
                           break;
                        case KeyboardKey.KEY_TAB:
                        case KeyboardKey.KEY_KP_ENTER:
                        case KeyboardKey.KEY_ENTER:
                           if (controlFBuffer == "") break;

                           if (submittedControlFBuffer == controlFBuffer) {
                              if (modifiers.Contains(KeyboardKey.KEY_LEFT_SHIFT) || modifiers.Contains(KeyboardKey.KEY_RIGHT_SHIFT)) {
                                 DecreaseControlFMatchByOne();
                              } else {
                                 IncreaseControlFMatchByOne();
                              }
                           } else {
                              controlFMatches.Clear();
                              totalControlFMatches = 0;
                              submittedControlFBuffer = controlFBuffer;

                              for (int i = 0; i < lines.Count; i++) {
                                 int[] indices = lines[i].Find(new Regex(controlFBuffer));

                                 if (indices.Length > 0) {
                                    controlFMatches.Add(new ControlFMatchLine(i, indices));
                                    totalControlFMatches += indices.Length;
                                 }
                              }

                              if (controlFMatches.Count > 0)
                                 currentControlFMatch = new ControlFMatch(controlFMatches[0], 0, 0, 0);
                              else
                                 currentControlFMatch = null;
                           }

                           if (currentControlFMatch != null) {
                              cursor.position.y = currentControlFMatch.line.lineNumber;

                              Vector2 highlightedTextLength = Raylib.MeasureTextEx(Program.font, submittedControlFBuffer, Program.config.fontSize, 0);
                              int rectangleStartX = Program.config.leftPadding + (int)Raylib.MeasureTextEx(Program.font,
                                                                                                           lines[currentControlFMatch.line.lineNumber].Value.Substring(0, currentControlFMatch.line.matchIndices[currentControlFMatch.index]),
                                                                                                           Program.config.fontSize,
                                                                                                           0).X;

                              controlFHighlightMatchRect = new Rectangle(rectangleStartX,
                                                                         cursor.position.y * Line.Height + Program.YMargin,
                                                                         highlightedTextLength.X,
                                                                         highlightedTextLength.Y);

                              controlFHighlightMatchTimer.Restart();
                           }

                           CameraMoveDirection cameraMoveDirection = cursor.MakeSureCursorIsVisibleToCamera(lines,
                                                                                                            ref camera,
                                                                                                            Program.config.fontSize,
                                                                                                            Program.config.leftPadding,
                                                                                                            Program.font);

                           Program.MoveCameraIfNecessary(ref camera, cameraMoveDirection);

                           break;
                     }
                  }
                  break;
            }
         }

         if (!Program.isDraggingWindow) {
            if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT)) {
               Vector2 mousePosition = Raylib.GetMousePosition();
               Int2 mousePositionInWorldSpace = (Int2)Raylib.GetScreenToWorld2D(mousePosition, camera);

               cursor.position.y = cursor.CalculatePositionFromWorldSpaceCoordinates(lines,
                                                                                     Program.config.fontSize,
                                                                                     Program.config.leftPadding,
                                                                                     Program.font,
                                                                                     mousePositionInWorldSpace).y;
            }
         }

         Program.HandleMouseWheelInput(Raylib.GetMouseWheelMove(), null, modifiers, ref camera, this);
      }

      public void PostHandleInput() {
         Program.MakeSureCameraNotBelowZeroInBothAxes(ref camera);
         Program.ClampCameraToText(lines, ref camera);

         if (Raylib.IsWindowResized()) {
            Raylib.UnloadImage((Image)windowCoverImage);
            Raylib.UnloadTexture((Texture)windowCoverTexture);
            Raylib.UnloadRenderTexture((RenderTexture)highlightedLineRenderTexture);

            windowCoverImage = Raylib.GenImageColor(Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), Raylib.WHITE);
            windowCoverTexture = Raylib.LoadTextureFromImage((Image)windowCoverImage);
            highlightedLineRenderTexture = Raylib.LoadRenderTexture(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
            isHighlightedLineRenderTextureChanged = true;

            windowResizeTimer.Restart();
         }
      }

      public void Render() {
         if (windowCoverImage == null ||
               windowCoverTexture == null ||
               highlightedLineRenderTexture == null) // All this nonsense happening because IsImageReady(), IsTextureReady() and IsRenderTextureReady() functions dont exist in this nuget package
            return;

         //////////////////////////////////////
         // Screen space rendering (background)
         //////////////////////////////////////
         {
            Program.DrawBackground();
         }

         ////////////////////////
         // World space rendering
         ////////////////////////
         {
            Raylib.BeginScissorMode(0, Line.Height, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

            Raylib.BeginMode2D(camera);
            {
               Program.HighlightLineCursorIsAt(cursor);

               switch (internalState) {
                  case InternalState.Normal:
                     if (keyPressHighlightMatchTimer.ElapsedMilliseconds < 1500) {
                        int alpha = (int)(MathF.Exp(-1 * 6 * (keyPressHighlightMatchTimer.ElapsedMilliseconds / 1000.0f)) * 255);
                        Raylib.DrawRectangleRec(keyPressHighlightMatchRect, new Color(255, 255, 255, alpha));
                     }

                     break;
                  case InternalState.ControlF:
                     for (int i = 0; i < controlFMatches.Count; i++) {
                        int[] matches = controlFMatches[i].matchIndices;

                        for (int j = 0; j < matches.Length; j++) {
                           int match = matches[j];
                           int rectangleStartX = Program.config.leftPadding + (int)Raylib.MeasureTextEx(Program.font, lines[controlFMatches[i].lineNumber].Value.Substring(0, match), Program.config.fontSize, 0).X;
                           int rectangleLength = (int)Raylib.MeasureTextEx(Program.font, submittedControlFBuffer, Program.config.fontSize, 0).X;

                           Raylib.DrawRectangleLines(rectangleStartX, Line.Height * controlFMatches[i].lineNumber + Program.YMargin, rectangleLength, Line.Height, new Color(255, 0, 0, 150));
                        }
                     }

                     if (controlFHighlightMatchTimer.ElapsedMilliseconds < 1500) {
                        int alpha = (int)(MathF.Exp(-1 * 6 * (controlFHighlightMatchTimer.ElapsedMilliseconds / 1000.0f)) * 255);
                        Raylib.DrawRectangleRec(controlFHighlightMatchRect, new Color(255, 255, 255, alpha));
                     }

                     break;
               }

               Program.RenderLines(lines.GetRange(0, directories.Count), Program.font, (Color)directoryColor, Program.YMargin, camera);
               Program.RenderLines(lines.GetRange(directories.Count, files.Count), Program.font, (Color)fileColor, Line.Height * directories.Count + Program.YMargin, camera);
            }
            Raylib.EndMode2D();

            Raylib.EndScissorMode();
         }

         {
            Raylib.BeginTextureMode((RenderTexture)highlightedLineRenderTexture);
            {
               Raylib.BeginMode2D(camera);

               Raylib.ClearBackground(Raylib.BLANK);
               Raylib.DrawTextEx(Program.font,
                                 lines[cursor.position.y].Value,
                                 new Vector2(Program.config.leftPadding, cursor.position.y * Line.Height + Program.YMargin),
                                 Program.config.fontSize,
                                 0,
                                 Raylib.WHITE);

               Raylib.EndMode2D();
            }
            Raylib.EndTextureMode();
         }

         {
            Raylib.BeginShaderMode(Program.rainbowShader);
            {
               Raylib.SetShaderValue(Program.rainbowShader,
                                     Program.rainbowShaderTimeLoc,
                                     (float)Raylib.GetTime(),
                                     ShaderUniformDataType.SHADER_UNIFORM_FLOAT);

               if (isHighlightedLineRenderTextureChanged) {
                  Raylib.SetShaderValueTexture(Program.rainbowShader,
                                               Program.rainbowShaderHighlightedLineMaskLoc,
                                               ((RenderTexture)highlightedLineRenderTexture).texture);
               }
               isHighlightedLineRenderTextureChanged = false;

               // Dispatch. Not quite.
               Raylib.DrawTextureRec((Texture)windowCoverTexture,
                                     new Rectangle(0, 0, ((Texture)windowCoverTexture).width, -((Texture)windowCoverTexture).height),
                                     new Vector2(0, 0),
                                     Raylib.WHITE);
            }
            Raylib.EndShaderMode();
         }

         ////////////////////////////////
         // Screen space rendering ie. UI
         ////////////////////////////////
         {
            string currentDirectory = Directory.GetCurrentDirectory();
            float currentDirectoryTextLength = Raylib.MeasureTextEx(Program.font, currentDirectory, Program.config.fontSize, 0).X;
            Vector2 centeredPosition = new Vector2(Raylib.GetScreenWidth() / 2 - currentDirectoryTextLength / 2, 0);

            Raylib.DrawRectangleGradientH(0, Program.YMargin, Raylib.GetScreenWidth() / 2, 1, Raylib.BLANK, Program.config.textColor);
            Raylib.DrawRectangleGradientH(Raylib.GetScreenWidth() / 2, Program.YMargin, Raylib.GetScreenWidth() / 2, 1, Program.config.textColor, Raylib.BLANK);

            Raylib.DrawTextEx(Program.font,
                              currentDirectory,
                              centeredPosition,
                              Program.config.fontSize,
                              0,
                              Program.config.textColor);

            if (internalState == InternalState.ControlF) {
               int horizontalSpace = 5;
               int verticalSpace = 2;

               Vector2 textPosition = new Vector2(Raylib.GetScreenWidth() - 150, Program.YMargin + 50);
               Int2 textLength = (Int2)Raylib.MeasureTextEx(Program.font, controlFBuffer, Program.config.fontSize, 0);

               string regexLabel = "REGEX";
               Int2 regexLabelLength = (Int2)Raylib.MeasureTextEx(Program.font, regexLabel, Program.config.fontSize, 0);
               Vector2 regexLabelOffset = new Vector2(5, Line.Height);

               string currentMatchIndicatorText = $"{currentControlFMatch?.overallIndex + 1 ?? 0}/{totalControlFMatches}";
               Vector2 currentMatchIndicatorTextOffset = regexLabelOffset + new Vector2(regexLabelLength.x + 2 * horizontalSpace + 1, 0); // Offset is relative to textPosition

               if (textPosition.X + textLength.x + 2 * horizontalSpace > Raylib.GetScreenWidth()) {
                  textPosition.X = Raylib.GetScreenWidth() - textLength.x - 2 * horizontalSpace;
               }

               Rectangle rectangle = Program.GenerateSurroundingRectangle(controlFBuffer, textPosition, Program.font, Program.config.fontSize, horizontalSpace, verticalSpace);
               Rectangle regexLabelRect = Program.GenerateSurroundingRectangle(regexLabel, textPosition + regexLabelOffset, Program.font, Program.config.fontSize, horizontalSpace, verticalSpace);
               Rectangle currentMatchIndicatorTextRect = Program.GenerateSurroundingRectangle(currentMatchIndicatorText, textPosition + currentMatchIndicatorTextOffset, Program.font, Program.config.fontSize, horizontalSpace, verticalSpace);

               Raylib.DrawRectangleRounded(rectangle, 0.5f, 8, new Color(50, 50, 50, 255));
               Raylib.DrawRectangleRounded(regexLabelRect, 0.5f, 8, new Color(50, 50, 50, 255));

               if (totalControlFMatches > 0)
                  Raylib.DrawRectangleRounded(currentMatchIndicatorTextRect, 0.5f, 8, new Color(50, 50, 50, 230));

               Raylib.DrawTextEx(Program.font,
                                 controlFBuffer,
                                 textPosition,
                                 Program.config.fontSize,
                                 0,
                                 Program.config.textColor);

               Raylib.DrawTextEx(Program.font,
                                 regexLabel,
                                 textPosition + regexLabelOffset,
                                 Program.config.fontSize,
                                 0,
                                 Program.config.textColor);

               if (totalControlFMatches > 0) {
                  Raylib.DrawTextEx(Program.font,
                                    currentMatchIndicatorText,
                                    textPosition + currentMatchIndicatorTextOffset,
                                    Program.config.fontSize,
                                    0,
                                    Program.config.textColor);
               }
            }
         }
      }

      public void Update() {
         HandleInput();
         PostHandleInput();
         Render();
      }

      public void ExitState(IEditorState nextState) {
         if (nextState is not EditorStatePaused) {
            Raylib.UnloadImage((Image)windowCoverImage);
            Raylib.UnloadTexture((Texture)windowCoverTexture);
            Raylib.UnloadRenderTexture((RenderTexture)highlightedLineRenderTexture);

            windowCoverImage = null;
            windowCoverTexture = null;
            highlightedLineRenderTexture = null;
         }
      }

      static bool CheckIfHasPermissionToOpenDirectory(string path) {
         try {
            _ = Directory.GetDirectories(path);
         }
         catch (UnauthorizedAccessException) {
            return false;
         }

         return true;
      }

      void MoveCursorToPreviousDirectoryIfPreviousDirectoryExists(string previousDirectoryPath) {
         if (previousDirectoryPath == null) return;

         string cwd = Directory.GetCurrentDirectory();

         for (int i = 0; i < directories.Count; i++) {
            if (Path.GetFullPath(Path.Combine(cwd, directories[i])) == Path.GetFullPath(previousDirectoryPath)) {
               cursor = new Cursor() {
                  position = new Int2(0, i)
               };

               cursor.MakeSureCursorIsVisibleToCamera(lines,
                                                      ref camera,
                                                      Program.config.fontSize,
                                                      Program.config.leftPadding,
                                                      Program.font);
               break;
            }
         }
      }

      void MoveCursorToLastEditedFileIfPreviousFileExists(string fileName) {
         if (fileName == null) return;

         string cwd = Directory.GetCurrentDirectory();

         for (int i = 0; i < files.Count; i++) {
            if (Path.GetFullPath(Path.Combine(cwd, files[i])) == Path.GetFullPath(fileName)) {
               cursor = new Cursor() {
                  position = new Int2(0, i + directories.Count)
               };

               cursor.MakeSureCursorIsVisibleToCamera(lines,
                                                      ref camera,
                                                      Program.config.fontSize,
                                                      Program.config.leftPadding,
                                                      Program.font);

               break;
            }
         }
      }

      static string GetFileSizeText(long sizeInBytes) {
         string text;

         if (sizeInBytes < 1024) {
            text = $"{sizeInBytes}";
         } else if (sizeInBytes < 1024 * 1024) {
            text = $"{(double)sizeInBytes / 1024:n2}KB";
         } else if (sizeInBytes < 1024 * 1024 * 1024) {
            text = $"{(double)sizeInBytes / 1024 / 1024:n2}MB";
         } else {
            text = $"{(double)sizeInBytes / 1024 / 1024 / 1024:n2}GB";
         }

         return text;
      }

      static int FindLengthOfMostLongLine(in List<string> lines) {
         int maxLength = 0;

         foreach (string line in lines) {
            if (line.Length > maxLength) {
               maxLength = line.Length;
            }
         }

         return maxLength;
      }

      void IncreaseControlFMatchByOne() {
         if (currentControlFMatch == null) return;

         ControlFMatchLine line = currentControlFMatch.line;

         if (line.matchIndices.Length > currentControlFMatch.index + 1) {
            currentControlFMatch.index++;
            currentControlFMatch.overallIndex++;
         } else {
            ControlFMatchLine nextLine;
            try {
               nextLine = controlFMatches[currentControlFMatch.indexOfLineInMatchBuffer + 1];
               currentControlFMatch = new ControlFMatch(nextLine, 0, currentControlFMatch.indexOfLineInMatchBuffer + 1, currentControlFMatch.overallIndex + 1);
            }
            catch (ArgumentOutOfRangeException) {
               nextLine = controlFMatches[0];
               currentControlFMatch = new ControlFMatch(nextLine, 0, 0, 0);
            }
         }
      }

      void DecreaseControlFMatchByOne() {
         if (currentControlFMatch == null) return;

         ControlFMatchLine line = currentControlFMatch.line;

         if (currentControlFMatch.index > 0) {
            currentControlFMatch.index--;
            currentControlFMatch.overallIndex--;
         } else {
            ControlFMatchLine previousLine;
            try {
               previousLine = controlFMatches[currentControlFMatch.indexOfLineInMatchBuffer - 1];
               currentControlFMatch = new ControlFMatch(previousLine, previousLine.matchIndices.Length - 1, currentControlFMatch.indexOfLineInMatchBuffer - 1, currentControlFMatch.overallIndex - 1);
            }
            catch (ArgumentOutOfRangeException) {
               previousLine = controlFMatches[controlFMatches.Count - 1];
               currentControlFMatch = new ControlFMatch(previousLine, previousLine.matchIndices.Length - 1, controlFMatches.Count - 1, totalControlFMatches - 1);
            }
         }
      }
   }
}
