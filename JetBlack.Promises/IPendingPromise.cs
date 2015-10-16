namespace JetBlack.Promises
{
    /// <summary>
    /// A promise that can be rejected or resolved.
    /// </summary>
    public interface IPendingPromise : IRejectable
    {
        /// <summary>
        /// Resolve the promise.
        /// </summary>
        void Resolve();
    }

    /// <summary>
    /// A promise that can be rejected or resolved.
    /// </summary>
    public interface IPendingPromise<in T> : IRejectable
    {
        /// <summary>
        /// Resolve the promise with a particular value.
        /// </summary>
        /// <param name="value">The value with which to resolve the promise.</param>
        void Resolve(T value);
    }
}
