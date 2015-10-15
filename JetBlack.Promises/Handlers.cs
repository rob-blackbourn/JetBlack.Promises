using System;
using System.Collections.Generic;

namespace JetBlack.Promises
{
    internal class Handlers
    {
        private readonly IList<RejectHandler> _resolvers = new List<RejectHandler>();
        private readonly IList<RejectHandler<Exception>> _rejectors = new List<RejectHandler<Exception>>();

        public void AddResolver(Action onResolved, IRejectable rejectable)
        {
            _resolvers.Add(new RejectHandler { Callback = onResolved, Rejectable = rejectable });
        }

        public void AddRejector(Action<Exception> onRejected, IRejectable rejectable)
        {
            _rejectors.Add(new RejectHandler<Exception> { Callback = onRejected, Rejectable = rejectable });
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
        private readonly IList<RejectHandler<T>> _resolvers = new List<RejectHandler<T>>();
        private readonly IList<RejectHandler<Exception>> _rejectors = new List<RejectHandler<Exception>>();

        public void AddResolvers(Action<T> onResolved, IRejectable rejectable)
        {
            _resolvers.Add(new RejectHandler<T> { Callback = onResolved, Rejectable = rejectable });
        }

        public void AddRejectors(Action<Exception> onRejected, IRejectable rejectable)
        {
            _rejectors.Add(new RejectHandler<Exception> { Callback = onRejected, Rejectable = rejectable });
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
