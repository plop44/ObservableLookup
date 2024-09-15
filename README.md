ObservableLookup is an Rx operator to split an observable into GroupedObservables that can be accessed in 0(1) time.

### Example 1 - ToObservableLookup

```
var observable = Enumerable.Range(0, 30).ToObservable().Publish();

var observableLookup = observable.ToObservableLookup(t => t % 10);
observableLookup[3].Materialize().Subscribe(Console.WriteLine);

observable.Connect();
```

Output

```OnNext(3)
OnNext(13)
OnNext(23)
OnCompleted()
```

### Example 2 - ToObservableLookupReplayingLast

```
var observable = Enumerable.Range(0, 30).ToObservable().Merge(Observable.Never<int>()).Publish();

var observableLookup = observable.ToObservableLookupReplayingLast(t => t % 10);

observable.Connect();

observableLookup[3].Materialize().Subscribe(Console.WriteLine);
```

Output
```
OnNext(23)
```
