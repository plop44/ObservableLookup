using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ObservableLookup.Tests;

public class ObservableLookupTests
{
    [Test]
    public void ReadMeCase1()
    {
        var observable = Observable.Range(0, 30).Publish();

        var observableLookup = observable.ToObservableLookup(t => t % 10);

        observableLookup[3].Materialize().Subscribe(Console.WriteLine);

        observable.Connect();
    }

    [Test]
    public void ReadMeCase2()
    {
        var observable = Observable.Range(0, 30).Concat(Observable.Never<int>()).Publish();

        var observableLookup = observable.ToObservableLookupReplayingLast(t => t % 10);

        observable.Connect();

        // we subscribe after all values get played
        observableLookup[3].Materialize().Subscribe(Console.WriteLine);
    }

    [Test]
    public void WhenSubscribeWithNoValuesProduced()
    {
        var result = new List<int>();
        Observable.Never<int>().ToObservableLookup(t => t % 4)[1].Subscribe(result.Add);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void WhenSubscribeWithNoValuesProducedWhileReplayingLast()
    {
        var result = new List<int>();
        Observable.Never<int>().ToObservableLookupReplayingLast(t => t % 4)[1].Subscribe(result.Add);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void WhenSubscribeWithNoValuesProducedWhileReplayingLastOrDefault()
    {
        var result = new List<int>();
        Observable.Never<int>().ToObservableLookupReplayingLastOrDefault(t => t % 4, 12345)[1].Subscribe(result.Add);
        Assert.That(result, Is.EqualTo(new[] { 12345 }));
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
    public void WhenOnNextThenSubscribeWhileReplayingLastOrDefault()
    {
        var result = new List<int>();
        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookupReplayingLastOrDefault(t => t % 4, 12345);
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
    public void WhenWeMissTheFirstValueForKeyWhileReplayingLastOrDefault()
    {
        var result = new List<int>();
        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookupReplayingLastOrDefault(t => t % 4, 12345);
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
    public void WhenMultipleOnNextThenSubscribeWhileReplayingLastOrDefault()
    {
        var result = new List<int>();
        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookupReplayingLastOrDefault(t => t % 4, 12345);
        subject.OnNext(1);
        subject.OnNext(5);
        observableLookup[1].Subscribe(result.Add);
        Assert.That(result, Is.EqualTo(new[] { 5 }));
    }

    [Test]
    public void CompleteShouldPropagate()
    {
        var isCompleted = false;

        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookup(t => t % 4);
        observableLookup[1].Subscribe(_ => { }, () => isCompleted = true);
        subject.OnCompleted();

        Assert.That(isCompleted, Is.True);
    }

    [Test]
    public void CompleteShouldPropagateEvenWhenSubscribingAfter()
    {
        var isCompleted = false;

        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookup(t => t % 4);
        subject.OnCompleted();
        observableLookup[1].Subscribe(_ => { }, () => isCompleted = true);

        Assert.That(isCompleted, Is.True);
    }

    [Test]
    public void CompleteShouldPropagateWhileReplayingLast()
    {
        var isCompleted = false;

        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookupReplayingLast(t => t % 4);
        observableLookup[1].Subscribe(_ => { }, () => isCompleted = true);
        subject.OnCompleted();

        Assert.That(isCompleted, Is.True);
    }

    [Test]
    public void CompleteShouldPropagateEvenWhenSubscribingAfterWhileReplayingLast()
    {
        var isCompleted = false;

        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookupReplayingLast(t => t % 4);
        subject.OnCompleted();
        observableLookup[1].Subscribe(_ => { }, () => isCompleted = true);

        Assert.That(isCompleted, Is.True);
    }

    [Test]
    public void CompleteShouldPropagateWhileReplayingLastOrDefault()
    {
        var isCompleted = false;

        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookupReplayingLastOrDefault(t => t % 4, 12345);
        observableLookup[1].Subscribe(_ => { }, () => isCompleted = true);
        subject.OnCompleted();

        Assert.That(isCompleted, Is.True);
    }

    [Test]
    public void CompleteShouldPropagateEvenWhenSubscribingAfterWhileReplayingLastOrDefault()
    {
        var isCompleted = false;

        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookupReplayingLastOrDefault(t => t % 4, 12345);
        subject.OnCompleted();
        observableLookup[1].Subscribe(_ => { }, () => isCompleted = true);

        Assert.That(isCompleted, Is.True);
    }

    [Test]
    public void WhenOnNextThenCompleteThenSubscribe()
    {
        var result = new List<int>();
        var isCompleted = false;

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
        var isCompleted = false;

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
    public void WhenOnNextThenCompleteThenSubscribeWhileReplayingLastOrDefault()
    {
        var result = new List<int>();
        var isCompleted = false;

        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookupReplayingLastOrDefault(t => t % 4, 12345);
        subject.OnNext(1);
        subject.OnCompleted();

        observableLookup[1].Subscribe(result.Add, () => isCompleted = true);

        // ASSERT
        Assert.That(result, Is.Empty, "Behavior subject does not replay last message after it completes. Replay subject does");
        Assert.That(isCompleted, Is.True);
    }

    [Test]
    public void WhenOnNextThenCompleteThenSubscribeForANewKey()
    {
        var result = new List<int>();
        var isCompleted = false;

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
        var isCompleted = false;

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
    public void WhenOnNextThenCompleteThenSubscribeForANewKeyWhileReplayingLastOrDefault()
    {
        var result = new List<int>();
        var isCompleted = false;

        var subject = new Subject<int>();
        var observableLookup = subject.ToObservableLookupReplayingLastOrDefault(t => t % 4, 12345);
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
    public void WhenWeDisposeTwiceWhileReplayingLastOrDefault()
    {
        var observableLookupReplayingLast = new Subject<int>().ToObservableLookupReplayingLastOrDefault(t => t % 4, 12345);
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

        for (var i = 0; i < 10; i++) observableLookup[i].Subscribe(result.Add);

        // ACT
        Enumerable.Range(0, 10_000)
            .AsParallel().WithDegreeOfParallelism(6).ForAll(num => { subject.OnNext(num); });

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

        for (var i = 0; i < 10; i++) observableLookup[i].Subscribe(result.Add);

        // ACT
        Enumerable.Range(0, 10_000)
            .AsParallel().WithDegreeOfParallelism(6).ForAll(num => { subject.OnNext(num); });

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

        for (var i = 0; i < 10; i++) observableLookup[i].Subscribe(result.Add);

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
        observableLookup[0].Subscribe(_ => { } /*, error => { capturedException2 = error; }*/);

        // ACT
        var exception = new Exception();
        Assert.Throws<Exception>(() => subject.OnError(exception));
    }

    [Test]
    public void WhenWeUseEqualityComparer()
    {
        var observable = Observable.Range(0, 100).Publish();
        var observableLookup = observable.ToObservableLookup(GetKeyInEnglish, StringComparer.OrdinalIgnoreCase);
        var resultA = new List<int>();
        var resultB = new List<int>();
        var resultC = new List<int>();

        observableLookup["One"].Subscribe(resultA.Add);
        observableLookup["ONE"].Subscribe(resultB.Add);
        observableLookup["one"].Subscribe(resultC.Add);

        observable.Connect();

        Assert.That(resultA, Is.EquivalentTo(new[] { 1, 11, 21, 31, 41, 51, 61, 71, 81, 91 }));
        Assert.That(resultB, Is.EquivalentTo(resultA));
        Assert.That(resultC, Is.EquivalentTo(resultA));
        Assert.That(resultA, Is.EquivalentTo(resultB));
        Assert.That(resultB, Is.EquivalentTo(resultC));
    }

    [Test]
    public void WhenWeUseEqualityComparerWhileReplayingLast()
    {
        var observable = Observable.Range(0, 100).Publish();
        var observableLookup = observable.ToObservableLookupReplayingLast(GetKeyInEnglish, StringComparer.OrdinalIgnoreCase);
        var resultA = new List<int>();
        var resultB = new List<int>();
        var resultC = new List<int>();

        observableLookup["One"].Subscribe(resultA.Add);
        observableLookup["ONE"].Subscribe(resultB.Add);
        observableLookup["one"].Subscribe(resultC.Add);

        observable.Connect();

        Assert.That(resultA, Is.EquivalentTo(new[] { 1, 11, 21, 31, 41, 51, 61, 71, 81, 91 }));
        Assert.That(resultB, Is.EquivalentTo(resultA));
        Assert.That(resultC, Is.EquivalentTo(resultA));
        Assert.That(resultA, Is.EquivalentTo(resultB));
        Assert.That(resultB, Is.EquivalentTo(resultC));
    }

    [Test]
    public void WhenWeUseEqualityComparerWhileReplayingLastOrDefault()
    {
        var observable = Observable.Range(0, 100).Publish();
        var observableLookup = observable.ToObservableLookupReplayingLastOrDefault(GetKeyInEnglish, 12345, StringComparer.OrdinalIgnoreCase);
        var resultA = new List<int>();
        var resultB = new List<int>();
        var resultC = new List<int>();

        observableLookup["One"].Subscribe(resultA.Add);
        observableLookup["ONE"].Subscribe(resultB.Add);
        observableLookup["one"].Subscribe(resultC.Add);

        observable.Connect();

        Assert.That(resultA, Is.EquivalentTo(new[] { 12345, 1, 11, 21, 31, 41, 51, 61, 71, 81, 91 }));
        Assert.That(resultB, Is.EquivalentTo(resultA));
        Assert.That(resultC, Is.EquivalentTo(resultA));
        Assert.That(resultA, Is.EquivalentTo(resultB));
        Assert.That(resultB, Is.EquivalentTo(resultC));
    }

    private string GetKeyInEnglish(int input)
    {
        var inputBounded = input % 10;
        return inputBounded switch
        {
            0 => "Zero",
            1 => "One",
            2 => "Two",
            3 => "Three",
            4 => "Four",
            5 => "Five",
            6 => "Six",
            7 => "Seven",
            8 => "Eight",
            9 => "Nine",
            _ => throw new ArgumentOutOfRangeException(nameof(input), "Input must be a non-negative integer.")
        };
    }
}