using System;
using System.Linq;
using NUnit.Framework;

namespace JetBlack.Promises.Test
{
    [TestFixture]
    public class PromiseNonGenericFixture
    {
        [Test]
        public void CanResolveSimplePromise()
        {
            var promise = Promise.Resolved();

            var completed = 0;
            promise.Then(() => ++completed);

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void CanRejectSimplePromise()
        {
            var ex = new Exception();
            var promise = Promise.Rejected(ex);

            var errors = 0;
            promise.Catch(e =>
            {
                Assert.AreEqual(ex, e);
                ++errors;
            });

            Assert.AreEqual(1, errors);
        }

        [Test]
        public void ExceptionIsThrownForRejectAfterReject()
        {
            var promise = new Promise();
            promise.Reject(new ApplicationException());
            Assert.Throws<ApplicationException>(() => promise.Reject(new ApplicationException()));
        }

        [Test]
        public void ExceptionIsThrownForRejectAfterResolve()
        {
            var promise = new Promise();
            promise.Resolve();
            Assert.Throws<ApplicationException>(() => promise.Reject(new ApplicationException()));
        }

        [Test]
        public void ExceptionIsThrownForResolveAfterReject()
        {
            var promise = new Promise();
            promise.Reject(new ApplicationException());
            Assert.Throws<ApplicationException>(promise.Resolve);
        }

        [Test]
        public void CanResolvePromiseAndTriggerThenHandler()
        {
            var promise = new Promise();
            var completed = 0;

            promise.Then(() => ++completed);
            promise.Resolve();

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void ExceptionIsThrownForResolveAfterResolve()
        {
            var promise = new Promise();
            promise.Resolve();
            Assert.Throws<ApplicationException>(promise.Resolve);
        }

        [Test]
        public void CanResolvePromiseAndTriggerMultipleThenHandlersInOrder()
        {
            var promise = new Promise();

            var completed = 0;

            promise.Then(() => Assert.AreEqual(1, ++completed));
            promise.Then(() => Assert.AreEqual(2, ++completed));

            promise.Resolve();

            Assert.AreEqual(2, completed);
        }

        [Test]
        public void CanResolvePromiseAndTriggerThenHandlerWithCallbackRegistrationAfterResolve()
        {
            var promise = new Promise();

            var completed = 0;

            promise.Resolve();

            promise.Then(() => ++completed);

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void CanRejectPromiseAndTriggerErrorHandler()
        {
            var promise = new Promise();

            var ex = new ApplicationException();
            var completed = 0;
            promise.Catch(e =>
            {
                Assert.AreEqual(ex, e);
                ++completed;
            });

            promise.Reject(ex);

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void CanRejectPromiseAndTriggerMultipleErrorHandlersInOrder()
        {
            var promise = new Promise();

            var ex = new ApplicationException();
            var completed = 0;

            promise.Catch(e =>
            {
                Assert.AreEqual(ex, e);
                Assert.AreEqual(1, ++completed);
            });
            promise.Catch(e =>
            {
                Assert.AreEqual(ex, e);
                Assert.AreEqual(2, ++completed);
            });

            promise.Reject(ex);

            Assert.AreEqual(2, completed);
        }

        [Test]
        public void CanRejectPromiseAndTriggerErrorHandlerWithRegistrationAfterReject()
        {
            var promise = new Promise();

            var ex = new ApplicationException();
            promise.Reject(ex);

            var completed = 0;
            promise.Catch(e =>
            {
                Assert.AreEqual(ex, e);
                ++completed;
            });

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void ErrorHandlerIsNotInvokedForResolvedPromised()
        {
            var promise = new Promise();

            promise.Catch(e =>
            {
                throw new ApplicationException("This shouldn't happen");
            });

            promise.Resolve();
        }

        [Test]
        public void ThenHandlerIsNotInvokedForRejectedPromise()
        {
            var promise = new Promise();

            promise.Then(() =>
            {
                throw new ApplicationException("This shouldn't happen");
            });

            promise.Reject(new ApplicationException("Rejection!"));
        }

        [Test]
        public void ChainMultiplePromisesUsingAll()
        {
            var promise = new Promise();
            var chainedPromise1 = new Promise();
            var chainedPromise2 = new Promise();

            var completed = 0;

            promise
                //.ThenAll(() => LinqExts.FromItems(chainedPromise1, chainedPromise2).Cast<IPromise>())
                .ThenAll(() => new []{chainedPromise1, chainedPromise2})
                .Then(() => ++completed);

            Assert.AreEqual(0, completed);

            promise.Resolve();

            Assert.AreEqual(0, completed);

            chainedPromise1.Resolve();

            Assert.AreEqual(0, completed);

            chainedPromise2.Resolve();

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void ChainMultiplePromisesUsingAllThatAreResolvedOutOfOrder()
        {
            var promise = new Promise();
            var chainedPromise1 = new Promise<int>();
            var chainedPromise2 = new Promise<int>();
            const int chainedResult1 = 10;
            const int chainedResult2 = 15;

            var completed = 0;

            promise
                //.ThenAll(() => LinqExts.FromItems(chainedPromise1, chainedPromise2).Cast<IPromise<int>>())
                .ThenAll(() => new[]{chainedPromise1, chainedPromise2})
                .Then(result =>
                {
                    var items = result.ToArray();
                    Assert.AreEqual(2, items.Length);
                    Assert.AreEqual(chainedResult1, items[0]);
                    Assert.AreEqual(chainedResult2, items[1]);

                    ++completed;
                });

            Assert.AreEqual(0, completed);

            promise.Resolve();

            Assert.AreEqual(0, completed);

            chainedPromise1.Resolve(chainedResult1);

            Assert.AreEqual(0, completed);

            chainedPromise2.Resolve(chainedResult2);

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void ChainMultipleValuePromisesUsingAllResolvedOutOfOrder()
        {
            var promise = new Promise();
            var chainedPromise1 = new Promise<int>();
            var chainedPromise2 = new Promise<int>();
            const int chainedResult1 = 10;
            const int chainedResult2 = 15;

            var completed = 0;

            promise
                //.ThenAll(() => LinqExts.FromItems(chainedPromise1, chainedPromise2).Cast<IPromise<int>>())
                .ThenAll(() => new []{chainedPromise1, chainedPromise2})
                .Then(result =>
                {
                    var items = result.ToArray();
                    Assert.AreEqual(2, items.Length);
                    Assert.AreEqual(chainedResult1, items[0]);
                    Assert.AreEqual(chainedResult2, items[1]);

                    ++completed;
                });

            Assert.AreEqual(0, completed);

            promise.Resolve();

            Assert.AreEqual(0, completed);

            chainedPromise2.Resolve(chainedResult2);

            Assert.AreEqual(0, completed);

            chainedPromise1.Resolve(chainedResult1);

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void CombinedPromiseIsResolvedWhenChildrenAreResolved()
        {
            var promise1 = new Promise();
            var promise2 = new Promise();

            //var all = Promise.All(LinqExts.FromItems<IPromise>(promise1, promise2));
            var all = Promise.All(promise1, promise2);

            var completed = 0;

            all.Then(() => ++completed);

            promise1.Resolve();
            promise2.Resolve();

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void CombinedPromiseIsRejectedWhenFirstPromiseIsRejected()
        {
            var promise1 = new Promise();
            var promise2 = new Promise();

            //var all = Promise.All(LinqExts.FromItems<IPromise>(promise1, promise2));
            var all = Promise.All(promise1, promise2);

            all.Then(() =>
            {
                throw new ApplicationException("Shouldn't happen");
            });

            var errors = 0;
            all.Catch(e =>
            {
                ++errors;
            });

            promise1.Reject(new ApplicationException("Error!"));
            promise2.Resolve();

            Assert.AreEqual(1, errors);
        }

        [Test]
        public void CombinedPromiseIsRejectedWhenSecondPromiseIsRejected()
        {
            var promise1 = new Promise();
            var promise2 = new Promise();

            //var all = Promise.All(LinqExts.FromItems<IPromise>(promise1, promise2));
            var all = Promise.All(promise1, promise2);

            all.Then(() =>
            {
                throw new ApplicationException("Shouldn't happen");
            });

            var errors = 0;
            all.Catch(e =>
            {
                ++errors;
            });

            promise1.Resolve();
            promise2.Reject(new ApplicationException("Error!"));

            Assert.AreEqual(1, errors);
        }

        [Test]
        public void CombinedPromiseIsRejectedWhenBothPromisesAreRejected()
        {
            var promise1 = new Promise();
            var promise2 = new Promise();

            //var all = Promise.All(LinqExts.FromItems<IPromise>(promise1, promise2));
            var all = Promise.All(promise1, promise2);

            all.Then(() =>
            {
                throw new ApplicationException("Shouldn't happen");
            });

            var errors = 0;
            all.Catch(e =>
            {
                ++errors;
            });

            promise1.Reject(new ApplicationException("Error!"));
            promise2.Reject(new ApplicationException("Error!"));

            Assert.AreEqual(1, errors);
        }

        [Test]
        public void CombinedPromiseIsResolvedIfThereAreNoPromises()
        {
            //var all = Promise.All(LinqExts.Empty<IPromise>());
            var all = Promise.All(new IPromise[0]);

            var completed = 0;

            all.Then(() => ++completed);
        }

        [Test]
        public void CombinedPromiseIsResolvedWhenAllPromisesAreAlreadyResolved()
        {
            var promise1 = Promise.Resolved();
            var promise2 = Promise.Resolved();

            //var all = Promise.All(LinqExts.FromItems(promise1, promise2));
            var all = Promise.All(promise1, promise2);

            var completed = 0;

            all.Then(() =>
            {
                ++completed;
            });

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void ExceptionThrownDuringTransformRejectsPromise()
        {
            var promise = new Promise();

            var errors = 0;
            var ex = new Exception();

            var transformedPromise = promise
                .Then(() =>
                {
                    throw ex;
                })
                .Catch(e =>
                {
                    Assert.AreEqual(ex, e);

                    ++errors;
                });

            promise.Resolve();

            Assert.AreEqual(1, errors);
        }

        [Test]
        public void CanChainPromise()
        {
            var promise = new Promise();
            var chainedPromise = new Promise();

            var completed = 0;

            promise
                .Then(() => chainedPromise)
                .Then(() => ++completed);

            promise.Resolve();
            chainedPromise.Resolve();

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void CanChainPromiseAndConvertToPromiseThatYieldsSomeValue()
        {
            var promise = new Promise();
            var chainedPromise = new Promise<string>();
            const string chainedPromiseValue = "some-value";

            var completed = 0;

            promise
                .Then(() => chainedPromise)
                .Then(v =>
                {
                    Assert.AreEqual(chainedPromiseValue, v);

                    ++completed;
                });

            promise.Resolve();
            chainedPromise.Resolve(chainedPromiseValue);

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void ExceptionThrownInChainRejectsResultingPromise()
        {
            var promise = new Promise();
            var chainedPromise = new Promise();

            var ex = new Exception();
            var errors = 0;

            promise
                .Then(() =>
                {
                    throw ex;
                })
                .Catch(e =>
                {
                    Assert.AreEqual(ex, e);

                    ++errors;
                });

            promise.Resolve();

            Assert.AreEqual(1, errors);
        }

        [Test]
        public void RejectionOfSourcePromiseRejectsChainedPromise()
        {
            var promise = new Promise();
            var chainedPromise = new Promise();

            var ex = new Exception();
            var errors = 0;

            promise
                .Then(() => chainedPromise)
                .Catch(e =>
                {
                    Assert.AreEqual(ex, e);

                    ++errors;
                });

            promise.Reject(ex);

            Assert.AreEqual(1, errors);
        }

        [Test]
        public void RaceIsResolvedWhenFirstPromiseIsResolvedFirst()
        {
            var promise1 = new Promise();
            var promise2 = new Promise();

            var completed = 0;

            Promise
                .Race(promise1, promise2)
                .Then(() => ++completed);

            promise1.Resolve();

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void RaceIsResolvedWhenSecondPromiseIsResolvedFirst()
        {
            var promise1 = new Promise();
            var promise2 = new Promise();

            var completed = 0;

            Promise
                .Race(promise1, promise2)
                .Then(() => ++completed);

            promise2.Resolve();

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void RaceIsRejectedWhenFirstPromiseIsRejectedFirst()
        {
            var promise1 = new Promise();
            var promise2 = new Promise();

            Exception ex = null;

            Promise
                .Race(promise1, promise2)
                .Catch(e => ex = e);

            var expected = new Exception();
            promise1.Reject(expected);

            Assert.AreEqual(expected, ex);
        }

        [Test]
        public void RaceIsRejectedWhenSecondPromiseIsRejectedFirst()
        {
            var promise1 = new Promise();
            var promise2 = new Promise();

            Exception ex = null;

            Promise
                .Race(promise1, promise2)
                .Catch(e => ex = e);

            var expected = new Exception();
            promise2.Reject(expected);

            Assert.AreEqual(expected, ex);
        }

        [Test]
        public void SequenceWithNoOperationsIsDirectlyResolved()
        {
            var completed = 0;

            Promise
                .Sequence(new Func<IPromise>[0])
                .Then(() => ++completed);

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void SequencedIsNotResolvedWhenOperationIsNotResolved()
        {
            var completed = 0;

            Promise
                .Sequence(() => new Promise())
                .Then(() => ++completed);

            Assert.AreEqual(0, completed);
        }

        [Test]
        public void SequenceIsResolvedWhenOperationIsResolved()
        {
            var completed = 0;

            Promise
                .Sequence(Promise.Resolved)
                .Then(() => ++completed);

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void SequenceIsUnresolvedWhenSomeOperationsAreUnresolved()
        {
            var completed = 0;

            Promise
                .Sequence(
                    Promise.Resolved,
                    () => new Promise()
                )
                .Then(() => ++completed);

            Assert.AreEqual(0, completed);
        }

        [Test]
        public void SequenceIsResolvedWhenAllOperationsAreResolved()
        {
            var completed = 0;

            Promise
                .Sequence(
                    Promise.Resolved,
                    Promise.Resolved
                )
                .Then(() => ++completed);

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void SequencedOperationsAreRunInOrderIsDirectlyResolved()
        {
            var order = 0;

            Promise
                .Sequence(
                    () =>
                    {
                        Assert.AreEqual(1, ++order);
                        return Promise.Resolved();
                    },
                    () =>
                    {
                        Assert.AreEqual(2, ++order);
                        return Promise.Resolved();
                    },
                    () =>
                    {
                        Assert.AreEqual(3, ++order);
                        return Promise.Resolved();
                    }
                );

            Assert.AreEqual(3, order);
        }

        [Test]
        public void ExceptionThrownInSequenceRejectsThePromise()
        {
            var errored = 0;
            var completed = 0;
            var ex = new Exception();

            Promise
                .Sequence(() =>
                {
                    throw ex;
                })
                .Catch(e =>
                {
                    Assert.AreEqual(ex, e);
                    ++errored;
                })
                .Then(() => ++completed);

            Assert.AreEqual(1, errored);
            Assert.AreEqual(0, completed);
        }

        [Test]
        public void ExceptionThrownInSequenceStopsFollowingOperationsFromBeingInvoked()
        {
            var completed = 0;

            Promise
                .Sequence(
                    () =>
                    {
                        ++completed;
                        return Promise.Resolved();
                    },
                    () =>
                    {
                        throw new Exception();
                    },
                    () =>
                    {
                        ++completed;
                        return Promise.Resolved();
                    }
                );

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void CanResolvePromiseViaResolverFunction()
        {
            var promise = new Promise((resolve, reject) => resolve());

            var completed = 0;
            promise.Then(() =>
            {
                ++completed;
            });

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void CanRejectPromiseViaRejectFunction()
        {
            var ex = new Exception();
            var promise = new Promise((resolve, reject) => reject(ex));

            var completed = 0;
            promise.Catch(e =>
            {
                Assert.AreEqual(ex, e);
                ++completed;
            });

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void ExceptionThrownDuringResolverRejectsProimse()
        {
            var ex = new Exception();
            var promise = new Promise((resolve, reject) =>
            {
                throw ex;
            });

            var completed = 0;
            promise.Catch(e =>
            {
                Assert.AreEqual(ex, e);
                ++completed;
            });

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void UnhandledExceptionIsPropagatedViaEvent()
        {
            var promise = new Promise();
            var ex = new Exception();
            var eventRaised = 0;

            EventHandler<ExceptionEventArgs> handler = (s, e) =>
            {
                Assert.AreEqual(ex, e.Exception);

                ++eventRaised;
            };

            Promise.UnhandledException += handler;

            try
            {
                promise
                    .Then(() =>
                    {
                        throw ex;
                    })
                    .Done();

                promise.Resolve();

                Assert.AreEqual(1, eventRaised);
            }
            finally
            {
                Promise.UnhandledException -= handler;
            }
        }

        [Test]
        public void HandledExceptionIsNotPropagatedViaEvent()
        {
            var promise = new Promise();
            var ex = new Exception();
            var eventRaised = 0;

            EventHandler<ExceptionEventArgs> handler = (s, e) => ++eventRaised;

            Promise.UnhandledException += handler;

            try
            {
                promise
                    .Then(() =>
                    {
                        throw ex;
                    })
                    .Catch(_ =>
                    {
                        // Catch the error.
                    })
                    .Done();

                promise.Resolve();

                Assert.AreEqual(1, eventRaised);
            }
            finally
            {
                Promise.UnhandledException -= handler;
            }

        }

        [Test]
        public void CanHandleDoneOnResolved()
        {
            var promise = new Promise();
            var callback = 0;

            promise.Done(() => ++callback);

            promise.Resolve();

            Assert.AreEqual(1, callback);
        }

        [Test]
        public void CanHandleDoneOnResolvedWithOnReject()
        {
            var promise = new Promise();
            var callback = 0;
            var errorCallback = 0;

            promise.Done(
                () => ++callback,
                ex => ++errorCallback
            );

            promise.Resolve();

            Assert.AreEqual(1, callback);
            Assert.AreEqual(0, errorCallback);
        }

        /*todo:
         * Also want a test that exception thrown during Then triggers the error handler.
         * How do Javascript promises work in this regard?
        [Test]
        public void exception_during_Done_onResolved_triggers_error_hander()
        {
            var promise = new Promise();
            var callback = 0;
            var errorCallback = 0;
            var expectedValue = 5;
            var expectedException = new Exception();
            promise.Done(
                value =>
                {
                    Assert.AreEqual(expectedValue, value);
                    ++callback;
                    throw expectedException;
                },
                ex =>
                {
                    Assert.AreEqual(expectedException, ex);
                    ++errorCallback;
                }
            );
            promise.Resolve(expectedValue);
            Assert.AreEqual(1, callback);
            Assert.AreEqual(1, errorCallback);
        }
         * */

        [Test]
        public void ExceptionDuringThenOnResolvedTriggersErrorHander()
        {
            var promise = new Promise();
            var callback = 0;
            var errorCallback = 0;
            var expectedException = new Exception();

            promise
                .Then(() =>
                {
                    throw expectedException;

                    return Promise.Resolved();
                })
                .Done(
                    () => ++callback,
                    ex =>
                    {
                        Assert.AreEqual(expectedException, ex);

                        ++errorCallback;
                    }
                );

            promise.Resolve();

            Assert.AreEqual(0, callback);
            Assert.AreEqual(1, errorCallback);
        }

        [Test]
        public void InnerExceptionHandledByOuterPromise()
        {
            var promise = new Promise();
            var errorCallback = 0;
            var expectedException = new Exception();

            var eventRaised = 0;

            EventHandler<ExceptionEventArgs> handler = (s, e) => ++eventRaised;

            Promise.UnhandledException += handler;

            try
            {
                promise
                    .Then(() => Promise.Resolved().Then(() => { throw expectedException; }))
                    .Catch(ex =>
                    {
                        Assert.AreEqual(expectedException, ex);

                        ++errorCallback;
                    });

                promise.Resolve();

                // No "done" in the chain, no generic event handler should be called
                Assert.AreEqual(0, eventRaised);

                // Instead the catch should have got the exception
                Assert.AreEqual(1, errorCallback);
            }
            finally
            {
                Promise.UnhandledException -= handler;
            }
        }

        [Test]
        public void InnerExceptionHandledByOuterPromiseWithResults()
        {
            var promise = new Promise<int>();
            var errorCallback = 0;
            var expectedException = new Exception();

            var eventRaised = 0;

            EventHandler<ExceptionEventArgs> handler = (s, e) => ++eventRaised;

            Promise.UnhandledException += handler;

            try
            {
                promise
                    .Then((_) => Promise<int>.Resolved(5).Then((__) => { throw expectedException; }))
                    .Catch(ex =>
                    {
                        Assert.AreEqual(expectedException, ex);

                        ++errorCallback;
                    });

                promise.Resolve(2);

                // No "done" in the chain, no generic event handler should be called
                Assert.AreEqual(0, eventRaised);

                // Instead the catch should have got the exception
                Assert.AreEqual(1, errorCallback);
            }
            finally
            {
                Promise.UnhandledException -= handler;
            }
        }
    }
}
