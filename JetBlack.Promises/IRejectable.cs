using System;

namespace JetBlack.Promises
{
    /// <summary>
    /// Something that can be rejected.
    /// </summary>
    public interface IRejectable
    {
        /// <summary>
        /// Reject with the given error.
        /// </summary>
        /// <param name="error">The error which explains the reason for the rejection.</param>
        void Reject(Exception error);
    }
}
