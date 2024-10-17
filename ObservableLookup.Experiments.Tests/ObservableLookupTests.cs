using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ObservableLookup.Experiments.Tests;

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
    public void WhenSubscribeThenOnNext()
    {
        var result = new List<int>();
        var subject = new Subject<int>();
        subject.ToObservableLookup(t => t % 4)[1].Subscribe(result.Add);
        subject.OnNext(1);
        Assert.That(result, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void WhenSubscribeThenOnNextButForWrongKey()
    {
        var result = new List<int>();
        var subject = new Subject<int>();
        subject.ToObservableLookup(t => t % 4)[2].Subscribe(result.Add);
        subject.OnNext(1);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void WhenSubscribeThenOnNextButWeDisposedSubscription()
    {
        var result = new List<int>();
        var subject = new Subject<int>();
        subject.ToObservableLookup(t => t % 4)[1].Subscribe(result.Add).Dispose();
        subject.OnNext(1);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void WhenSubscribeThenOnNextButWeDisposedObservableLookup()
    {
        var result = new List<int>();
        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookup(t => t % 4);
        observableLookup[1].Subscribe(result.Add);
        observableLookup.Dispose();
        subject.OnNext(1);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void WhenOnNextThenSubscribe()
    {
        var result = new List<int>();
        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookup(t => t % 4);
        subject.OnNext(1);
        observableLookup[1].Subscribe(result.Add);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void WhenOnNextThenSubscribeWhileReplayingLast()
    {
        var result = new List<int>();
        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookupReplayingLast(t => t % 4);
        subject.OnNext(1);
        observableLookup[1].Subscribe(result.Add);
        Assert.That(result, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void WhenWeMissTheFirstValueForKey()
    {
        var result = new List<int>();
        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookup(t => t % 4);
        subject.OnNext(1);
        observableLookup[1].Subscribe(result.Add);
        subject.OnNext(5);
        Assert.That(result, Is.EqualTo(new[] { 5 }));
    }

    [Test]
    public void WhenWeMissTheFirstValueForKeyWhileReplayingLast()
    {
        var result = new List<int>();
        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookupReplayingLast(t => t % 4);
        subject.OnNext(1);
        observableLookup[1].Subscribe(result.Add);
        subject.OnNext(5);
        Assert.That(result, Is.EqualTo(new[] { 1, 5 }));
    }

    [Test]
    public void WhenMultipleOnNextThenSubscribeWhileReplayingLast()
    {
        var result = new List<int>();
        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookupReplayingLast(t => t % 4);
        subject.OnNext(1);
        subject.OnNext(5);
        observableLookup[1].Subscribe(result.Add);
        Assert.That(result, Is.EqualTo(new[] { 5 }));
    }

    [Test]
    public void CompleteShouldPropagate()
    {
        bool isCompleted = false;

        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookup(t => t % 4);
        observableLookup[1].Subscribe(_ => { }, () => isCompleted = true);
        subject.OnCompleted();

        Assert.That(isCompleted, Is.True);
    }

    [Test]
    public void CompleteShouldPropagateEvenWhenSubscribingAfter()
    {
        bool isCompleted = false;

        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookup(t => t % 4);
        subject.OnCompleted();
        observableLookup[1].Subscribe(_ => { }, () => isCompleted = true);

        Assert.That(isCompleted, Is.True);
    }

    [Test]
    public void CompleteShouldPropagateWhileReplayingLast()
    {
        bool isCompleted = false;

        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookupReplayingLast(t => t % 4);
        observableLookup[1].Subscribe(_ => { }, () => isCompleted = true);
        subject.OnCompleted();

        Assert.That(isCompleted, Is.True);
    }

    [Test]
    public void CompleteShouldPropagateEvenWhenSubscribingAfterWhileReplayingLast()
    {
        bool isCompleted = false;

        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookupReplayingLast(t => t % 4);
        subject.OnCompleted();
        observableLookup[1].Subscribe(_ => { }, () => isCompleted = true);

        Assert.That(isCompleted, Is.True);
    }

    [Test]
    public void WhenOnNextThenCompleteThenSubscribe()
    {
        var result = new List<int>();
        bool isCompleted = false;

        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookup(t => t % 4);
        subject.OnNext(1);
        subject.OnCompleted();

        observableLookup[1].Subscribe(result.Add, () => isCompleted = true);

        // ASSERT
        Assert.That(result, Is.Empty);
        Assert.That(isCompleted, Is.True);
    }

    [Test]
    public void WhenOnNextThenCompleteThenSubscribeWhileReplayingLast()
    {
        var result = new List<int>();
        bool isCompleted = false;

        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookupReplayingLast(t => t % 4);
        subject.OnNext(1);
        subject.OnCompleted();

        observableLookup[1].Subscribe(result.Add, () => isCompleted = true);

        // ASSERT
        Assert.That(result, Is.EqualTo(new[] { 1 }));
        Assert.That(isCompleted, Is.True);
    }

    [Test]
    public void WhenOnNextThenCompleteThenSubscribeForANewKey()
    {
        var result = new List<int>();
        bool isCompleted = false;

        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookup(t => t % 4);
        subject.OnNext(1);
        subject.OnCompleted();

        observableLookup[0].Subscribe(result.Add, () => isCompleted = true);

        // ASSERT
        Assert.That(result, Is.Empty);
        Assert.That(isCompleted, Is.True);
    }

    [Test]
    public void WhenOnNextThenCompleteThenSubscribeForANewKeyWhileReplayingLast()
    {
        var result = new List<int>();
        bool isCompleted = false;

        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookupReplayingLast(t => t % 4);
        subject.OnNext(1);
        subject.OnCompleted();

        observableLookup[0].Subscribe(result.Add, () => isCompleted = true);

        // ASSERT
        Assert.That(result, Is.Empty);
        Assert.That(isCompleted, Is.True);
    }

    [Test]
    public void WhenWeDisposeTwice()
    {
        var observableLookup = new Subject<int>().ToObservableLookup(t => t % 4);
        observableLookup.Dispose();
        observableLookup.Dispose();

        Assert.Pass("Disposing twice did not throw");
    }

    [Test]
    public void WhenWeDisposeTwiceWhileReplayingLast()
    {
        var observableLookupReplayingLast = new Subject<int>().ToObservableLookupReplayingLast(t => t % 4);
        observableLookupReplayingLast.Dispose();
        observableLookupReplayingLast.Dispose();

        Assert.Pass("Disposing twice did not throw");
    }

    [Test]
    public void WhenWeSendLotOfDataToOurObservable()
    {
        var subject = new Subject<int>();
        var result = new List<int>();

        var observableLookup = subject
            // synchronize is needed below otherwise we can have a concurrent access to GroupBy below in the case input is used from multiple threads.
            .Synchronize()
            .ToObservableLookup(t => t % 10);

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

        var observableLookup = subject
            // synchronize is needed below otherwise we can have a concurrent access to GroupBy below in the case input is used from multiple threads.
            .Synchronize()
            .ToObservableLookupReplayingLast(t => t % 10);

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

        var observableLookup = subject
            // synchronize is needed below otherwise we can have a concurrent access to GroupBy below in the case input is used from multiple threads.
            .Synchronize()
            .ToObservableLookupReplayingLast(t => t % 10);

        // ACT
        Enumerable.Range(0, 10_000)
            .AsParallel().WithDegreeOfParallelism(6).ForAll(num => { subject.OnNext(num); });

        for (int i = 0; i < 10; i++)
        {
            observableLookup[i].Subscribe(result.Add);
        }

        Assert.That(result.Select(t => t % 10), Is.EquivalentTo(Enumerable.Range(0, 10).ToArray()));
    }

    [Test]
    public void WhenWeGotAnError()
    {
        var subject = new Subject<int>();

        var observableLookup = subject.ToObservableLookupReplayingLast(t => t % 10);

        Exception capturedException1 = null;
        Exception capturedException2 = null;
        observableLookup[0].Subscribe(_ => { }, error => { capturedException1 = error; });
        observableLookup[0].Subscribe(_ => { }, error => { capturedException2 = error; });

        // ACT
        var exception = new Exception();
        subject.OnError(exception);

        Assert.That(capturedException1, Is.EqualTo(exception));
        Assert.That(capturedException2, Is.EqualTo(exception));
    }

    [Test]
    public void WhenWeGotAnErrorButOneConsumerDoesNotCatchesIt()
    {
        var subject = new Subject<int>();

        var observableLookup = subject.ToObservableLookupReplayingLast(t => t % 10);

        Exception capturedException1 = null;
        observableLookup[0].Subscribe(_ => { }, error => { capturedException1 = error; });
        observableLookup[0].Subscribe(_ => { }/*, error => { capturedException2 = error; }*/);

        // ACT
        var exception = new Exception();
        Assert.Throws<Exception>(() => subject.OnError(exception));
    }
}