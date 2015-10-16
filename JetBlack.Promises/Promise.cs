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
            return Catch(onRejected ?? Extensions.Nothing, new Promise());
        }

        private IPromise Catch(Action<Exception> onRejected, Promise result)
        {
            return
                Apply(
                    result,
                    result.Resolve,
                    error =>
                    {
                        onRejected(error);
                        result.Reject(error);
                    });
        }

        public IPromise Then(Action onFulfilled = null, Action<Exception> onRejected = null)
        {
            return Then(onFulfilled ?? Extensions.Nothing, onRejected ?? Extensions.Nothing, new Promise());
        }

        private IPromise Then(Action onFulfilled, Action<Exception> onRejected, Promise result)
        {
            return
                Apply(
                    result,
                    () =>
                    {
                        onFulfilled();
                        result.Resolve();
                    },
                    error =>
                    {
                        onRejected(error);
                        result.Reject(error);
                    });
        }

        public IPromise<TNext> Then<TNext>(Func<IPromise<TNext>> onFulfilled, Action<Exception> onRejected = null)
        {
            return Then(onFulfilled, onRejected ?? Extensions.Nothing, new Promise<TNext>());
        }

        private IPromise<TNext> Then<TNext>(Func<IPromise<TNext>> onFulfilled, Action<Exception> onRejected, Promise<TNext> result)
        {
            return
                Apply(
                    result,
                    () => onFulfilled().Then(nextValue => result.Resolve(nextValue), result.Reject),
                    error =>
                    {
                        onRejected(error);
                        result.Reject(error);
                    });
        }

        public IPromise Then(Func<IPromise> onFulfilled, Action<Exception> onRejected = null)
        {
            return Then(onFulfilled, onRejected ?? Extensions.Nothing, new Promise());
        }

        private Promise Then(Func<IPromise> onFulfilled, Action<Exception> onRejected, Promise result)
        {
            return
                Apply(
                    result,
                    () =>
                    {
                        if (onFulfilled == null)
                            result.Resolve();
                        else
                            onFulfilled().Then(() => result.Resolve(), result.Reject);
                    },
                    error =>
                    {
                        onRejected(error);
                        result.Reject(error);
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
            return All(promises, new Promise());
        }

        public static IPromise All(IEnumerable<IPromise> promises)
        {
            return All(promises.ToList(), new Promise());
        }

        private static IPromise All(ICollection<IPromise> promises, Promise result)
        {
            var remaining = promises.Count;

            promises.ForEach(promise =>
                promise.Catch(error =>
                {
                    if (result.State == PromiseState.Pending)
                        result.Reject(error);
                }).Then(() =>
                {
                    if (--remaining <= 0)
                        result.Resolve();
                }).Done());

            return result;
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
            return sequence.Aggregate(Resolved(), (prevPromise, factory) => prevPromise.Then(factory));
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
            return Race(promises, new Promise());
        }

        public static IPromise Race(IEnumerable<IPromise> promises)
        {
            return Race(promises.ToList(), new Promise());
        }

        private static IPromise Race(ICollection<IPromise> promises, Promise result)
        {
            if (promises.Count == 0)
                throw new ArgumentOutOfRangeException("promises", "Must have at least one promise.");

            promises.ForEach(promise =>
                promise.Catch(error =>
                {
                    if (result.State == PromiseState.Pending)
                        result.Reject(error); // If a promise errorred and the result promise is still pending, reject it.
                }).Then(() =>
                {
                    if (result.State == PromiseState.Pending)
                        result.Resolve();
                }).Done());

            return result;
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

        public override string ToString()
        {
            var s = new StringBuilder("State=").Append(State);
            if (State == PromiseState.Rejected)
            {
                    s.Append(", Exception=").Append(_error.Message);
                    if (!string.IsNullOrWhiteSpace(_error.StackTrace))
                        s.Append(": ").Append(_error.StackTrace);
            }
            return s.ToString();
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
                throw new ApplicationException("Can't resolve a promise that isn't pending.");

            _value = value;
            State = PromiseState.Resolved;
            _handlers.Resolve(value);
        }

        public void Reject(Exception error)
        {
            if (State != PromiseState.Pending)
                throw new ApplicationException("Can't reject a promise that isn't pending.");

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
            return Then(onFulfilled ?? Extensions.Nothing, onRejected ?? Extensions.Nothing, new Promise<T>());
        }

        private IPromise<T> Then(Action<T> onFulfilled, Action<Exception> onRejected, Promise<T> result)
        {
            return
                Apply(
                    result,
                    value =>
                    {
                        onFulfilled(value);
                        result.Resolve(value);
                    },
                    error =>
                    {
                        onRejected(error);
                        result.Reject(error);
                    });
        }

        public IPromise Then(Func<T, IPromise> onFulfilled, Action<Exception> onRejected = null)
        {
            return Then(onFulfilled, onRejected ?? Extensions.Nothing, new Promise());
        }

        private IPromise Then(Func<T, IPromise> onFulfilled, Action<Exception> onRejected, Promise result)
        {
            return
                Apply(
                    result,
                    value =>
                    {
                        if (onFulfilled != null)
                            onFulfilled(value).Then(() => result.Resolve(), result.Reject);
                        else
                            result.Resolve();
                    },
                    error =>
                    {
                        onRejected(error);
                        result.Reject(error);
                    });
        }

        public IPromise<TNext> Then<TNext>(Func<T, IPromise<TNext>> onFulfilled, Action<Exception> onRejected = null)
        {
            return Then(onFulfilled, onRejected ?? Extensions.Nothing, new Promise<TNext>());
        }

        private IPromise<TNext> Then<TNext>(Func<T, IPromise<TNext>> onFulfilled, Action<Exception> onRejected, Promise<TNext> result)
        {
            return
                Apply(
                    result,
                    value => onFulfilled(value).Then(chainedValue => result.Resolve(chainedValue), result.Reject),
                    error =>
                    {
                        onRejected(error);
                        result.Reject(error);
                    });
        }

        public IPromise<TNext> Project<TNext>(Func<T, TNext> projector)
        {
            return Transform(projector, new Promise<TNext>());
        }

        private IPromise<TNext> Transform<TNext>(Func<T, TNext> transform, Promise<TNext> result)
        {
            return
                Apply(
                    result,
                    value => result.Resolve(transform(value)),
                    result.Reject);
        }

        private TRejectable Apply<TRejectable>(TRejectable rejectable, Action<T> onFulfilled, Action<Exception> onRejected) where TRejectable:IRejectable
        {
            switch (State)
            {
                case PromiseState.Pending:
                    _handlers.AddResolvers(onFulfilled, rejectable);
                    _handlers.AddRejectors(onRejected, rejectable);
                    break;
                case PromiseState.Resolved:
                    onFulfilled.TryCatch(_value, rejectable.Reject);
                    break;
                case PromiseState.Rejected:
                    onRejected.TryCatch(_error, rejectable.Reject);
                    break;
            }

            return rejectable;
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
            return All(promises, new Promise<IEnumerable<T>>());
        }

        public static IPromise<IEnumerable<T>> All(IEnumerable<IPromise<T>> promises)
        {
            return All(promises.ToList(), new Promise<IEnumerable<T>>());
        }

        private static IPromise<IEnumerable<T>> All(ICollection<IPromise<T>> promises, Promise<IEnumerable<T>> result)
        {
            var results = new T[promises.Count];

            if (promises.Count == 0)
                return Promise<IEnumerable<T>>.Resolved(results);

            var remaining = promises.Count;

            promises.ForEach((promise, index) =>
                promise.Catch(error =>
                {
                    if (result.State == PromiseState.Pending)
                        result.Reject(error);
                }).Then(next =>
                {
                    results[index] = next;

                    if (--remaining <= 0)
                        result.Resolve(results);
                }).Done());

            return result;
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
            return Race(promises, new Promise<T>());
        }

        public static IPromise<T> Race(IEnumerable<IPromise<T>> promises)
        {
            return Race(promises.ToList(), new Promise<T>());
        }

        private static IPromise<T> Race(ICollection<IPromise<T>> promises, Promise<T> result)
        {
            if (promises.Count == 0)
                throw new ArgumentOutOfRangeException("promises", "Must supply at least one promise.");

            promises.ForEach(promise =>
                promise.Catch(error =>
                {
                    if (result.State == PromiseState.Pending)
                        result.Reject(error);
                }).Then(next =>
                {
                    if (result.State == PromiseState.Pending)
                        result.Resolve(next);
                }).Done());

            return result;
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
