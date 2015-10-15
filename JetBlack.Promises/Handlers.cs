using System;
using System.Collections.Generic;

namespace JetBlack.Promises
{
    internal class Handlers
    {
        private readonly IList<RejectHandler<Exception>> _rejectHandlers = new List<RejectHandler<Exception>>();
        private readonly IList<RejectHandler> _resolveHandlers = new List<RejectHandler>();

        public void AddRejectHandler(Action<Exception> onRejected, IRejectable rejectable)
        {
            _rejectHandlers.Add(new RejectHandler<Exception> { Callback = onRejected, Rejectable = rejectable });
        }

        public void AddResolveHandler(Action onResolved, IRejectable rejectable)
        {
            _resolveHandlers.Add(new RejectHandler { Callback = onResolved, Rejectable = rejectable });
        }

        private void Clear()
        {
            _rejectHandlers.Clear();
            _resolveHandlers.Clear();
        }

        public void Reject(Exception error)
        {
            _rejectHandlers.ForEach(handler => handler.Callback.TryCatch(error, handler.Rejectable.Reject));
            Clear();
        }

        public void Resolve()
        {
            _resolveHandlers.ForEach(handler => handler.Callback.TryCatch(handler.Rejectable.Reject));
            Clear();
        }
    }

    internal class Handlers<T>
    {
        private readonly IList<RejectHandler<Exception>> _rejectHandlers = new List<RejectHandler<Exception>>();
        private readonly IList<RejectHandler<T>> _resolveHandlers = new List<RejectHandler<T>>();

        public void AddRejectHandler(Action<Exception> onRejected, IRejectable rejectable)
        {
            _rejectHandlers.Add(new RejectHandler<Exception> { Callback = onRejected, Rejectable = rejectable });
        }

        public void AddResolveHandler(Action<T> onResolved, IRejectable rejectable)
        {
            _resolveHandlers.Add(new RejectHandler<T> { Callback = onResolved, Rejectable = rejectable });
        }

        private void Clear()
        {
            _rejectHandlers.Clear();
            _resolveHandlers.Clear();
        }

        public void Reject(Exception error)
        {
            _rejectHandlers.ForEach(handler => handler.Callback.TryCatch(error, handler.Rejectable.Reject));
            Clear();
        }

        public void Resolve(T value)
        {
            _resolveHandlers.ForEach(resolveHandler => resolveHandler.Callback.TryCatch(value, resolveHandler.Rejectable.Reject));
            Clear();
        }
    }
}
