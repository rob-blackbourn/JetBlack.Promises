﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlack.Promises
{
    public class Promise : IPromise, IPendingPromise
    {
        public static event EventHandler<ExceptionEventArgs> UnhandledException;

        private readonly Handlers _handlers = new Handlers();

        private Exception _rejectionException;

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
                throw new ApplicationException("Attempt to resolve a promise that is already in state: " + State + ", a promise can only be resolved when it is still in state: " + PromiseState.Pending);

            State = PromiseState.Resolved;
            _handlers.Resolve();
        }

        public void Reject(Exception error)
        {
            if (State != PromiseState.Pending)
                throw new ApplicationException("Attempt to reject a promise that is already in state: " + State + ", a promise can only be rejected when it is still in state: " + PromiseState.Pending);

            _rejectionException = error;
            State = PromiseState.Rejected;
            _handlers.Reject(error);
        }

        public void Done(Action onResolved = null, Action<Exception> onRejected = null)
        {
            ActionHandlers(new Promise(), onResolved ?? Extensions.Nothing, onRejected ?? (error => PropagateUnhandledException(this, error)));
        }

        public IPromise Catch(Action<Exception> onRejected = null)
        {
            return Catch(new Promise(), onRejected ?? Extensions.Nothing);
        }

        private IPromise Catch(Promise resultPromise, Action<Exception> onRejected)
        {
            return
                ActionHandlers(
                    resultPromise,
                    resultPromise.Resolve,
                    error =>
                    {
                        onRejected(error);
                        resultPromise.Reject(error);
                    });
        }

        public IPromise Then(Action onResolved = null, Action<Exception> onRejected = null)
        {
            return Then(new Promise(), onResolved ?? Extensions.Nothing, onRejected ?? Extensions.Nothing);
        }

        private IPromise Then(Promise resultPromise, Action onResolved, Action<Exception> onRejected)
        {
            return
                ActionHandlers(
                    resultPromise,
                    () =>
                    {
                        onResolved();
                        resultPromise.Resolve();
                    },
                    error =>
                    {
                        onRejected(error);
                        resultPromise.Reject(error);
                    });
        }

        public IPromise<TNext> Then<TNext>(Func<IPromise<TNext>> onResolved, Action<Exception> onRejected = null)
        {
            return Then(new Promise<TNext>(), onResolved, onRejected ?? Extensions.Nothing);
        }

        private IPromise<TNext> Then<TNext>(Promise<TNext> resultPromise, Func<IPromise<TNext>> onResolved, Action<Exception> onRejected)
        {
            return
                ActionHandlers(
                    resultPromise,
                    () => onResolved().Then(nextValue => resultPromise.Resolve(nextValue), resultPromise.Reject),
                    error =>
                    {
                        onRejected(error);
                        resultPromise.Reject(error);
                    });
        }

        public IPromise Then(Func<IPromise> onResolved, Action<Exception> onRejected = null)
        {
            return Then(new Promise(), onResolved, onRejected ?? Extensions.Nothing);
        }

        private Promise Then(Promise resultPromise, Func<IPromise> onResolved, Action<Exception> onRejected)
        {
            return
                ActionHandlers(
                    resultPromise,
                    () =>
                    {
                        if (onResolved == null)
                            resultPromise.Resolve();
                        else
                            onResolved().Then(() => resultPromise.Resolve(), resultPromise.Reject);
                    },
                    error =>
                    {
                        onRejected(error);
                        resultPromise.Reject(error);
                    });
        }

        private TRejectable ActionHandlers<TRejectable>(TRejectable resultPromise, Action resolveHandler, Action<Exception> rejectHandler) where TRejectable : IRejectable
        {
            switch (State)
            {
                case PromiseState.Resolved:
                    resolveHandler.TryCatch(resultPromise.Reject);
                    break;

                case PromiseState.Rejected:
                    rejectHandler.TryCatch(_rejectionException, resultPromise.Reject);
                    break;

                default:
                    _handlers.AddResolveHandler(resolveHandler, resultPromise);
                    _handlers.AddRejectHandler(rejectHandler, resultPromise);
                    break;
            }

            return resultPromise;
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

        public static IPromise Sequence(params Func<IPromise>[] fns)
        {
            return Sequence((IEnumerable<Func<IPromise>>)fns);
        }

        public static IPromise Sequence(IEnumerable<Func<IPromise>> fns)
        {
            return fns.Aggregate(Resolved(), (prevPromise, fn) => prevPromise.Then(fn));
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

        private Exception _rejectionException;
        private T _resolveValue;

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
                throw new ApplicationException("Attempt to resolve a promise that is already in state: " + State + ", a promise can only be resolved when it is still in state: " + PromiseState.Pending);

            _resolveValue = value;
            State = PromiseState.Resolved;
            _handlers.Resolve(value);
        }

        public void Reject(Exception error)
        {
            if (State != PromiseState.Pending)
                throw new ApplicationException("Attempt to reject a promise that is already in state: " + State + ", a promise can only be rejected when it is still in state: " + PromiseState.Pending);

            _rejectionException = error;
            State = PromiseState.Rejected;
            _handlers.Reject(error);
        }

        public void Done(Action<T> onResolved = null, Action<Exception> onRejected = null)
        {
            ActionHandlers(new Promise<T>(), onResolved ?? Extensions.Nothing, onRejected ?? (error => Promise.PropagateUnhandledException(this, error)));
        }

        public IPromise<T> Catch(Action<Exception> onRejected = null)
        {
            return Catch(new Promise<T>(), onRejected ?? Extensions.Nothing);
        }

        private IPromise<T> Catch(Promise<T> resultPromise, Action<Exception> onRejected)
        {
            return
                ActionHandlers(
                    resultPromise,
                    resultPromise.Resolve,
                    error =>
                    {
                        onRejected(error);
                        resultPromise.Reject(error);
                    });
        }

        public IPromise<T> Then(Action<T> onResolved, Action<Exception> onRejected = null)
        {
            return Then(new Promise<T>(), onResolved ?? Extensions.Nothing, onRejected ?? Extensions.Nothing);
        }

        private IPromise<T> Then(Promise<T> resultPromise, Action<T> onResolved, Action<Exception> onRejected)
        {
            return
                ActionHandlers(
                    resultPromise,
                    value =>
                    {
                        onResolved(value);
                        resultPromise.Resolve(value);
                    },
                    error =>
                    {
                        onRejected(error);
                        resultPromise.Reject(error);
                    });
        }

        public IPromise Then(Func<T, IPromise> onResolved, Action<Exception> onRejected = null)
        {
            return Then(new Promise(), onResolved, onRejected ?? Extensions.Nothing);
        }

        private IPromise Then(Promise resultPromise, Func<T, IPromise> onResolved, Action<Exception> onRejected)
        {
            return
                ActionHandlers(
                    resultPromise,
                    value =>
                    {
                        if (onResolved != null)
                            onResolved(value).Then(() => resultPromise.Resolve(), resultPromise.Reject);
                        else
                            resultPromise.Resolve();
                    },
                    error =>
                    {
                        onRejected(error);
                        resultPromise.Reject(error);
                    });
        }

        public IPromise<TNext> Then<TNext>(Func<T, IPromise<TNext>> onResolved, Action<Exception> onRejected = null)
        {
            return Then(new Promise<TNext>(), onResolved, onRejected ?? Extensions.Nothing);
        }

        private IPromise<TNext> Then<TNext>(Promise<TNext> resultPromise, Func<T, IPromise<TNext>> onResolved, Action<Exception> onRejected)
        {
            return
                ActionHandlers(
                    resultPromise,
                    value => onResolved(value).Then(chainedValue => resultPromise.Resolve(chainedValue), resultPromise.Reject),
                    error =>
                    {
                        onRejected(error);
                        resultPromise.Reject(error);
                    });
        }

        public IPromise<TNext> Transform<TNext>(Func<T, TNext> transform)
        {
            return Transform(new Promise<TNext>(), transform);
        }

        private IPromise<TNext> Transform<TNext>(Promise<TNext> resultPromise, Func<T, TNext> transform)
        {
            return
                ActionHandlers(
                    resultPromise,
                    value => resultPromise.Resolve(transform(value)),
                    resultPromise.Reject);
        }

        private TRejectable ActionHandlers<TRejectable>(TRejectable resultPromise, Action<T> resolveHandler, Action<Exception> rejectHandler) where TRejectable:IRejectable
        {
            switch (State)
            {
                case PromiseState.Resolved:
                    resolveHandler.TryCatch(_resolveValue, resultPromise.Reject);
                    break;
                case PromiseState.Rejected:
                    rejectHandler.TryCatch(_rejectionException, resultPromise.Reject);
                    break;
                default:
                    _handlers.AddResolveHandler(resolveHandler, resultPromise);
                    _handlers.AddRejectHandler(rejectHandler, resultPromise);
                    break;
            }

            return resultPromise;
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
    }
}