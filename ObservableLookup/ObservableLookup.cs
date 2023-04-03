using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ObservableLookup
{
    public static class ObservableLookupExtensions
    {
        public static ObservableLookup<TKey, TElement> ToObservableLookup<TKey, TElement>(this IObservable<IGroupedObservable<TKey, TElement>> input) where TKey : notnull => new(input, _ => new Subject<TElement>());
        public static ObservableLookup<TKey, TElement> ToObservableLookupReplayingLast<TKey, TElement>(this IObservable<IGroupedObservable<TKey, TElement>> input) where TKey : notnull => new(input, _ => new ReplaySubject<TElement>(1));
    }

    public class ObservableLookup<TKey, TElement> where TKey : notnull
    {
        private readonly IObservable<IGroupedObservable<TKey, TElement>> _input;
        private readonly Func<TKey, ISubject<TElement>> _subjectFactory;
        private readonly ConcurrentDictionary<TKey, ISubject<TElement>> _subjects = new();
        private bool _disposed;

        public ObservableLookup(IObservable<IGroupedObservable<TKey, TElement>> input, Func<TKey, ISubject<TElement>> subjectFactory)
        {
            _input = input;
            _subjectFactory = subjectFactory;
        }

        public IDisposable Connect()
        {
            var subscription = _input.Subscribe(OnNextGroup, onCompleted: OnComplete);

            return new CompositeDisposable(subscription, Disposable.Create(OnDispose));
        }

        private void OnDispose()
        {
            if (_disposed) return;

            _disposed = true;
            foreach (var subject in _subjects.Values)
            {
                subject.OnCompleted();
                (subject as IDisposable)?.Dispose();
            }
        }

        private void OnComplete()
        {
            foreach (var subject in _subjects.Values)
            {
                subject.OnCompleted();
            }
        }

        private void OnNextGroup(IGroupedObservable<TKey, TElement> element)
        {
            element.Subscribe(_subjects.GetOrAdd(element.Key, t => _subjectFactory.Invoke(t)));
        }

        public IObservable<TElement> this[TKey key]
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException("This is disposed");

                return _subjects.GetOrAdd(key, t => _subjectFactory.Invoke(t));
            }
        }
    }
}