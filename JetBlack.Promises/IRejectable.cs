using System;

namespace JetBlack.Promises
{
    public interface IRejectable
    {
        void Reject(Exception error);
    }
}
