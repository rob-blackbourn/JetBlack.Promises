using System;
using System.Collections.Generic;

namespace JetBlack.Promises
{
    internal class Handlers
    {
        private readonly IList<AsyncHandler> _resolvers = new List<AsyncHandler>();
        private readonly IList<AsyncHandler<Exception>> _rejectors = new List<AsyncHandler<Exception>>();

        public void AddResolver(Action onResolved, IRejectable rejectable)
        {
            _resolvers.Add(new AsyncHandler { Callback = onResolved, Rejectable = rejectable });
        }

        public void AddRejector(Action<Exception> onRejected, IRejectable rejectable)
        {
            _rejectors.Add(new AsyncHandler<Exception> { Callback = onRejected, Rejectable = rejectable });
        }

        public void Resolve()
        {
            _resolvers.ForEach(handler => handler.Callback.TryCatch(handler.Rejectable.Reject));
            Clear();
        }

        public void Reject(Exception error)
        {
            _rejectors.ForEach(handler => handler.Callback.TryCatch(error, handler.Rejectable.Reject));
            Clear();
        }

        private void Clear()
        {
            _rejectors.Clear();
            _resolvers.Clear();
        }
    }

    internal class Handlers<T>
    {
        private readonly IList<AsyncHandler<T>> _resolvers = new List<AsyncHandler<T>>();
        private readonly IList<AsyncHandler<Exception>> _rejectors = new List<AsyncHandler<Exception>>();

        public void AddResolvers(Action<T> onResolved, IRejectable rejectable)
        {
            _resolvers.Add(new AsyncHandler<T> { Callback = onResolved, Rejectable = rejectable });
        }

        public void AddRejectors(Action<Exception> onRejected, IRejectable rejectable)
        {
            _rejectors.Add(new AsyncHandler<Exception> { Callback = onRejected, Rejectable = rejectable });
        }

        public void Resolve(T value)
        {
            _resolvers.ForEach(resolveHandler => resolveHandler.Callback.TryCatch(value, resolveHandler.Rejectable.Reject));
            Clear();
        }

        public void Reject(Exception error)
        {
            _rejectors.ForEach(handler => handler.Callback.TryCatch(error, handler.Rejectable.Reject));
            Clear();
        }

        private void Clear()
        {
            _rejectors.Clear();
            _resolvers.Clear();
        }
    }
}
