using System;
using System.Collections.Generic;

namespace JetBlack.Promises
{
    /// <summary>
    /// Implements a non-generic promise, this is a promise that simply resolves without delivering a value.
    /// </summary>
    public interface IPromise
    {
        /// <summary>
        /// Completes the promise. 
        /// onResolved is called on successful completion.
        /// onRejected is called on error.
        /// </summary>
        void Done(Action onResolved = null, Action<Exception> onRejected = null);

        /// <summary>
        /// Handle errors for the promise. 
        /// </summary>
        IPromise Catch(Action<Exception> onRejected);

        /// <summary>
        /// Add a resolved callback and a rejected callback.
        /// </summary>
        IPromise Then(Action onResolved, Action<Exception> onRejected = null);

        /// <summary>
        /// Add a resolved callback and a rejected callback.
        /// The resolved callback chains a value promise (optionally converting to a different value type).
        /// </summary>
        IPromise<TNext> Then<TNext>(Func<IPromise<TNext>> onResolved, Action<Exception> onRejected = null);

        /// <summary>
        /// Add a resolved callback and a rejected callback.
        /// The resolved callback chains a non-value promise.
        /// </summary>
        IPromise Then(Func<IPromise> onResolved, Action<Exception> onRejected = null);

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve.
        /// The resulting promise is resolved when all of the promises have resolved.
        /// It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        IPromise ThenAll(Func<IEnumerable<IPromise>> chain);

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve.
        /// Converts to a non-value promise.
        /// The resulting promise is resolved when all of the promises have resolved.
        /// It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        IPromise<IEnumerable<TNext>> ThenAll<TNext>(Func<IEnumerable<IPromise<TNext>>> chain);

        /// <summary>
        /// Chain a sequence of operations using promises.
        /// Reutrn a collection of functions each of which starts an async operation and yields a promise.
        /// Each function will be called and each promise resolved in turn.
        /// The resulting promise is resolved after each promise is resolved in sequence.
        /// </summary>
        IPromise ThenSequence(Func<IEnumerable<Func<IPromise>>> chain);

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// </summary>
        IPromise ThenRace(Func<IEnumerable<IPromise>> chain);

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Converts to a value promise.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// </summary>
        IPromise<TNext> ThenRace<TNext>(Func<IEnumerable<IPromise<TNext>>> chain);
    }

    /// <summary>
    /// Implements a C# promise.
    /// https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise
    /// </summary>
    public interface IPromise<T>
    {
        /// <summary>
        /// Completes the promise. 
        /// onResolved is called on successful completion.
        /// onRejected is called on error.
        /// </summary>
        void Done(Action<T> onResolved = null, Action<Exception> onRejected = null);

        /// <summary>
        /// Handle errors for the promise. 
        /// </summary>
        IPromise<T> Catch(Action<Exception> onRejected = null);

        /// <summary>
        /// Add a resolved callback and a rejected callback.
        /// </summary>
        IPromise<T> Then(Action<T> onResolved, Action<Exception> onRejected = null);

        /// <summary>
        /// Add a resolved callback and a rejected callback.
        /// The resolved callback chains a value promise (optionally converting to a different value type).
        /// </summary>
        IPromise<TNext> Then<TNext>(Func<T, IPromise<TNext>> onResolved, Action<Exception> onRejected = null);

        /// <summary>
        /// Add a resolved callback and a rejected callback.
        /// The resolved callback chains a non-value promise.
        /// </summary>
        IPromise Then(Func<T, IPromise> onResolved, Action<Exception> onRejected = null);

        /// <summary>
        /// Return a new promise with a different value.
        /// May also change the type of the value.
        /// </summary>
        IPromise<TNext> Transform<TNext>(Func<T, TNext> transform);

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve.
        /// Returns a promise for a collection of the resolved results.
        /// The resulting promise is resolved when all of the promises have resolved.
        /// It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        IPromise<IEnumerable<TNext>> ThenAll<TNext>(Func<T, IEnumerable<IPromise<TNext>>> chain);

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve.
        /// Converts to a non-value promise.
        /// The resulting promise is resolved when all of the promises have resolved.
        /// It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        IPromise ThenAll(Func<T, IEnumerable<IPromise>> chain);

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// Yields the value from the first promise that has resolved.
        /// </summary>
        IPromise<TNext> ThenRace<TNext>(Func<T, IEnumerable<IPromise<TNext>>> chain);

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Converts to a non-value promise.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// Yields the value from the first promise that has resolved.
        /// </summary>
        IPromise ThenRace(Func<T, IEnumerable<IPromise>> chain);
    }
}
