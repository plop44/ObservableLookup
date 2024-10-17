using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ObservableLookup;

public static class ObservableLookupExtensions
{
    public static ObservableLookup<TSource, TKey> ToObservableLookup<TSource, TKey>(this IObservable<TSource> input, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? equalityComparer = null) where TKey : notnull
    {
        return new ObservableLookup<TSource, TKey>(input, keySelector, () => new Subject<TSource>(), equalityComparer);
    }

    public static ObservableLookup<TSource, TKey> ToObservableLookupReplayingLast<TSource, TKey>(this IObservable<TSource> input, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? equalityComparer = null) where TKey : notnull
    {
        return new ObservableLookup<TSource, TKey>(input, keySelector, () => new ReplaySubject<TSource>(1), equalityComparer);
    }

    public static ObservableLookup<TSource, TKey> ToObservableLookupReplayingLastOrDefault<TSource, TKey>(this IObservable<TSource> input, Func<TSource, TKey> keySelector, TSource defaultValue, IEqualityComparer<TKey>? equalityComparer = null) where TKey : notnull
    {
        return new ObservableLookup<TSource, TKey>(input, keySelector, () => new BehaviorSubject<TSource>(defaultValue), equalityComparer);
    }
}

public class ObservableLookup<TSource, TKey> : IDisposable where TKey : notnull
{
    private readonly CompositeDisposable _compositeDisposable = new();
    private readonly Func<SubjectBase<TSource>> _subjectFactory;
    private readonly ConcurrentDictionary<TKey, SubjectBase<TSource>> _keyedSubjects;
    private bool _isCompleted;

    internal ObservableLookup(IObservable<TSource> input, Func<TSource, TKey> keySelector, Func<SubjectBase<TSource>> subjectFactory, IEqualityComparer<TKey>? equalityComparer = null)
    {
        _keyedSubjects = new(equalityComparer);
        _subjectFactory = subjectFactory;

        input
            .Subscribe(t => GetOrAdd(keySelector.Invoke(t)).OnNext(t),
                OnError,
                OnComplete)
            .DisposeWith(_compositeDisposable);
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
    
    private void OnError(Exception exception)
    {
        _isCompleted = true;
        foreach (var keyedSubject in _keyedSubjects) keyedSubject.Value.OnError(exception);
    }

    private SubjectBase<TSource> GetOrAdd(TKey key)
    {
        return _keyedSubjects.GetOrAdd(key, _ => _subjectFactory.Invoke());
    }
}