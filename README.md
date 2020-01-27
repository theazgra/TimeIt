# TimeIt

Application to measure execution time of another application.
- Inspired by unix `time` command and old windows `TIMEIT` application.

Usage:
```
TimeIt.exe filename [arguments]
```

As of current version:
 - Wall time, Kernel time and User time is reported.
    ```
    λ timeit Test.exe
    Wall time:      0sec 297ms
    Kernel time:    0sec 15ms
    User time:      0sec 15ms
    ```
 - `stdout` and `stderr` are by default redirected to the console.
 - Executed applications with theirs arguments are logged with time to TimeItLog.txt