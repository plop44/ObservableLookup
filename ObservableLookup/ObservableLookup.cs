using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ObservableLookup;

public static class ObservableLookupExtensions
{
    public static ObservableLookup<TSource, TKey> ToObservableLookup<TSource, TKey>(this IObservable<TSource> input, Func<TSource, TKey> keySelector) where TKey : notnull
    {
        return new(input, keySelector);
    }

    public static ObservableLookup<TSource, TKey> ToObservableLookupReplayingLast<TSource, TKey>(this IObservable<TSource> input, Func<TSource, TKey> keySelector) where TKey : notnull
    {
        return new(input, keySelector, true);
    }
}

public class ObservableLookup<TSource, TKey> : IDisposable where TKey : notnull
{
    private readonly CompositeDisposable _compositeDisposable = new();
    private readonly Subject<(TKey, Action<IObservable<TSource>>)> _subscriptions = new();

    internal ObservableLookup(IObservable<TSource> input, Func<TSource, TKey> keySelector, bool isReplayingLast = false)
    {
        var inputConnectable = input.Publish();
        var sourceModified = inputConnectable.Select(t => new ValueOrAction(keySelector(t), t, default, true));
        var subscriptionsModified = _subscriptions.Select(t => new ValueOrAction(t.Item1, default, t.Item2, false));

        var completeObservable = inputConnectable.StartWith(default(TSource)).LastAsync();

        var subscription = sourceModified
            .Merge(subscriptionsModified)
            .TakeUntil(completeObservable)
            .GroupBy(t => t.Key)
            .Select(t => t.Publish(t2 =>
            {
                var subscriptions = t2.Where(t3 => !t3.IsValue).Select(t3 => t3.Subscription ?? throw new Exception("Expecting a Subscription"));
                var values = t2.Where(t3 => t3.IsValue).Select(t3 => t3.Value!);

                if (isReplayingLast)
                {
                    var connectableObservable = values.Replay(1);
                    values = connectableObservable;
                    connectableObservable.Connect();
                }

                return subscriptions.Select(action => (action, values: values.AsObservable()));
            }))
            .Merge()
            .Subscribe(t => { t.action.Invoke(t.values); });

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
                _subscriptions.OnNext((key, t => { compositeDisposable.Add(t.Subscribe(o)); }));

                return compositeDisposable;
            });
        }
    }

    public void Dispose()
    {
        _compositeDisposable.Dispose();
    }

    private record struct ValueOrAction(TKey Key, TSource? Value, Action<IObservable<TSource>>? Subscription, bool IsValue);
}