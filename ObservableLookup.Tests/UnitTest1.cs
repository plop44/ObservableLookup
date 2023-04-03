using System.Reactive.Linq;
using Microsoft.Reactive.Testing;

namespace ObservableLookup.Tests;

public class ObservableLookupTests
{
    [Test]
    public void WhenWeReceiveAMessage()
    {
        var testScheduler = new TestScheduler();

        var observable = testScheduler.CreateColdObservable(
            ReactiveTest.OnNext(100, 1));

        var observableLookup = observable.GroupBy(t => t % 4)
            .ToObservableLookup();

        var result = testScheduler.CreateObserver<int>();

        observableLookup[1].Subscribe(result);

        // ACT
        observableLookup.Connect();
        testScheduler.Start();

        // ASSERT
        ReactiveAssert.AreElementsEqual(new[] { ReactiveTest.OnNext(100, 1) }, result.Messages);
    }

    [Test]
    public void WhenWeReceiveAMessageButWithWrongKey()
    {
        var testScheduler = new TestScheduler();

        var observable = testScheduler.CreateColdObservable(
            ReactiveTest.OnNext(100, 1));

        var observableLookup = observable.GroupBy(t => t % 4)
            .ToObservableLookup();

        var result = testScheduler.CreateObserver<int>();

        observableLookup[3].Subscribe(result);

        // ACT
        observableLookup.Connect();
        testScheduler.Start();

        // ASSERT
        CollectionAssert.IsEmpty(result.Messages);
    }

    [Test]
    public void WhenWeReceiveAMessageButWeDisposedSubscription()
    {
        var testScheduler = new TestScheduler();

        var observable = testScheduler.CreateColdObservable(
            ReactiveTest.OnNext(100, 1));

        var observableLookup = observable.GroupBy(t => t % 4)
            .ToObservableLookup();

        var result = testScheduler.CreateObserver<int>();

        observableLookup[1].Subscribe(result);

        // ACT
        observableLookup.Connect().Dispose();
        testScheduler.Start();

        // ASSERT
        ReactiveAssert.AreElementsEqual(new[] { ReactiveTest.OnCompleted<int>(0) }, result.Messages);
    }

    [Test]
    public void WhenWeReceiveAMessageButWeDisposedSubscriptionThenSubscribe()
    {
        var testScheduler = new TestScheduler();

        var observable = testScheduler.CreateColdObservable(
            ReactiveTest.OnNext(100, 1));

        var observableLookup = observable.GroupBy(t => t % 4)
            .ToObservableLookup();

        var result = testScheduler.CreateObserver<int>();
        
        // ACT
        var disposable = observableLookup.Connect();
        testScheduler.Start();
        disposable.Dispose();
        Assert.Throws<ObjectDisposedException>(() => observableLookup[1].Subscribe(result));
    }

