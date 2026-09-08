using BenchmarkDotNet.Attributes;

namespace JitResearch;

/// <summary>
/// Задание 4 (дополнительно). Формальный бенчмарк через BenchmarkDotNet —
/// библиотеку, установленную командой "dotnet add package BenchmarkDotNet"
/// согласно методичке.
///
/// Важно: BenchmarkDotNet намеренно исключает время JIT-компиляции из
/// итоговых цифр — перед замерами он выполняет фазу "Pilot"/"Warmup",
/// прогревая метод. Поэтому здесь сравнивается не "холодный старт",
/// а именно установившаяся производительность разных способов
/// суммирования массива — то, ради чего BenchmarkDotNet и создавался.
/// Сравнение "холодного" и "прогретого" вызова выполняется отдельно,
/// вручную через Stopwatch, в ColdStartDemo.
/// </summary>
[MemoryDiagnoser]
public class ArraySumBenchmarks
{
    private int[] _data = Array.Empty<int>();

    [Params(10_000)]
    public int Size;

    [GlobalSetup]
    public void Setup()
    {
        _data = new int[Size];
        for (int i = 0; i < Size; i++)
            _data[i] = i + 1;
    }

    [Benchmark(Baseline = true)]
    public long LoopSum()
    {
        long sum = 0;
        for (int i = 0; i < _data.Length; i++)
            sum += _data[i];
        return sum;
    }

    [Benchmark]
    public long LinqSum() => _data.Sum(x => (long)x);

    [Benchmark]
    public long SpanSum()
    {
        long sum = 0;
        foreach (int x in _data.AsSpan())
            sum += x;
        return sum;
    }
}
