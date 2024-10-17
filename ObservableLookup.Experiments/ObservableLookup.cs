using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ObservableLookup.Experiments;

public static class ObservableLookupExtensions
{
    public static ObservableLookup<TSource, TKey> ToObservableLookup<TSource, TKey>(this IObservable<TSource> input, Func<TSource, TKey> keySelector) where TKey : notnull
    {
        return new ObservableLookup<TSource, TKey>(input, keySelector);
    }

    public static ObservableLookup<TSource, TKey> ToObservableLookupReplayingLast<TSource, TKey>(this IObservable<TSource> input, Func<TSource, TKey> keySelector) where TKey : notnull
    {
        return new ObservableLookup<TSource, TKey>(input, keySelector, true);
    }
}

public class ObservableLookup<TSource, TKey> : IDisposable where TKey : notnull
{
    private readonly CompositeDisposable _compositeDisposable = new();
    private readonly Subject<(TKey, IObserver<TSource>, CompositeDisposable)> _subscriptions = new();

    internal ObservableLookup(IObservable<TSource> input, Func<TSource, TKey> keySelector, bool isReplayingLast = false)
    {
        var inputConnectable = input.Publish();
        var inputAsValueOrActions = inputConnectable.Select(t => new ValueOrAction(keySelector(t), t, default, default, true));
        var subscriptionsAsValueOrActions = _subscriptions.Select(t => new ValueOrAction(t.Item1, default, t.Item2, t.Item3, false));

        var completeObservable = inputConnectable.Catch(Observable.Empty<TSource>()).LastOrDefaultAsync();

        var subscription = inputAsValueOrActions
            .Merge(subscriptionsAsValueOrActions)
            .GroupBy(t => t.Key)
            .Select(t => t.Publish(t2 =>
            {
                var subscriptions = t2.Where(t3 => !t3.IsValue).Select(t3 => (observer: t3.SubscriptionObserver ?? throw new Exception("Expecting a Subscription"), disposable: t3.SubscriptionDisposable ?? throw new Exception("Expecting a Subscription")));
                var values = t2.Where(t3 => t3.IsValue).Select(t3 => t3.Value!);

                if (isReplayingLast)
                {
                    var replaySubject = new ReplaySubject<TSource>(1);
                    var connectableObservable = values.Multicast(replaySubject);
                    values = connectableObservable;
                    completeObservable.Subscribe(_ => replaySubject.OnCompleted());
                    connectableObservable.Connect();
                }
                else
                {
                    var connectableObservable = values.TakeUntil(completeObservable).Publish();
                    values = connectableObservable;
                    connectableObservable.Connect();
                }

                return subscriptions.Select(action => (action, values));
            }))
            .Merge()
            .Subscribe(t =>
                {
                    var subscription = t.values.Subscribe(t.action.observer);
                    t.action.disposable.Add(subscription);
                },
                e =>
                {
                    // error are getting passed to downstream subjects
                });

        _compositeDisposable.Add(subscription);
        _compositeDisposable.Add(inputConnectable.Connect());
    }

    public IObservable<TSource> this[TKey key]
    {
        get
        {
            if (_compositeDisposable.IsDisposed)
                return Observable.Empty<TSource>();

            return Observable.Create<TSource>(o =>
            {
                var compositeDisposable = new CompositeDisposable();
                _subscriptions.OnNext((key, o, compositeDisposable));

                return compositeDisposable;
            });
        }
    }

    public void Dispose()
    {
        _compositeDisposable.Dispose();
    }

    private record struct ValueOrAction(TKey Key, TSource? Value, IObserver<TSource> SubscriptionObserver, CompositeDisposable SubscriptionDisposable, bool IsValue);
}