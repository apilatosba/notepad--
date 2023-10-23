#define VISUAL_STUDIO
using Raylib_CsLo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;

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
      ColorInt directoryColor;
      ColorInt fileColor;
      Stopwatch lastKeyboardInputTimer = new Stopwatch();
      Stopwatch windowResizeTimer = new Stopwatch();

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

         MoveCursorToPreviousDirectory(previousDirectoryPath);
         MoveCursorToLastEditedFile(theFileUserWasEditing);

         cursor ??= new Cursor();

         if(windowCoverImage == null)
            windowCoverImage = Raylib.GenImageColor(Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), Raylib.WHITE);
         if(windowCoverTexture == null)
            windowCoverTexture = Raylib.LoadTextureFromImage((Image)windowCoverImage);
         if(highlightedLineRenderTexture == null)
            highlightedLineRenderTexture = Raylib.LoadRenderTexture(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

         directoryColor = Program.config.textColor - new ColorInt(40, 20, 0, 0);
         fileColor = Program.config.textColor + new ColorInt(-10, 20, -10, 0);

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
            if (specialKey != KeyboardKey.KEY_NULL) {
#if VISUAL_STUDIO
               Console.WriteLine(specialKey);
#endif
               lastKeyboardInputTimer.Restart();

               switch (specialKey) {
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
                        Program.filePath = pressedLineValue;
                        Program.lines = Program.ReadLinesFromFile(Program.filePath);
                        IEditorState.SetStateTo(new EditorStatePlaying());
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
                                             modifiers.Contains(KeyboardKey.KEY_LEFT_CONTROL) || modifiers.Contains(KeyboardKey.KEY_RIGHT_CONTROL));
         }

         Program.HandleMouseWheelInput(Raylib.GetMouseWheelMove(), null, modifiers, ref camera);
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
               //Program.RenderLines(lines, Program.font);
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

               Raylib.SetShaderValueTexture(Program.rainbowShader,
                                            Program.rainbowShaderHighlightedLineMaskLoc,
                                            ((RenderTexture)highlightedLineRenderTexture).texture);

               // Dispatch
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

            Raylib.DrawRectangleGradientH(0, Line.Height, Raylib.GetScreenWidth() / 2, 1, Raylib.BLANK, Program.config.textColor);
            Raylib.DrawRectangleGradientH(Raylib.GetScreenWidth() / 2, Line.Height, Raylib.GetScreenWidth() / 2, 1, Raylib.WHITE, Program.config.textColor);
            //Raylib.DrawLineEx(new Vector2(0, Line.Height),
            //                  new Vector2(Raylib.GetScreenWidth(), Line.Height),
            //                  2,
            //                  Program.config.textColor);

            Raylib.DrawTextEx(Program.font,
                              currentDirectory,
                              centeredPosition,
                              Program.config.fontSize,
                              0,
                              Program.config.textColor);
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

      void MoveCursorToPreviousDirectory(string previousDirectoryPath) {
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

      void MoveCursorToLastEditedFile(string fileName) {
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
            text = $"{sizeInBytes / 1024}KB";
         } else if (sizeInBytes < 1024 * 1024 * 1024) {
            text = $"{sizeInBytes / 1024 / 1024}MB";
         } else {
            text = $"{sizeInBytes / 1024 / 1024 / 1024}GB";
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
   }
}
