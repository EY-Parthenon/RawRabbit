using System;
using BenchmarkDotNet.Running;
using RawRabbit.PerformanceTest;

Console.WriteLine("RawRabbit .NET 9 Performance Benchmarks");
Console.WriteLine("=========================================\n");

BenchmarkSwitcher.FromAssembly(typeof(PubSubBenchmarks).Assembly).Run(args);
