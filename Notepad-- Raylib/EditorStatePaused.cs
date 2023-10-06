//#define VISUAL_STUDIO
using Raylib_CsLo;
using System.Diagnostics;
using System.IO;

namespace Notepad___Raylib {
   internal class EditorStatePaused : IEditorState {
      Rectangle window;
      Color windowColor = new Color(51, 51, 51, 255);
      EditorStatePlaying editorStatePlaying = new EditorStatePlaying();

      public EditorStatePaused() {
      }

      public void HandleInput() {
         if (Program.ShouldAcceptKeyboardInput(out _, out KeyboardKey specialKey)) {
            switch (specialKey) {
               case KeyboardKey.KEY_ESCAPE:
                  SetStateTo(new EditorStatePlaying());
                  break;
            }
         }
      }

      public void PostHandleInput() {
         window = new Rectangle(Raylib.GetScreenWidth() / 4, 0, Raylib.GetScreenWidth() / 2, Raylib.GetScreenHeight());
      }

      public void Render() {
         editorStatePlaying.Render();

         Raylib.DrawRectangleRec(window, windowColor);

         Int2 centerOfWindow = new Int2((int)(window.x + window.width / 2), (int)(window.y + window.height / 2));
         Rectangle settings = new Rectangle(centerOfWindow.x - window.width / 7, centerOfWindow.y - window.height / 4, 2 * window.width / 7, 2 * window.height / 13);
         //Raylib.DrawRectangleRec(settings, Raylib.RED);
         if (RayGui.GuiButton(settings, "Edit Settings")) {
            Process.Start(new ProcessStartInfo("notepad--", Program.CONFIG_FILE_NAME) {
               UseShellExecute = true,
               CreateNoWindow = true,
#if VISUAL_STUDIO
#else
               WorkingDirectory = Program.GetExecutableDirectory()
#endif
            });
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

      }
   }
}
