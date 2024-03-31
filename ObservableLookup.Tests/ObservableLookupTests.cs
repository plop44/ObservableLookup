using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;

namespace ObservableLookup.Tests;

public class ObservableLookupTests
{
    [Test]
    public void ReadMeCase1()
    {
        var observable = Enumerable.Range(0, 30).ToObservable().Publish();

        var observableLookup = observable.ToObservableLookup(t => t % 10);
        observableLookup[3].Materialize().Subscribe(Console.WriteLine);

        observable.Connect();
    }

    [Test]
    public void ReadMeCase2()
    {
        var observable = Enumerable.Range(0, 30).ToObservable().Merge(Observable.Never<int>()).Publish();

        var observableLookup = observable.ToObservableLookupReplayingLast(t => t % 10);

        observable.Connect();

        observableLookup[3].Materialize().Subscribe(Console.WriteLine);
    }

    [Test]
    public void WhenWeReceiveAMessage()
    {
        var testScheduler = new TestScheduler();

        var observableLookup = testScheduler.CreateHotObservable(ReactiveTest.OnNext(100, 1))
            .ToObservableLookup(t => t % 4);

        var result = testScheduler.CreateObserver<int>();

        testScheduler.Schedule(TimeSpan.FromTicks(0), (_, _) =>
        {
            observableLookup[1].Subscribe(result);
            return Disposable.Empty;
        });

        // ACT
        testScheduler.Start();

        // ASSERT
        ReactiveAssert.AreElementsEqual(new[] { ReactiveTest.OnNext(100, 1) }, result.Messages);
    }

    [Test]
    public void WhenWeReceiveAMessageButWeSubscribeToAnotherKey()
    {
        var testScheduler = new TestScheduler();

        var observableLookup = testScheduler.CreateHotObservable(ReactiveTest.OnNext(100, 1))
            .ToObservableLookup(t => t % 4);
        var result = testScheduler.CreateObserver<int>();

        testScheduler.Schedule(TimeSpan.FromTicks(0), (_, _) =>
        {
            observableLookup[3].Subscribe(result);
            return Disposable.Empty;
        });

        // ACT
        testScheduler.Start();

        // ASSERT
        CollectionAssert.IsEmpty(result.Messages);
    }

    [Test]
    public void WhenWeReceiveAMessageButWeDisposedSubscription()
    {
        var testScheduler = new TestScheduler();

        var observableLookup = testScheduler.CreateHotObservable(ReactiveTest.OnNext(100, 1))
            .ToObservableLookup(t => t % 4);
        var result = testScheduler.CreateObserver<int>();

        testScheduler.Schedule(TimeSpan.FromTicks(0), (_, _) =>
        {
            observableLookup[1].Subscribe(result);
            return Disposable.Empty;
        });

        testScheduler.Schedule(TimeSpan.FromTicks(50), (_, _) =>
        {
            observableLookup.Dispose();
            return Disposable.Empty;
        });

        // ACT
        testScheduler.Start();

        // ASSERT
        CollectionAssert.IsEmpty(result.Messages);
    }

    [Test]
    public void WhenWeReceiveAMessageButWeSubscribedAfterElementArrived()
    {
        var testScheduler = new TestScheduler();

        var observableLookup = testScheduler.CreateHotObservable(ReactiveTest.OnNext(100, 1))
            .ToObservableLookup(t => t % 4);

        // CreateHotObservable seems buggy, we need to call Start here, then later
        testScheduler.Start();

        var result = testScheduler.CreateObserver<int>();

        testScheduler.Schedule(TimeSpan.FromTicks(350), (_, _) =>
        {
            observableLookup[1].Subscribe(result);
            return Disposable.Empty;
        });

        // ACT
        testScheduler.Start();

        // ASSERT
        CollectionAssert.IsEmpty(result.Messages);
    }

    [Test]
    public void WhenWeReceiveAMessageButWeSubscribedAfterElementArrivedButWeUseReplay()
    {
        var testScheduler = new TestScheduler();

        var observableLookupReplayingLast = testScheduler.CreateHotObservable(ReactiveTest.OnNext(100, 1))
            .ToObservableLookupReplayingLast(t => t % 4);
        
        // CreateHotObservable seems buggy, we need to call Start here, then later
        testScheduler.Start();

        var result = testScheduler.CreateObserver<int>();

        testScheduler.Schedule(TimeSpan.FromTicks(350), (_, _) =>
        {
            observableLookupReplayingLast[1].Subscribe(result);
            return Disposable.Empty;
        });

        // ACT
        testScheduler.Start();

        // ASSERT
        ReactiveAssert.AreElementsEqual(new[] { ReactiveTest.OnNext(101, 1) }, result.Messages);
    }

    [Test]
    public void WhenWeReceiveAMessageButWeSubscribedAfterElementArrivedButWeUseReplay2()
    {
        var testScheduler = new TestScheduler();

        var observableLookupReplayingLast = testScheduler.CreateHotObservable(ReactiveTest.OnNext(100, 1)
                ,ReactiveTest.OnNext(200, 5))
            .ToObservableLookupReplayingLast(t => t % 4);

        // CreateHotObservable seems buggy, we need to call Start here, then later
        testScheduler.Start();

        var result = testScheduler.CreateObserver<int>();

        testScheduler.Schedule(TimeSpan.FromTicks(350), (_, _) =>
        {
            observableLookupReplayingLast[1].Subscribe(result);
            return Disposable.Empty;
        });

        // ACT
        testScheduler.Start();

        // ASSERT
        ReactiveAssert.AreElementsEqual(new[] { ReactiveTest.OnNext(201, 5) }, result.Messages);
    }

