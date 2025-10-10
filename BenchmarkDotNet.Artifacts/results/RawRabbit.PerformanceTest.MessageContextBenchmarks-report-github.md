```

BenchmarkDotNet v0.14.0, Arch Linux
11th Gen Intel Core i5-1145G7 2.60GHz, 1 CPU, 8 logical and 4 physical cores
  [Host]     : .NET 9.0.9 (9.0.925.41916), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-GQUJRG : .NET 9.0.9 (9.0.925.41916), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Toolchain=.NET 9.0  

```
| Method                     | Mean     | Error     | StdDev    | Median   |
|--------------------------- |---------:|----------:|----------:|---------:|
| MessageContext_FromFactory | 2.176 ms | 0.1905 ms | 0.5587 ms | 2.021 ms |
| MessageContext_None        | 2.347 ms | 0.2296 ms | 0.6771 ms | 2.281 ms |
