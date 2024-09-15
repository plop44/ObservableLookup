using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ObservableLookup.Implementation2;

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
    private readonly bool _isReplayingLast;
    private readonly ConcurrentDictionary<TKey, SubjectBase<TSource>> _keyedSubjects = new();
    private bool _isCompleted;

    internal ObservableLookup(IObservable<TSource> input, Func<TSource, TKey> keySelector, bool isReplayingLast = false)
    {
        _isReplayingLast = isReplayingLast;
        var inputConnectable = input.Publish();

        inputConnectable
            .GroupBy(keySelector)
            .Subscribe(t => t.Subscribe(GetOrAdd(t.Key)).DisposeWith(_compositeDisposable),
                OnComplete);


        _compositeDisposable.Add(inputConnectable.Connect());
    }

    public IObservable<TSource> this[TKey key]
    {
        get
        {
            if (_compositeDisposable.IsDisposed)
                return Observable.Empty<TSource>();

            if (_isCompleted) return _keyedSubjects.TryGetValue(key, out var existing) ? existing : Observable.Empty<TSource>();

            return GetOrAdd(key);
        }
    }

    public void Dispose()
    {
        _compositeDisposable.Dispose();
    }

    private void OnComplete()
    {
        _isCompleted = true;
        foreach (var keyedSubject in _keyedSubjects) keyedSubject.Value.OnCompleted();
    }

    private SubjectBase<TSource> GetOrAdd(TKey key)
    {
        return _keyedSubjects.GetOrAdd(key, t => _isReplayingLast ? new ReplaySubject<TSource>(1) : new Subject<TSource>());
    }
}