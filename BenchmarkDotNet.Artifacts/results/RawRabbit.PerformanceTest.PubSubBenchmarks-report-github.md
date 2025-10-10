```

BenchmarkDotNet v0.14.0, Arch Linux
11th Gen Intel Core i5-1145G7 2.60GHz, 1 CPU, 8 logical and 4 physical cores
  [Host]     : .NET 9.0.9 (9.0.925.41916), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-GQUJRG : .NET 9.0.9 (9.0.925.41916), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Toolchain=.NET 9.0  

```
| Method                       | Mean     | Error     | StdDev    | Median   |
|----------------------------- |---------:|----------:|----------:|---------:|
| ConsumerAcknowledgements_Off | 1.395 ms | 0.0671 ms | 0.1859 ms | 1.461 ms |
| ConsumerAcknowledgements_On  | 2.162 ms | 0.2195 ms | 0.6438 ms | 2.226 ms |
| DeliveryMode_NonPersistant   | 1.539 ms | 0.0733 ms | 0.2066 ms | 1.547 ms |
| DeliveryMode_Persistant      | 1.737 ms | 0.0600 ms | 0.1673 ms | 1.698 ms |
