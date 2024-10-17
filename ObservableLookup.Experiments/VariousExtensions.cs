using System.Reactive.Disposables;

namespace ObservableLookup.Experiments;

internal static class VariousExtensions
{
    public static T DisposeWith<T>(this T disposable, CompositeDisposable compositeDisposable) where T : IDisposable
    {
        if (disposable == null) throw new ArgumentNullException(nameof(disposable));
        if (compositeDisposable == null) throw new ArgumentNullException(nameof(compositeDisposable));

        compositeDisposable.Add(disposable);
        return disposable;
    }
}