# ObservableLookup

**ObservableLookup** is an Rx operator that splits an observable into keyed sub-observables, which can be accessed in O(1) time. It extends reactive streams with the ability to group values dynamically while maintaining efficient access to each group.

### Example 1 - ToObservableLookup

```
var observable = Observable.Range(0, 30).Publish();

var observableLookup = observable.ToObservableLookup(t => t % 10);

observableLookup[3].Materialize().Subscribe(Console.WriteLine);

observable.Connect();
```

Output

```
OnNext(3)
OnNext(13)
OnNext(23)
OnCompleted()
```

### Example 2 - ToObservableLookupReplayingLast

```
var observable = Observable.Range(0, 30).Concat(Observable.Never<int>()).Publish();

var observableLookup = observable.ToObservableLookupReplayingLast(t => t % 10);

observable.Connect();

// we subscribe after all values get played
observableLookup[3].Materialize().Subscribe(Console.WriteLine);
```

Output
```
OnNext(23)
```

The `ObservableLookup` project provides the main, optimized implementation that should be used. 
In contrast, `ObservableLookup.Experiments` offers an alternative version, where we attempt to build the same functionality using only existing Rx operators. However, this experimental version has significantly higher complexity.
