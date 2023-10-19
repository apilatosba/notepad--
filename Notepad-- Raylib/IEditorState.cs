using Raylib_CsLo;

namespace Notepad___Raylib {
   internal interface IEditorState {
      protected void HandleInput();
      protected void PostHandleInput();
      protected internal void Render();
      void Update();
      static void SetStateTo(IEditorState state) {
         IEditorState previousState = Program.editorState;
         Program.editorState = state;
         state.EnterState(previousState);
      }
      protected internal void EnterState(IEditorState previousState);
   }
}
