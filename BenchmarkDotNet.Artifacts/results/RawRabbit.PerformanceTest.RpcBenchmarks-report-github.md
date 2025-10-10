```

BenchmarkDotNet v0.14.0, Arch Linux
11th Gen Intel Core i5-1145G7 2.60GHz, 1 CPU, 8 logical and 4 physical cores
  [Host]     : .NET 9.0.9 (9.0.925.41916), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-GQUJRG : .NET 9.0.9 (9.0.925.41916), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Toolchain=.NET 9.0  

```
| Method    | Mean     | Error     | StdDev    |
|---------- |---------:|----------:|----------:|
| DirectRpc | 1.784 ms | 0.0356 ms | 0.0395 ms |
| NormalRpc | 2.147 ms | 0.0412 ms | 0.0385 ms |
