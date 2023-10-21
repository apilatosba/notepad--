﻿#define VISUAL_STUDIO
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

      Stopwatch timeSinceLastMouseInput = new Stopwatch();
      Image windowCoverImage;
      Texture windowCoverTexture;
      RenderTexture highlightedLineRenderTexture;
      ColorInt directoryColor;
      ColorInt fileColor;

      public void EnterState(IEditorState previousState) {
         Program.YMargin = Line.Height;

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

            for(int i = directories.Count; i < lines.Count; i++) {
               FileInfo fileInfo = new FileInfo(lines[i].Value);
               string fileSizeText = GetFileSizeText(fileInfo.Length);
               lines[i].Value = lines[i].Value.PadRight(lengthOfMostLongLine + 1);
               lines[i].InsertTextAt(lines[i].Value.Length, fileSizeText);
            }
         }

         MoveCursorToPreviousDirectory(previousDirectoryPath);
         MoveCursorToLastEditedFile(theFileUserWasEditing);

         cursor ??= new Cursor();
         windowCoverImage = Raylib.GenImageColor(Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), Raylib.WHITE);
         windowCoverTexture = Raylib.LoadTextureFromImage(windowCoverImage);
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
               switch (specialKey) {
                  case KeyboardKey.KEY_KP_ENTER:
                  case KeyboardKey.KEY_ENTER:
                     bool isFile;
                     string pressedLineValue/* = lines[cursor.position.y].Value*/;

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
                        isFile = false; // To be able to compile
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

                     // todo
                     // open file if it is a file
                     // open directory if it is a directory
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

         Program.HandleMouseWheelInput(Raylib.GetMouseWheelMove(), timeSinceLastMouseInput, modifiers, ref camera);
      }

      public void PostHandleInput() {
         Program.MakeSureCameraNotBelowZeroInBothAxes(ref camera);
         Program.ClampCameraToText(lines, ref camera);

         if (Raylib.IsWindowResized()) {
            Raylib.UnloadImage(windowCoverImage);
            Raylib.UnloadTexture(windowCoverTexture);
            Raylib.UnloadRenderTexture(highlightedLineRenderTexture);

            windowCoverImage = Raylib.GenImageColor(Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), Raylib.WHITE);
            windowCoverTexture = Raylib.LoadTextureFromImage(windowCoverImage);
            highlightedLineRenderTexture = Raylib.LoadRenderTexture(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
         }
      }

      public void Render() {
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
               Program.RenderLines(lines.GetRange(0, directories.Count), Program.font, (Color)directoryColor, Program.YMargin);
               Program.RenderLines(lines.GetRange(directories.Count, files.Count), Program.font, (Color)fileColor, Line.Height * directories.Count + Program.YMargin);
            }
            Raylib.EndMode2D();

            Raylib.EndScissorMode();
         }

         {
            Raylib.BeginTextureMode(highlightedLineRenderTexture);
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
                                            highlightedLineRenderTexture.texture);

               Raylib.DrawTextureRec(windowCoverTexture,
                                     new Rectangle(0, 0, windowCoverTexture.width, -windowCoverTexture.height),
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
