using System;
using System.Collections.Generic;

namespace JetBlack.Promises
{
    /// <summary>
    /// A promise which takes no arguments.
    /// </summary>
    public interface IPromise
    {
        /// <summary>
        /// Completes a promise. Calls onFulfilled or onRejected with the fulfillment value or rejection reason of the promise (as appropriate).
        /// 
        /// Unlike &quot;then&quot; it does not return a Promise.
        /// </summary>
        /// <param name="onFulfilled">Called on successful completion.</param>
        /// <param name="onRejected">Called on error.</param>
        void Done(Action onFulfilled = null, Action<Exception> onRejected = null);

        /// <summary>
        /// Returns a Promise and deals with rejected cases only.
        /// </summary>
        /// <param name="onRejected">Called if the promise is rejected.</param>
        /// <returns>A new promise resolving to the return value of the callback if it is called, or to its original fulfillment value if the promise is instead fulfilled.</returns>
        IPromise Catch(Action<Exception> onRejected);

        /// <summary>
        /// Appends fulfill and rejection handlers to the promise, and returns a new promise
        /// resolving to the return value of the called handler, or to its original settled
        /// value if the promise was not handled (i.e. if the relevant handler onResolved or
        /// onRejected is undefined).
        /// </summary>
        /// <param name="onFulfilled">Called if the promise is fulfilled.</param>
        /// <param name="onRejected">Called if the promise is rejected.</param>
        /// <returns>A new promise resolving to the return value of the called handler, or to its original settled value if the promise was not handled.</returns>
        IPromise Then(Action onFulfilled, Action<Exception> onRejected = null);

        /// <summary>
        /// Add a fulfill callback and a rejected callback.
        /// 
        /// The resolved callback chains a value promise of the specified type.
        /// </summary>
        /// <typeparam name="TNext">The type of the next promise.</typeparam>
        /// <param name="onFulfilled">Called if the promise is fullfilled.</param>
        /// <param name="onRejected">Called if the promise is rejected.</param>
        /// <returns>The value with which the promise was fulfilled.</returns>
        IPromise<TNext> Then<TNext>(Func<IPromise<TNext>> onFulfilled, Action<Exception> onRejected = null);

        /// <summary>
        /// Calls onFulfilled or onRejected and returns a new promise.
        /// </summary>
        /// <param name="onFulfilled">Called if the promise is fullfilled.</param>
        /// <param name="onRejected">Called if the promise is rejected.</param>
        /// <returns>A new promise which takes no argument.</returns>
        IPromise Then(Func<IPromise> onFulfilled, Action<Exception> onRejected = null);

        // <summary>
        // 
        // </summary>
        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve. The resulting 
        /// promise is resolved when all of the promises have resolved. It is
        /// rejected as soon as any of the promises have been rejected.
        /// </summary>
        /// <param name="chain">A function returning an enumeration of chains.</param>
        /// <returns>A promise which takes no arguments.</returns>
        IPromise ThenAll(Func<IEnumerable<IPromise>> chain);

        /// <summary>
        /// Returns a Promise that waits for all promises in the chain to be fulfilled
        /// and is then fulfilled with an array of those resulting values (in the same
        /// order as the input).
        /// </summary>
        /// <typeparam name="TNext">The type of the argument of the resultant promise.</typeparam>
        /// <param name="chain">A function returning an enumeration of chains.</param>
        /// <returns>A promise which takes the resolved value.</returns>
        IPromise<IEnumerable<TNext>> ThenAll<TNext>(Func<IEnumerable<IPromise<TNext>>> chain);

        /// <summary>
        /// Chain a sequence of operations using promises.
        /// Reutrn a collection of functions each of which starts an async operation and yields a promise.
        /// Each function will be called and each promise resolved in turn.
        /// The resulting promise is resolved after each promise is resolved in sequence.
        /// </summary>
        /// <param name="chain">A function returning an enumeration of chains.</param>
        /// <returns>A promise which takes no arguments.</returns>
        IPromise ThenSequence(Func<IEnumerable<Func<IPromise>>> chain);

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// </summary>
        /// <param name="chain">A function returning an enumeration of chains.</param>
        /// <returns>A promise which takes no arguments.</returns>
        IPromise ThenRace(Func<IEnumerable<IPromise>> chain);

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Converts to a value promise.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// </summary>
        /// <returns>A promise which takes the resolved value.</returns>
        IPromise<TNext> ThenRace<TNext>(Func<IEnumerable<IPromise<TNext>>> chain);
    }

