namespace JetBlack.Promises
{
    /// <summary>
    /// Interface for a promise that can be rejected or resolved.
    /// </summary>
    public interface IPendingPromise : IRejectable
    {
        /// <summary>
        /// Resolve the promise with a particular value.
        /// </summary>
        void Resolve();
    }

    /// <summary>
    /// Interface for a promise that can be rejected or resolved.
    /// </summary>
    public interface IPendingPromise<in T> : IRejectable
    {
        /// <summary>
        /// Resolve the promise with a particular value.
        /// </summary>
        void Resolve(T value);
    }
}
