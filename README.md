# Results

[![Build status](https://ci.appveyor.com/api/projects/status/pphkda8eq81bqc9w?svg=true)](https://ci.appveyor.com/project/gmich/results)

A lightweight functional results library for synchronous chained code execution.

##Motivation

##Quickstart

The Results library is available as a NuGet package. You can install it using the NuGet Package Console window:

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
  
###Chaining the results

```
  Result<int> Division(int dividend, int divisor) => 
     (divisor == 0)? 
      Result.FailWith<int>(State.Error, "Divide by zero error") : Result.Ok(dividend / divisor);
     
  Division(dividend,divisor)
  .OnSuccess(result=> {} //do something with the successful result)
  .OnFailure(result=> {} //do something with the failed result)
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
  ListOfResults()
  .FilterSuccessful();
```

```
  ListOfResults()
  .ForEach(res=> {} //do something with the successful results);  
```

```
  ListOfResults()
  .AnyFailures();
```
