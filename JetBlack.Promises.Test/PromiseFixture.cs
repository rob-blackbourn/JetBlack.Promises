using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;

namespace JetBlack.Promises.Test
{
    [TestFixture]
    public class PromiseFixture
    {
        [Test]
        public void CanResolveSimplePromise()
        {
            const int promisedValue = 5;
            var promise = Promise<int>.Resolved(promisedValue);

            var completed = 0;
            promise.Then(v =>
            {
                Assert.AreEqual(promisedValue, v);
                ++completed;
            });

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void CanRejectSimplePromise()
        {
            var ex = new Exception();
            var promise = Promise<int>.Rejected(ex);

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
            var promise = new Promise<int>();

            promise.Reject(new ApplicationException());

            Assert.Throws<ApplicationException>(() =>
                promise.Reject(new ApplicationException())
            );
        }

        [Test]
        public void ExceptionIsThrownForRejectAfterResolve()
        {
            var promise = new Promise<int>();

            promise.Resolve(5);

            Assert.Throws<ApplicationException>(() =>
                promise.Reject(new ApplicationException())
            );
        }

        [Test]
        public void ExceptionIsThrownForResolveAfterReject()
        {
            var promise = new Promise<int>();

            promise.Reject(new ApplicationException());

            Assert.Throws<ApplicationException>(() =>
                promise.Resolve(5)
            );
        }

        [Test]
        public void CanResolvePromiseAndTriggerThenHandler()
        {
            var promise = new Promise<int>();

            var completed = 0;
            const int promisedValue = 15;

            promise.Then(v =>
            {
                Assert.AreEqual(promisedValue, v);
                ++completed;
            });

            promise.Resolve(promisedValue);

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void ExceptionIsThrownForResolveAfterResolve()
        {
            var promise = new Promise<int>();

            promise.Resolve(5);

            Assert.Throws<ApplicationException>(() =>
                promise.Resolve(5)
            );
        }

        [Test]
        public void CanResolvePromiseAndTriggerMultipleThenHandlersInOrder()
        {
            var promise = new Promise<int>();

            var completed = 0;

            promise.Then(v => Assert.AreEqual(1, ++completed));
            promise.Then(v => Assert.AreEqual(2, ++completed));

            promise.Resolve(1);

            Assert.AreEqual(2, completed);
        }

        [Test]
        public void CanResolvePromiseAndTriggerThenHandlerWithCallbackRegistrationAfterResolve()
        {
            var promise = new Promise<int>();

            var completed = 0;
            const int promisedValue = -10;

            promise.Resolve(promisedValue);

            promise.Then(v =>
            {
                Assert.AreEqual(promisedValue, v);
                ++completed;
            });

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void CanRejectPromiseAndTriggerErrorHandler()
        {
            var promise = new Promise<int>();

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
            var promise = new Promise<int>();

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
            var promise = new Promise<int>();

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
            var promise = new Promise<int>();

            promise.Catch(e =>
            {
                throw new ApplicationException("This shouldn't happen");
            });

            promise.Resolve(5);
        }

        [Test]
        public void ThenHandlerIsNotInvokedForRejectedPromise()
        {
            var promise = new Promise<int>();

            promise.Then(v =>
            {
                throw new ApplicationException("This shouldn't happen");
            });

            promise.Reject(new ApplicationException("Rejection!"));
        }

        [Test]
        public void ChainMultiplePromisesUsingAll()
        {
            var promise = new Promise<string>();
            var chainedPromise1 = new Promise<int>();
            var chainedPromise2 = new Promise<int>();
            const int chainedResult1 = 10;
            const int chainedResult2 = 15;

            var completed = 0;

            promise
                //.ThenAll(i => LinqExts.FromItems(chainedPromise1, chainedPromise2).Cast<IPromise<int>>())
                .ThenAll(_ => new[]{chainedPromise1, chainedPromise2})
                .Then(result =>
                {
                    var items = result.ToArray();
                    Assert.AreEqual(2, items.Length);
                    Assert.AreEqual(chainedResult1, items[0]);
                    Assert.AreEqual(chainedResult2, items[1]);

                    ++completed;
                });

            Assert.AreEqual(0, completed);

            promise.Resolve("hello");

            Assert.AreEqual(0, completed);

            chainedPromise1.Resolve(chainedResult1);

            Assert.AreEqual(0, completed);

            chainedPromise2.Resolve(chainedResult2);

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void ChainMultiplePromisesUsingAllThatAreResolvedOutOfOrder()
        {
            var promise = new Promise<string>();
            var chainedPromise1 = new Promise<int>();
            var chainedPromise2 = new Promise<int>();
            const int chainedResult1 = 10;
            const int chainedResult2 = 15;

            var completed = 0;

            promise
                //.ThenAll(i => LinqExts.FromItems(chainedPromise1, chainedPromise2).Cast<IPromise<int>>())
                .ThenAll(i => new[]{chainedPromise1, chainedPromise2})
                .Then(result =>
                {
                    var items = result.ToArray();
                    Assert.AreEqual(2, items.Length);
                    Assert.AreEqual(chainedResult1, items[0]);
                    Assert.AreEqual(chainedResult2, items[1]);

                    ++completed;
                });

            Assert.AreEqual(0, completed);

            promise.Resolve("hello");

            Assert.AreEqual(0, completed);

            chainedPromise2.Resolve(chainedResult2);

            Assert.AreEqual(0, completed);

            chainedPromise1.Resolve(chainedResult1);

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void ChainMultiplePromisesUsingAllAndConvertToNonValuePromise()
        {
            var promise = new Promise<string>();
            var chainedPromise1 = new Promise();
            var chainedPromise2 = new Promise();

            var completed = 0;

            promise
                //.ThenAll(i => LinqExts.FromItems(chainedPromise1, chainedPromise2).Cast<IPromise>())
                .ThenAll(i => new[]{chainedPromise1, chainedPromise2})
                .Then(() =>
                {
                    ++completed;
                });

            Assert.AreEqual(0, completed);

            promise.Resolve("hello");

            Assert.AreEqual(0, completed);

            chainedPromise1.Resolve();

            Assert.AreEqual(0, completed);

            chainedPromise2.Resolve();

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void CombinedPromiseIsResolvedWhenChildrenAreResolved()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            //var all = Promise<int>.All(LinqExts.FromItems<IPromise<int>>(promise1, promise2));
            var all = Promise<int>.All(promise1, promise2);

            var completed = 0;

            all.Then(v =>
            {
                ++completed;

                var values = v.ToArray();
                Assert.AreEqual(2, values.Length);
                Assert.AreEqual(1, values[0]);
                Assert.AreEqual(2, values[1]);
            });

            promise1.Resolve(1);
            promise2.Resolve(2);

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void CombinedPromiseIsRejectedWhenFirstPromiseIsRejected()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            //var all = Promise<int>.All(LinqExts.FromItems<IPromise<int>>(promise1, promise2));
            var all = Promise<int>.All(promise1, promise2);

            all.Then(v =>
            {
                throw new ApplicationException("Shouldn't happen");
            });

            var errors = 0;
            all.Catch(e =>
            {
                ++errors;
            });

            promise1.Reject(new ApplicationException("Error!"));
            promise2.Resolve(2);

            Assert.AreEqual(1, errors);
        }

        [Test]
        public void CombinedPromiseIsRejectedWhenSecondPromiseIsRejected()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            //var all = Promise<int>.All(LinqExts.FromItems<IPromise<int>>(promise1, promise2));
            var all = Promise<int>.All(promise1, promise2);

            all.Then(v =>
            {
                throw new ApplicationException("Shouldn't happen");
            });

            var errors = 0;
            all.Catch(e =>
            {
                ++errors;
            });

            promise1.Resolve(2);
            promise2.Reject(new ApplicationException("Error!"));

            Assert.AreEqual(1, errors);
        }

        [Test]
        public void CombinedPromiseIsRejectedWhenBothPromisesAreRejected()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            //var all = Promise<int>.All(LinqExts.FromItems<IPromise<int>>(promise1, promise2));
            var all = Promise<int>.All(promise1, promise2);

            all.Then(v =>
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
            //var all = Promise<int>.All(LinqExts.Empty<IPromise<int>>());
            var all = Promise<int>.All(new IPromise<int>[0]);

            var completed = 0;

            all.Then(v =>
            {
                ++completed;

                Assert.IsEmpty(v);
            });

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void CombinedPromiseIsResolvedWhenAllPromisesAreAlreadyResolved()
        {
            var promise1 = Promise<int>.Resolved(1);
            var promise2 = Promise<int>.Resolved(1);

            //var all = Promise<int>.All(LinqExts.FromItems(promise1, promise2));
            var all = Promise<int>.All(promise1, promise2);

            var completed = 0;

            all.Then(v =>
            {
                ++completed;

                Assert.IsEmpty(v);
            });

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void CanTransformPromiseValue()
        {
            var promise = new Promise<int>();

            const int promisedValue = 15;
            var completed = 0;

            promise
                .Project(v => v.ToString(CultureInfo.InvariantCulture))
                .Then(v =>
                {
                    Assert.AreEqual(promisedValue.ToString(CultureInfo.InvariantCulture), v);
                    ++completed;
                });

            promise.Resolve(promisedValue);

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void RejectionOfSourcePromiseRejectsTransformedPromise()
        {
            var promise = new Promise<int>();

            var ex = new Exception();
            var errors = 0;

            promise
                .Project(v => v.ToString(CultureInfo.InvariantCulture))
                .Catch(e =>
                {
                    Assert.AreEqual(ex, e);
                    ++errors;
                });

            promise.Reject(ex);

            Assert.AreEqual(1, errors);
        }

        [Test]
        public void ExceptionThrownDuringTransformRejectsTransformedPromise()
        {
            var promise = new Promise<int>();

            const int promisedValue = 15;
            var errors = 0;
            var ex = new Exception();

            promise
                .Project<string>(v =>
                {
                    throw ex;
                })
                .Catch(e =>
                {
                    Assert.AreEqual(ex, e);

                    ++errors;
                });

            promise.Resolve(promisedValue);

            Assert.AreEqual(1, errors);
        }

        [Test]
        public void CanChainPromiseAndConvertTypeOfValue()
        {
            var promise = new Promise<int>();
            var chainedPromise = new Promise<string>();

            const int promisedValue = 15;
            const string chainedPromiseValue = "blah";
            var completed = 0;

            promise
                .Then(v => chainedPromise)
                .Then(v =>
                {
                    Assert.AreEqual(chainedPromiseValue, v);
                    ++completed;
                });

            promise.Resolve(promisedValue);
            chainedPromise.Resolve(chainedPromiseValue);

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void CanChainPromiseAndConvertToNonValuePromise()
        {
            var promise = new Promise<int>();
            var chainedPromise = new Promise();

            const int promisedValue = 15;
            var completed = 0;

            promise
                .Then(v => chainedPromise)
                .Then(() =>
                {
                    ++completed;
                });

            promise.Resolve(promisedValue);
            chainedPromise.Resolve();

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void ExceptionThrownInChainRejectsResultingPromise()
        {
            var promise = new Promise<int>();
            var chainedPromise = new Promise<string>();

            var ex = new Exception();
            var errors = 0;

            promise
                .Then<IPromise<string>>(v =>
                {
                    throw ex;
                })
                .Catch(e =>
                {
                    Assert.AreEqual(ex, e);
                    ++errors;
                });

            promise.Resolve(15);

            Assert.AreEqual(1, errors);
        }

        [Test]
        public void RejectionOfSourcePromiseRejectsChainedPromise()
        {
            var promise = new Promise<int>();
            var chainedPromise = new Promise<string>();

            var ex = new Exception();
            var errors = 0;

            promise
                .Then(v => chainedPromise)
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
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            var resolved = 0;

            Promise<int>
                .Race(promise1, promise2)
                .Then(i => resolved = i);

            promise1.Resolve(5);

            Assert.AreEqual(5, resolved);
        }

        [Test]
        public void RaceIsResolvedWhenSecondPromiseIsResolvedFirst()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            var resolved = 0;

            Promise<int>
                .Race(promise1, promise2)
                .Then(i => resolved = i);

            promise2.Resolve(12);

            Assert.AreEqual(12, resolved);
        }

        [Test]
        public void RaceIsRejectedWhenFirstPromiseIsRejectedFirst()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            Exception ex = null;

            Promise<int>
                .Race(promise1, promise2)
                .Catch(e => ex = e);

            var expected = new Exception();
            promise1.Reject(expected);

            Assert.AreEqual(expected, ex);
        }

        [Test]
        public void RaceIsRejectedWhenSecondPromiseIsRejectedFirst()
        {
            var promise1 = new Promise<int>();
            var promise2 = new Promise<int>();

            Exception ex = null;

            Promise<int>
                .Race(promise1, promise2)
                .Catch(e => ex = e);

            var expected = new Exception();
            promise2.Reject(expected);

            Assert.AreEqual(expected, ex);
        }

        [Test]
        public void CanResolvePromiseViaResolverFunction()
        {
            var promise = new Promise<int>((resolve, reject) => resolve(5));

            var completed = 0;
            promise.Then(v =>
            {
                Assert.AreEqual(5, v);
                ++completed;
            });

            Assert.AreEqual(1, completed);
        }

        [Test]
        public void CanRejectPromiseViaRejectFunction()
        {
            var ex = new Exception();
            var promise = new Promise<int>((resolve, reject) => reject(ex));

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
            var promise = new Promise<int>((resolve, reject) =>
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
            var promise = new Promise<int>();
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
                    .Then(a =>
                    {
                        throw ex;
                    })
                    .Done();

                promise.Resolve(5);

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
            var promise = new Promise<int>();
            var ex = new Exception();
            var eventRaised = 0;

            EventHandler<ExceptionEventArgs> handler = (s, e) => ++eventRaised;

            Promise.UnhandledException += handler;

            try
            {
                promise
                    .Then(a =>
                    {
                        throw ex;
                    })
                    .Catch(_ =>
                    {
                        // Catch the error.
                    })
                    .Done();

                promise.Resolve(5);

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
            var promise = new Promise<int>();
            var callback = 0;
            const int expectedValue = 5;

            promise.Done(value =>
            {
                Assert.AreEqual(expectedValue, value);
                ++callback;
            });

            promise.Resolve(expectedValue);

            Assert.AreEqual(1, callback);
        }

        [Test]
        public void CanHandleDoneOnResolvedWithOnReject()
        {
            var promise = new Promise<int>();
            var callback = 0;
            var errorCallback = 0;
            const int expectedValue = 5;

            promise.Done(
                value =>
                {
                    Assert.AreEqual(expectedValue, value);

                    ++callback;
                },
                ex =>
                {
                    ++errorCallback;
                }
            );

            promise.Resolve(expectedValue);

            Assert.AreEqual(1, callback);
            Assert.AreEqual(0, errorCallback);
        }

        /*todo:
         * Also want a test that exception thrown during Then triggers the error handler.
         * How do Javascript promises work in this regard?
        [Test]
        public void exception_during_Done_onResolved_triggers_error_hander()
        {
            var promise = new Promise<int>();
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
            var promise = new Promise<int>();
            var callback = 0;
            var errorCallback = 0;
            var expectedException = new Exception();

            promise
                .Then(value =>
                {
                    throw expectedException;

                    return Promise<int>.Resolved(10);
                })
                .Done(
                    value =>
                    {
                        ++callback;
                    },
                    ex =>
                    {
                        Assert.AreEqual(expectedException, ex);

                        ++errorCallback;
                    }
                );

            promise.Resolve(6);

            Assert.AreEqual(0, callback);
            Assert.AreEqual(1, errorCallback);
        }
    }
}