    [Test]
    public void WhenWeReceiveAMessageButWeDisposedSubscriptionThenSubscribe2()
    {
        var testScheduler = new TestScheduler();

        var observable = testScheduler.CreateColdObservable(
            ReactiveTest.OnNext(100, 1));

        var observableLookup = observable.GroupBy(t => t % 4)
            .ToObservableLookup();

        var result = testScheduler.CreateObserver<int>();

        // ACT
        var observableLookuped = observableLookup[1];
        var disposable = observableLookup.Connect();
        testScheduler.Start();
        disposable.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
        {
            observableLookuped.Subscribe(result);
        });
    }

    [Test]
    public void WhenWeReceiveAMessageButWeSubscribedAfterElementArrived()
    {
        var testScheduler = new TestScheduler();

        var observable = testScheduler.CreateColdObservable(
            ReactiveTest.OnNext(100, 1));

        var observableLookup = observable.GroupBy(t => t % 4)
            .ToObservableLookup();

        var result = testScheduler.CreateObserver<int>();

        // ACT
        observableLookup.Connect();
        testScheduler.Start();
        observableLookup[1].Subscribe(result);

        // ASSERT
        CollectionAssert.IsEmpty(result.Messages);
    }

    [Test]
    public void WhenWeReceiveAMessageButWeSubscribedAfterElementArrivedButWeUseReplay()
    {
        var testScheduler = new TestScheduler();

        var observable = testScheduler.CreateColdObservable(
            ReactiveTest.OnNext(100, 1));

        var observableLookup = observable.GroupBy(t => t % 4)
            .ToObservableLookupReplayingLast();

        var result = testScheduler.CreateObserver<int>();

        // ACT
        observableLookup.Connect();
        testScheduler.Start();
        observableLookup[1].Subscribe(result);

        // ASSERT
        ReactiveAssert.AreElementsEqual(new[] { ReactiveTest.OnNext(100, 1) }, result.Messages);
    }

    [Test]
    public void WhenWeReceiveAMessageButWeSubscribedAfterElementArrivedButWeUseReplay2()
    {
        var testScheduler = new TestScheduler();

        var observable = testScheduler.CreateColdObservable(
            ReactiveTest.OnNext(100, 1),ReactiveTest.OnNext(200, 5));

        var observableLookup = observable.GroupBy(t => t % 4)
            .ToObservableLookupReplayingLast();

        var result = testScheduler.CreateObserver<int>();

        // ACT
        observableLookup.Connect();
        testScheduler.Start();
        observableLookup[1].Subscribe(result);

        // ASSERT
        ReactiveAssert.AreElementsEqual(new[] { ReactiveTest.OnNext(200, 5) }, result.Messages);
    }

    [Test]
    public void WhenWeComplete()
    {
        var testScheduler = new TestScheduler();

        var observable = testScheduler.CreateColdObservable(
            ReactiveTest.OnCompleted<int>(400));

        var observableLookup = observable.GroupBy(t => t % 4)
            .ToObservableLookup();

        var result = testScheduler.CreateObserver<int>();

        observableLookup[1].Subscribe(result);

        // ACT
        observableLookup.Connect();
        testScheduler.Start();

        // ASSERT
        ReactiveAssert.AreElementsEqual(new[] { ReactiveTest.OnCompleted<int>(400) }, result.Messages);
    }

    [Test]
    public void WhenWeCompleteThenWeSubscribe()
    {
        var testScheduler = new TestScheduler();

        var observable = testScheduler.CreateColdObservable(
            ReactiveTest.OnCompleted<int>(400));

        var observableLookup = observable.GroupBy(t => t % 4)
            .ToObservableLookup();

        var result = testScheduler.CreateObserver<int>();

        // ACT
        observableLookup.Connect();
        testScheduler.Start();

        Assert.DoesNotThrow(() => observableLookup[1].Subscribe(result));

        // ASSERT
        CollectionAssert.IsEmpty(result.Messages);
    }

    [Test]
    public void WhenWeCompleteWhileReplayingLast()
    {
        var testScheduler = new TestScheduler();

        var observable = testScheduler.CreateColdObservable(ReactiveTest.OnNext(100, 1),
            ReactiveTest.OnCompleted<int>(400));

        var observableLookup = observable.GroupBy(t => t % 4)
            .ToObservableLookupReplayingLast();

        var result = testScheduler.CreateObserver<int>();

        // ACT
        observableLookup.Connect();
        testScheduler.Start();
        observableLookup[1].Subscribe(result);

        // ASSERT
        ReactiveAssert.AreElementsEqual(new[] { ReactiveTest.OnNext(400, 1), ReactiveTest.OnCompleted<int>(400) }, result.Messages);
    }

    [Test]
    public void WhenWeDisposeTwice()
    {
        var testScheduler = new TestScheduler();

        var observable = testScheduler.CreateColdObservable(
            ReactiveTest.OnNext(100, 1));

        var observableLookup = observable.GroupBy(t => t % 4)
            .ToObservableLookup();

        var result = testScheduler.CreateObserver<int>();

        observableLookup[1].Subscribe(result);

        // ACT
        var disposable = observableLookup.Connect();
        disposable.Dispose();
        
        // calling it a second time
        Assert.DoesNotThrow(disposable.Dispose);
    }
}