using System;

namespace JetBlack.Promises
{
    public struct RejectHandler
    {
        public Action Callback { get; set; }
        public IRejectable Rejectable { get; set; }
    }

    public struct RejectHandler<T>
    {
        public Action<T> Callback { get; set; }
        public IRejectable Rejectable { get; set; }
    }
}