    /// <summary>
    /// A promise which resolves to a value.
    /// </summary>
    /// <typeparam name="T">The type of the resolved value.</typeparam>
    public interface IPromise<out T>
    {
        /// <summary>
        /// Completes the promise.
        /// </summary>
        /// <param name="onFulfilled">Called if the promise is fulfilled.</param>
        /// <param name="onRejected">Called if the promise is rejected.</param>
        void Done(Action<T> onFulfilled = null, Action<Exception> onRejected = null);

        /// <summary>
        /// Handle errors for the promise. 
        /// </summary>
        /// <param name="onRejected">Called if the promise is rejected.</param>
        /// <returns>A promise which resolves to a value.</returns>
        IPromise<T> Catch(Action<Exception> onRejected = null);

        /// <summary>
        /// Calls onFulfilled or onRejected with the fulfillment value or rejection
        /// reason of the promise (as appropriate) and returns a new promise
        /// resolving to the return value of the called handler.
        /// </summary>
        /// <param name="onFulfilled">Called if the promise is fulfilled.</param>
        /// <param name="onRejected">Called if the promise is rejected.</param>
        /// <returns>A promise which resolves to a value.</returns>
        IPromise<T> Then(Action<T> onFulfilled, Action<Exception> onRejected = null);

        /// <summary>
        /// Calls onFulfilled or onRejected with the fulfillment value or rejection
        /// reason of the promise (as appropriate) and returns a new promise resolving
        /// to the return value of the called handler which may be different to that of this promise.
        /// </summary>
        /// <typeparam name="TNext">The type of the next promise.</typeparam>
        /// <param name="onFulfilled">Called if the promise is fulfilled.</param>
        /// <param name="onRejected">Called if the promise is rejected.</param>
        /// <returns>A promise which resolves to a value.</returns>
        IPromise<TNext> Then<TNext>(Func<T, IPromise<TNext>> onFulfilled, Action<Exception> onRejected = null);

        /// <summary>
        /// Calls onFulfilled or onRejected with the fulfillment value or rejection
        /// reason of the promise (as appropriate) and returns a new promise which
        /// takes no argument.
        /// </summary>
        /// <param name="onFulfilled">Called if the promise is fulfilled.</param>
        /// <param name="onRejected">Called if the promise is rejected.</param>
        /// <returns>A promise which takes no argument.</returns>
        IPromise Then(Func<T, IPromise> onFulfilled, Action<Exception> onRejected = null);

        // <summary>
        // Return a new promise with a different value.
        // May also change the type of the value.
        // </summary>
        /// <summary>
        /// Projects the promise to a new promise of the given type.
        /// </summary>
        /// <typeparam name="TNext">The type of the next promise.</typeparam>
        /// <param name="projector">The projection function to be applied to the resolved value.</param>
        /// <returns>A promise of the new type.</returns>
        IPromise<TNext> Project<TNext>(Func<T, TNext> projector);

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve.
        /// 
        /// Returns a promise for a collection of the resolved results. The resulting
        /// promise is resolved when all of the promises have resolved. It is rejected
        /// as soon as any of the promises have been rejected.
        /// </summary>
        /// <typeparam name="TNext">The type of the returned promise.</typeparam>
        /// <param name="chain">A factory function which takes the resolved value and generates an enumeration of promises.</param>
        /// <returns>A promise of the new type.</returns>
        IPromise<IEnumerable<TNext>> ThenAll<TNext>(Func<T, IEnumerable<IPromise<TNext>>> chain);

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve. Converts to a
        /// non-value promise. The resulting promise is resolved when all of the promises
        /// have resolved. It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        /// <param name="chain">A factory function which takes the resolved value and generates an enumeration of promises.</param>
        /// <returns>A promise which takes no argument.</returns>
        IPromise ThenAll(Func<T, IEnumerable<IPromise>> chain);

        /// <summary>
        /// Takes a function that yields an enumerable of promises. Returns a promise that
        /// resolves when the first of the promises has resolved. Yields the value from the
        /// first promise that has resolved.
        /// </summary>
        /// <typeparam name="TNext">The type of the value to which the returned promise will resolve.</typeparam>
        /// <param name="chain">A factory function which takes the resolved value and generates an enumeration of promises.</param>
        /// <returns>A promise of the new type.</returns>
        IPromise<TNext> ThenRace<TNext>(Func<T, IEnumerable<IPromise<TNext>>> chain);

        // <summary>
        // 
        // </summary>
        /// <summary>
        /// Takes a function that yields an enumerable of promises. Converts to a
        /// non-value promise. Returns a promise that resolves when the first of
        /// the promises has resolved. Yields the value from the first promise that
        /// has resolved.
        /// </summary>
        /// <param name="chain">A factory function which takes the resolved value and generates an enumeration of promises.</param>
        /// <returns>A promise which takes no argument.</returns>
        IPromise ThenRace(Func<T, IEnumerable<IPromise>> chain);
    }
}
