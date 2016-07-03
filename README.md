# Results

[![Build status](https://ci.appveyor.com/api/projects/status/pphkda8eq81bqc9w?svg=true)](https://ci.appveyor.com/project/gmich/results)
[![Nuget downloads](https://img.shields.io/nuget/v/gmich.results.svg)](https://www.nuget.org/packages/gmich.results/)

A lightweight functional results library for synchronous chained code execution.

##Motivation

Something can always go wrong in a program, a faulty database transaction, a network request timeout, or some bad input. Sometimes an exception is thrown, sometimes the function returns a null value, the list goes on and on, in other words: when a function is evaluated it can either: **succeed** or **fail**. 

The result library adds some extra information to the return value:
* a `State` enum that represents the executed function's state
* an error message. 

The flow of execution can continue by chaining functions, for example with `.OnSuccess(this Result<TValue> result, Action<TValue> action)` the value can be accessed **only on successful evaluation** through a delegate.

##Quickstart

The Results library is available as a [NuGet package](https://www.nuget.org/packages/Gmich.Results/). You can install it using the NuGet Package Console window:

    PM> Install-Package Gmich.Results

##Usage

The usage is pretty straightforward. Results are instantiated through static factories in the `Result` class. Then the results can be chained with a variety of extensions.

###Create a new result

**Non-generic**

```
  Result.Ok()
  
  Result.FailWith(State.Error,"This is an error message")
```

**Generic**

```
  Result.Ok("this was a success")
  
  Result.FailWith<string>(State.Error,"This is an error message")
```

 **Under condition**
 
 ```
  Result.RequireNotNull(()=>"this should not be null")
    
  Result.Test(true); //true success / false failure
    
  Result.Try(()=>"this is executed in a try catch block);
```

**Examples**

A method that returns `Result<int>`.
```
  Result<int> Division(int dividend, int divisor) => 
     (divisor == 0)? 
      Result.FailWith<int>(State.Error, "Divide by zero error") : Result.Ok(dividend / divisor);
```

The same method without the use of `Result.Ok()`. The return value is implicitly casted to `Result<int>`.
```
  Result<int> Division(int dividend, int divisor) => 
     (divisor == 0)? 
      Result.FailWith<int>(State.Error, "Divide by zero error") : (dividend / divisor);
```

###Chaining the results

```
  Division(dividend,divisor)
  .LogOnFailure(logger.Error) // logs the error message and state 
  .Ensure(num => num > 0) // tests the result through a predicate
  .OnSuccess(result=> {} // do something with the successful result)
  .OnFailure(result=> {} // do something with the failed result)
  .OnBoth(result=> {} // do something either way);

```

**Transform values**

```
  Result.Ok("string result")
  .OnSuccess(str=> Result.Ok(10)) // the value is transformed to integer
  .OnSuccess(number=> Result.Ok("back to string"));
```

```
  Result.Ok("string result")
  .OnSuccess(str=> {} //do something with the successful result)
  .As<int>(); //transform it to a Result<int>
```

**Linq**

```
  IEnumerableOfResults()
  .FilterSuccessful();
```

```
  IEnumerableOfResults()
  .ForEach(res=> {} //do something with the successful results);  
```

```
  IEnumerableOfResults()
  .AnyFailures();
```

###Retries

The results can be retried with interval and retry count using the __Result.Retry__ overloads.

```
    Result
    .Retry(() => Result.RequireNotNull(repository.FetchSomething()), 
           interval: TimeSpan.FromSeconds(20),
           count: 3);
```

###Future results

The future results are results that can be chained, act as logic gates _(AND, OR, NOT)_ and are lazy evaluated. The performance overhead is minimal because the future results use __Expressions__.

**Creation**

The instantiation is through a static class factory.

```
    FutureResult.For(() => Result.Try(() => repository.FetchSomething()));
```

**Chaining**

The chaining is through extension methods that are named and act as logic gates.  

```
    //build a chain
    var resultChain = 
        firstFutureResult.And(second).And(third)
        .Or(fourth)
        .And(fifth)
        .Not;
    
    resultChain
    .Result // evaluate it
    .OnSuccess(res => {} )
    .OnFailure(res => {} );
```


