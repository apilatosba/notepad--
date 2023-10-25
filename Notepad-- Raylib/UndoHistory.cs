using System;
using System.Collections.Generic;

namespace Notepad___Raylib {
   internal class UndoHistory<T> {
      LinkedList<T> undoStack;
      readonly int MAX_SIZE;

      public UndoHistory(int maxSize = 128) {
         MAX_SIZE = maxSize;
         undoStack = new LinkedList<T>();
      }

      public void Push(T item) {
         if(undoStack.Count == MAX_SIZE) 
            Dequeue();
         undoStack.AddLast(item);
      }

      /// <exception cref="InvalidOperationException">If stack is empty</exception>
      public T Pop() {
         if (undoStack.Count == 0) throw new InvalidOperationException("Undo stack is empty");
         T item = undoStack.Last.Value;
         undoStack.RemoveLast();
         return item;
      }

      /// <exception cref="InvalidOperationException">If stack is empty</exception>
      public T Dequeue() {
         if (undoStack.Count == 0) throw new InvalidOperationException("Undo stack is empty");
         T item = undoStack.First.Value;
         undoStack.RemoveFirst();
         return item;
      }
   }
}
