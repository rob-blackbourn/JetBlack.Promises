using System;

namespace JetBlack.Promises
{
    internal struct AsyncHandler
    {
        public Action Callback { get; set; }
        public IRejectable Rejectable { get; set; }
    }

    internal struct AsyncHandler<T>
    {
        public Action<T> Callback { get; set; }
        public IRejectable Rejectable { get; set; }
    }
}
