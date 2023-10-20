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
      Cursor cursor; // I have this cursor to use HandleArrowKeysNavigation method. I wont be rendering it.
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

      public void EnterState(IEditorState previousState) {
         directories.Clear();
         lines.Clear();

         if(previousState is EditorStateDirectoryView)
            Directory.SetCurrentDirectory(Program.directoryPath);

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
         }

         cursor = new Cursor();
         windowCoverImage = Raylib.GenImageColor(Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), Raylib.WHITE);
         windowCoverTexture = Raylib.LoadTextureFromImage(windowCoverImage);
         highlightedLineRenderTexture = Raylib.LoadRenderTexture(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
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
                     string pressedLineValue = lines[cursor.position.y].Value;

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
            Raylib.BeginMode2D(camera);
            {
               Program.HighlightLineCursorIsAt(cursor);
               Program.RenderLines(lines, Program.font);
            }
            Raylib.EndMode2D();
         }

         {
            Raylib.BeginTextureMode(highlightedLineRenderTexture);
            {
               Raylib.BeginMode2D(camera);

               Raylib.ClearBackground(Raylib.BLANK);
               Raylib.DrawTextEx(Program.font,
                                 lines[cursor.position.y].Value,
                                 new Vector2(Program.config.leftPadding, cursor.position.y * Line.Height),
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
   }
}
