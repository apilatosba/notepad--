﻿//#define VISUAL_STUDIO
using Raylib_CsLo;
using System.Diagnostics;
using System.IO;
using System.Numerics;

namespace Notepad___Raylib {
   internal class EditorStatePaused : IEditorState {
      Rectangle window;
      Color windowColor = new Color(51, 51, 51, 255);
      IEditorState previousState;

      public EditorStatePaused() {
      }

      public void HandleInput() {
         if (Program.ShouldAcceptKeyboardInput(out _, out KeyboardKey specialKey)) {
            switch (specialKey) {
               case KeyboardKey.KEY_ESCAPE:
                  IEditorState.SetStateTo(previousState);
                  break;
            }
         }
      }

      public void PostHandleInput() {
         window = new Rectangle(Raylib.GetScreenWidth() / 4, 0, Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight());
      }

      public void Render() {
         previousState.Render();

         Raylib.DrawRectangleRec(window, windowColor);

         Int2 centerOfWindow = new Int2((int)(window.x + window.width / 2), (int)(window.y + window.height / 2));
         Rectangle settings = new Rectangle(centerOfWindow.x - window.width / 7, centerOfWindow.y - window.height / 4, 2 * window.width / 7, 2 * window.height / 13);
         //Raylib.DrawRectangleRec(settings, Raylib.RED);
         if (RayGui.GuiButton(settings, "Edit Settings")) {
            Process.Start(new ProcessStartInfo(Path.Combine(Program.GetExecutableDirectory(), "notepad--"), $"\"{Program.GetConfigPath()}\"") {
               UseShellExecute = true,
               CreateNoWindow = true,
#if VISUAL_STUDIO
#else
               WorkingDirectory = Program.GetExecutableDirectory()
#endif
            });
         }

         if (previousState is EditorStatePlaying) {
            Rectangle openDirectoryRect = new Rectangle(centerOfWindow.x - window.width / 7, centerOfWindow.y - window.height / 13, 2 * window.width / 7, 2 * window.height / 13);

            if (RayGui.GuiButton(openDirectoryRect, "Open Containing Folder")) {
               EditorStatePlaying.lastKnownCursorPosition = null;
               EditorStatePlaying.lastKnownCameraTarget = null;

               Program.directoryPath = Path.GetDirectoryName(Program.filePath);
               IEditorState.SetStateTo(new EditorStateDirectoryView());
            }
         }

         Rectangle minimizeWindowRect = new Rectangle(centerOfWindow.x - window.width / 7, centerOfWindow.y + window.height / 13, 2 * window.width / 7, 2 * window.height / 13);

         if (RayGui.GuiButton(minimizeWindowRect, "Minimize Window")) {
            //Activator.CreateInstance(previousState.GetType());

            //Type previousStateType = previousState.GetType();
            //IEditorState.SetStateTo(previousState.GetType().GetConstructor(System.Type.EmptyTypes).Invoke(null) as IEditorState);

            //IEditorState.SetStateTo(previousState);
            Raylib.MinimizeWindow();
         }

         Rectangle quit = new Rectangle(centerOfWindow.x - window.width / 7, centerOfWindow.y + window.height / 4, 2 * window.width / 7, 2 * window.height / 13);

         if (RayGui.GuiButton(quit, "Quit")) {
            Program.isQuitButtonPressed = true;
         }

         {
            float rectWidth = window.width / 10;
            Rectangle repoRect = new Rectangle(window.x + window.width / 15, window.y + window.height - window.width / 9, rectWidth, rectWidth);

            if (RayGui.GuiButton(repoRect, (string)null)) {
               Raylib.OpenURL("https://github.com/apilatosba/notepad--");
            }

            {
               float scale = rectWidth / Program.repoIcon.width;

               Raylib.DrawTextureEx(Program.repoIcon, new Vector2(repoRect.x, repoRect.y), 0, scale, Raylib.WHITE);
            }
         }

         Program.DrawBorderIfNotMaximized(Program.config.borderColor, 1);
      }

      public void Update() {
         HandleInput();
         PostHandleInput();
         Render();
      }

      public void EnterState(IEditorState previousState) {
         this.previousState = previousState;
      }

      public void ExitState(IEditorState _) {
      }
   }
}