    [Test]
    public void WhenWeComplete()
    {
        var testScheduler = new TestScheduler();

        var observableLookup = testScheduler.CreateHotObservable(ReactiveTest.OnCompleted<int>(400))
            .ToObservableLookup(t => t % 4);
        
        var result = testScheduler.CreateObserver<int>();
        testScheduler.Schedule(TimeSpan.FromTicks(0), (_, _) =>
        {
            observableLookup[1].Subscribe(result);
            return Disposable.Empty;
        });

        // ACT
        testScheduler.Start();

        // ASSERT
        ReactiveAssert.AreElementsEqual(new[] { ReactiveTest.OnCompleted<int>(400) }, result.Messages);
    }

    [Test]
    public void WhenWeCompleteThenWeSubscribe()
    {
        var testScheduler = new TestScheduler();

        var observableLookup = testScheduler.CreateHotObservable(ReactiveTest.OnCompleted<int>(400))
            .ToObservableLookup(t => t % 4);

        var result = testScheduler.CreateObserver<int>();
        testScheduler.Schedule(TimeSpan.FromTicks(600), (_, _) =>
        {
            observableLookup[1].Subscribe(result);
            return Disposable.Empty;
        });

        // ACT
        testScheduler.Start();

        // ASSERT
        ReactiveAssert.AreElementsEqual(new[] { ReactiveTest.OnCompleted<int>(400) }, result.Messages);
    }

    [Test]
    public void WhenWeCompleteWhileReplayingLast()
    {
        var testScheduler = new TestScheduler();

        var observableLookupReplayingLast = testScheduler.CreateHotObservable(ReactiveTest.OnCompleted<int>(400))
            .ToObservableLookupReplayingLast(t => t % 4);

        var result = testScheduler.CreateObserver<int>();
        testScheduler.Schedule(TimeSpan.FromTicks(0), (_, _) =>
        {
            observableLookupReplayingLast[1].Subscribe(result);
            return Disposable.Empty;
        });

        // ACT
        testScheduler.Start();

        // ASSERT
        ReactiveAssert.AreElementsEqual(new[] { ReactiveTest.OnCompleted<int>(400) }, result.Messages);
    }

    [Test]
    public void WhenWeDisposeTwice()
    {
        var testScheduler = new TestScheduler();

        var observableLookup = testScheduler.CreateHotObservable(ReactiveTest.OnNext(100, 1))
            .ToObservableLookup(t => t % 4);

        testScheduler.Schedule(TimeSpan.FromTicks(150), (_, _) =>
        {
            observableLookup.Dispose();
            return Disposable.Empty;
        });

        testScheduler.Schedule(TimeSpan.FromTicks(250), (_, _) =>
        {
            observableLookup.Dispose();
            return Disposable.Empty;
        });


        // ACT
        testScheduler.Start();
        
        Assert.Pass("Disposing twice did not throw");
    }

    [Test]
    public void WhenWeSendLotOfDataToOurObservable()
    {
        var subject = new Subject<int>();
        var result = new List<int>();

        var observableLookup = subject.ToObservableLookup(t => t % 10);

        for (int i = 0; i < 10; i++)
        {
            observableLookup[i].Subscribe(result.Add);
        }

        // ACT
        Enumerable.Range(0, 10_000)
            .AsParallel().WithDegreeOfParallelism(6).ForAll(num =>
        {
            subject.OnNext(num);
        });

        Assert.That(result.OrderBy(t => t), Is.EquivalentTo(Enumerable.Range(0, 10_000).ToArray()));
    }

    [Test]
    public void WhenWeSendLotOfDataToOurObservableThatReplayLast()
    {
        var subject = new Subject<int>();
        var result = new List<int>();

        var observableLookup = subject.ToObservableLookupReplayingLast(t => t % 10);

        for (int i = 0; i < 10; i++)
        {
            observableLookup[i].Subscribe(result.Add);
        }

        // ACT
        Enumerable.Range(0, 10_000)
            .AsParallel().WithDegreeOfParallelism(6).ForAll(num =>
        {
            subject.OnNext(num);
        });

        Assert.That(result.OrderBy(t => t), Is.EquivalentTo(Enumerable.Range(0, 10_000).ToArray()));
    }

    [Test]
    public void WhenWeSendLotOfDataToOurObservableThatReplayLastBeforeSubscribing()
    {
        var subject = new Subject<int>();
        var result = new List<int>();

        var observableLookup = subject.ToObservableLookupReplayingLast(t => t % 10);

        // ACT
        Enumerable.Range(0, 10_000)
            .AsParallel().WithDegreeOfParallelism(6).ForAll(num => { subject.OnNext(num); });

        for (int i = 0; i < 10; i++)
        {
            observableLookup[i].Subscribe(result.Add);
        }

        Assert.That(result.Select(t => t % 10), Is.EquivalentTo(Enumerable.Range(0, 10).ToArray()));
    }
}