using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JetBlack.Promises
{
    public class Promise : IPromise, IPendingPromise
    {
        public static event EventHandler<ExceptionEventArgs> UnhandledException;

        private readonly Handlers _handlers = new Handlers();

        private Exception _error;

        public PromiseState State { get; private set; }

        public Promise()
        {
            State = PromiseState.Pending;
        }

        public Promise(Action<Action, Action<Exception>> resolver)
        {
            State = PromiseState.Pending;
            resolver.TryCatch(Resolve, Reject, Reject);
        }

        public void Resolve()
        {
            if (State != PromiseState.Pending)
                throw new InvalidOperationException("Can't resolve a promise which is not pending");

            State = PromiseState.Resolved;
            _handlers.Resolve();
        }

        public void Reject(Exception error)
        {
            if (State != PromiseState.Pending)
                throw new InvalidOperationException("Can't reject a promis which isn't pending");

            _error = error;
            State = PromiseState.Rejected;
            _handlers.Reject(error);
        }

        public void Done(Action onFulfilled = null, Action<Exception> onRejected = null)
        {
            Apply(new Promise(), onFulfilled ?? Extensions.Nothing, onRejected ?? (error => PropagateUnhandledException(this, error)));
        }

        public IPromise Catch(Action<Exception> onRejected = null)
        {
            return Catch(new Promise(), onRejected ?? Extensions.Nothing);
        }

        private IPromise Catch(Promise promise, Action<Exception> onRejected)
        {
            return
                Apply(
                    promise,
                    promise.Resolve,
                    error =>
                    {
                        onRejected(error);
                        promise.Reject(error);
                    });
        }

        public IPromise Then(Action onFulfilled = null, Action<Exception> onRejected = null)
        {
            return Then(new Promise(), onFulfilled ?? Extensions.Nothing, onRejected ?? Extensions.Nothing);
        }

        private IPromise Then(Promise promise, Action onFulfilled, Action<Exception> onRejected)
        {
            return
                Apply(
                    promise,
                    () =>
                    {
                        onFulfilled();
                        promise.Resolve();
                    },
                    error =>
                    {
                        onRejected(error);
                        promise.Reject(error);
                    });
        }

        public IPromise<TNext> Then<TNext>(Func<IPromise<TNext>> onFulfilled, Action<Exception> onRejected = null)
        {
            return Then(new Promise<TNext>(), onFulfilled, onRejected ?? Extensions.Nothing);
        }

        private IPromise<TNext> Then<TNext>(Promise<TNext> promise, Func<IPromise<TNext>> onFulfilled, Action<Exception> onRejected)
        {
            return
                Apply(
                    promise,
                    () => onFulfilled().Then(nextValue => promise.Resolve(nextValue), promise.Reject),
                    error =>
                    {
                        onRejected(error);
                        promise.Reject(error);
                    });
        }

        public IPromise Then(Func<IPromise> onFulfilled, Action<Exception> onRejected = null)
        {
            return Then(new Promise(), onFulfilled, onRejected ?? Extensions.Nothing);
        }

        private Promise Then(Promise promise, Func<IPromise> onFulfilled, Action<Exception> onRejected)
        {
            return
                Apply(
                    promise,
                    () =>
                    {
                        if (onFulfilled == null)
                            promise.Resolve();
                        else
                            onFulfilled().Then(() => promise.Resolve(), promise.Reject);
                    },
                    error =>
                    {
                        onRejected(error);
                        promise.Reject(error);
                    });
        }

        private TRejectable Apply<TRejectable>(TRejectable rejectable, Action onFulfilled, Action<Exception> onRejected) where TRejectable : IRejectable
        {
            switch (State)
            {
                case PromiseState.Pending:
                    _handlers.AddResolver(onFulfilled, rejectable);
                    _handlers.AddRejector(onRejected, rejectable);
                    break;

                case PromiseState.Resolved:
                    onFulfilled.TryCatch(rejectable.Reject);
                    break;

                case PromiseState.Rejected:
                    onRejected.TryCatch(_error, rejectable.Reject);
                    break;
            }

            return rejectable;
        }

        public IPromise ThenAll(Func<IEnumerable<IPromise>> chain)
        {
            return Then(() => All(chain()));
        }

        public IPromise<IEnumerable<T>> ThenAll<T>(Func<IEnumerable<IPromise<T>>> chain)
        {
            return Then(() => Promise<T>.All(chain()));
        }

        public static IPromise All(params IPromise[] promises)
        {
            return All((IEnumerable<IPromise>)promises); // Cast is required to force use of the other All function.
        }

        public static IPromise All(IEnumerable<IPromise> promises)
        {
            var promisesArray = promises.ToArray();
            if (promisesArray.Length == 0)
                return Resolved();

            var remaining = promisesArray.Length;
            var resultPromise = new Promise();

            promisesArray.ForEach(promise =>
                promise.Catch(error =>
                {
                    if (resultPromise.State == PromiseState.Pending)
                        resultPromise.Reject(error);
                }).Then(() =>
                {
                    if (--remaining <= 0)
                        resultPromise.Resolve();
                }).Done());

            return resultPromise;
        }

        public IPromise ThenSequence(Func<IEnumerable<Func<IPromise>>> chain)
        {
            return Then(() => Sequence(chain()));
        }

        public static IPromise Sequence(params Func<IPromise>[] sequence)
        {
            return Sequence((IEnumerable<Func<IPromise>>)sequence);
        }

        public static IPromise Sequence(IEnumerable<Func<IPromise>> sequence)
        {
            return sequence.Aggregate(Resolved(), (prevPromise, fn) => prevPromise.Then(fn));
        }

        public IPromise ThenRace(Func<IEnumerable<IPromise>> chain)
        {
            return Then(() => Race(chain()));
        }

        public IPromise<T> ThenRace<T>(Func<IEnumerable<IPromise<T>>> chain)
        {
            return Then(() => Promise<T>.Race(chain()));
        }

        public static IPromise Race(params IPromise[] promises)
        {
            return Race((IEnumerable<IPromise>)promises); // Cast is required to force use of the other function.
        }

        public static IPromise Race(IEnumerable<IPromise> promises)
        {
            var promisesArray = promises.ToArray();
            if (promisesArray.Length == 0)
            {
                throw new ApplicationException("At least 1 input promise must be provided for Race");
            }

            var resultPromise = new Promise();

            promisesArray.ForEach(promise =>
                promise.Catch(error =>
                {
                    if (resultPromise.State == PromiseState.Pending)
                        resultPromise.Reject(error); // If a promise errorred and the result promise is still pending, reject it.
                }).Then(() =>
                {
                    if (resultPromise.State == PromiseState.Pending)
                        resultPromise.Resolve();
                }).Done());

            return resultPromise;
        }

        public static IPromise Resolved()
        {
            var promise = new Promise();
            promise.Resolve();
            return promise;
        }

        public static IPromise Rejected(Exception error)
        {
            var promise = new Promise();
            promise.Reject(error);
            return promise;
        }

        internal static void PropagateUnhandledException(object sender, Exception error)
        {
            var handler = UnhandledException;
            if (handler != null)
                handler(sender, new ExceptionEventArgs(error));
        }
    }

    public class Promise<T> : IPromise<T>, IPendingPromise<T>
    {
        private readonly Handlers<T> _handlers = new Handlers<T>();

        private Exception _error;
        private T _value;

        public PromiseState State { get; private set; }

        public Promise()
        {
            State = PromiseState.Pending;
        }

        public Promise(Action<Action<T>, Action<Exception>> resolver)
        {
            State = PromiseState.Pending;
            resolver.TryCatch(Resolve, Reject, Reject);
        }

        public void Resolve(T value)
        {
            if (State != PromiseState.Pending)
                throw new ApplicationException("Attempt to resolve a promise that is already in state: {0}, a promise can only be resolved when it is still in state: {1}".Format(State, PromiseState.Pending));

            _value = value;
            State = PromiseState.Resolved;
            _handlers.Resolve(value);
        }

        public void Reject(Exception error)
        {
            if (State != PromiseState.Pending)
                throw new ApplicationException("Attempt to reject a promise that is already in state: {0}, a promise can only be rejected when it is still in state: {1}".Format(State, PromiseState.Pending));

            _error = error;
            State = PromiseState.Rejected;
            _handlers.Reject(error);
        }

        public void Done(Action<T> onFulfilled = null, Action<Exception> onRejected = null)
        {
            Apply(new Promise<T>(), onFulfilled ?? Extensions.Nothing, onRejected ?? (error => Promise.PropagateUnhandledException(this, error)));
        }

        public IPromise<T> Catch(Action<Exception> onRejected = null)
        {
            return Catch(new Promise<T>(), onRejected ?? Extensions.Nothing);
        }

        private IPromise<T> Catch(Promise<T> promise, Action<Exception> onRejected)
        {
            return
                Apply(
                    promise,
                    promise.Resolve,
                    error =>
                    {
                        onRejected(error);
                        promise.Reject(error);
                    });
        }

        public IPromise<T> Then(Action<T> onFulfilled, Action<Exception> onRejected = null)
        {
            return Then(new Promise<T>(), onFulfilled ?? Extensions.Nothing, onRejected ?? Extensions.Nothing);
        }

        private IPromise<T> Then(Promise<T> promise, Action<T> onFulfilled, Action<Exception> onRejected)
        {
            return
                Apply(
                    promise,
                    value =>
                    {
                        onFulfilled(value);
                        promise.Resolve(value);
                    },
                    error =>
                    {
                        onRejected(error);
                        promise.Reject(error);
                    });
        }

        public IPromise Then(Func<T, IPromise> onFulfilled, Action<Exception> onRejected = null)
        {
            return Then(new Promise(), onFulfilled, onRejected ?? Extensions.Nothing);
        }

        private IPromise Then(Promise promise, Func<T, IPromise> onFulfilled, Action<Exception> onRejected)
        {
            return
                Apply(
                    promise,
                    value =>
                    {
                        if (onFulfilled != null)
                            onFulfilled(value).Then(() => promise.Resolve(), promise.Reject);
                        else
                            promise.Resolve();
                    },
                    error =>
                    {
                        onRejected(error);
                        promise.Reject(error);
                    });
        }

        public IPromise<TNext> Then<TNext>(Func<T, IPromise<TNext>> onFulfilled, Action<Exception> onRejected = null)
        {
            return Then(new Promise<TNext>(), onFulfilled, onRejected ?? Extensions.Nothing);
        }

        private IPromise<TNext> Then<TNext>(Promise<TNext> promise, Func<T, IPromise<TNext>> onFulfilled, Action<Exception> onRejected)
        {
            return
                Apply(
                    promise,
                    value => onFulfilled(value).Then(chainedValue => promise.Resolve(chainedValue), promise.Reject),
                    error =>
                    {
                        onRejected(error);
                        promise.Reject(error);
                    });
        }

        public IPromise<TNext> Project<TNext>(Func<T, TNext> projector)
        {
            return Transform(new Promise<TNext>(), projector);
        }

        private IPromise<TNext> Transform<TNext>(Promise<TNext> promise, Func<T, TNext> transform)
        {
            return
                Apply(
                    promise,
                    value => promise.Resolve(transform(value)),
                    promise.Reject);
        }

        private TRejectable Apply<TRejectable>(TRejectable promise, Action<T> onFulfilled, Action<Exception> onRejected) where TRejectable:IRejectable
        {
            switch (State)
            {
                case PromiseState.Pending:
                    _handlers.AddResolvers(onFulfilled, promise);
                    _handlers.AddRejectors(onRejected, promise);
                    break;
                case PromiseState.Resolved:
                    onFulfilled.TryCatch(_value, promise.Reject);
                    break;
                case PromiseState.Rejected:
                    onRejected.TryCatch(_error, promise.Reject);
                    break;
            }

            return promise;
        }

        public IPromise<IEnumerable<TNext>> ThenAll<TNext>(Func<T, IEnumerable<IPromise<TNext>>> chain)
        {
            return Then(value => Promise<TNext>.All(chain(value)));
        }

        public IPromise ThenAll(Func<T, IEnumerable<IPromise>> chain)
        {
            return Then(value => Promise.All(chain(value)));
        }

        public static IPromise<IEnumerable<T>> All(params IPromise<T>[] promises)
        {
            return All((IEnumerable<IPromise<T>>)promises); // Cast is required to force use of the other All function.
        }

        public static IPromise<IEnumerable<T>> All(IEnumerable<IPromise<T>> promises)
        {
            var promisesArray = promises.ToArray();
            if (promisesArray.Length == 0)
                return Promise<IEnumerable<T>>.Resolved(new T[0]);

            var remainingCount = promisesArray.Length;
            var results = new T[remainingCount];
            var resultPromise = new Promise<IEnumerable<T>>();

            promisesArray.ForEach((promise, index) => promise
                .Catch(error =>
                {
                    if (resultPromise.State == PromiseState.Pending)
                    {
                        // If a promise errorred and the result promise is still pending, reject it.
                        resultPromise.Reject(error);
                    }
                })
                .Then(result =>
                {
                    results[index] = result;

                    --remainingCount;
                    if (remainingCount <= 0)
                    {
                        // This will never happen if any of the promises errorred.
                        resultPromise.Resolve(results);
                    }
                })
                .Done());

            return resultPromise;
        }

        public IPromise<TNext> ThenRace<TNext>(Func<T, IEnumerable<IPromise<TNext>>> chain)
        {
            return Then(value => Promise<TNext>.Race(chain(value)));
        }

        public IPromise ThenRace(Func<T, IEnumerable<IPromise>> chain)
        {
            return Then(value => Promise.Race(chain(value)));
        }

        public static IPromise<T> Race(params IPromise<T>[] promises)
        {
            return Race((IEnumerable<IPromise<T>>)promises); // Cast is required to force use of the other function.
        }

        public static IPromise<T> Race(IEnumerable<IPromise<T>> promises)
        {
            var promisesArray = promises.ToArray();
            if (promisesArray.Length == 0)
            {
                throw new ApplicationException("At least 1 input promise must be provided for Race");
            }

            var resultPromise = new Promise<T>();

            promisesArray.ForEach(promise => promise
                .Catch(error =>
                {
                    if (resultPromise.State == PromiseState.Pending)
                        resultPromise.Reject(error);// If a promise errorred and the result promise is still pending, reject it.
                })
                .Then(result =>
                {
                    if (resultPromise.State == PromiseState.Pending)
                        resultPromise.Resolve(result);
                })
                .Done());

            return resultPromise;
        }

        public static IPromise<T> Resolved(T value)
        {
            var promise = new Promise<T>();
            promise.Resolve(value);
            return promise;
        }

        public static IPromise<T> Rejected(Exception error)
        {
            var promise = new Promise<T>();
            promise.Reject(error);
            return promise;
        }

        public override string ToString()
        {
            var s = new StringBuilder("State=").Append(State);
            switch (State)
            {
                case PromiseState.Resolved:
                    s.Append(", ResolvedValue=").Append(_value);
                    break;
                case PromiseState.Rejected:
                    s.Append(", Exception=").Append(_error.Message);
                    if (!string.IsNullOrWhiteSpace(_error.StackTrace))
                        s.Append(": ").Append(_error.StackTrace);
                    break;
            }
            return s.ToString();
        }
    }
}
