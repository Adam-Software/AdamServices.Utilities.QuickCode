using System;
using System.Collections.Generic;

namespace QuickCode.Services.Interfaces.RemotePythonRunnerServiceDependency.CustomCollection
{
    public class EventQueue<T>
    {
        private readonly Queue<T> queue = new();
        public event EventHandler Enqueued;

        protected virtual void OnEnqueued()
        {
            Enqueued?.Invoke(this, EventArgs.Empty);
        }

        public virtual void Enqueue(T item)
        {
            queue.Enqueue(item);
            OnEnqueued();
        }

        public int Count
        {
            get
            {
                return queue.Count;
            }
        }

        public void Clear()
        {
            queue.Clear();
        }

        public virtual T Dequeue()
        {
            T item = queue.Dequeue();
            //OnEnqueued();
            return item;
        }
    }
}
