using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CampaignQueueMonitor.Utils;

public static class FuncExtensions
{
    public static T SuppressExceptions<T>(this Func<T> func, T defaultValue)
        => ExceptionUtils.SuppressExceptions(func, defaultValue);
}

public static class RangeExtensions
{
    public static int LengthInclusive(this Range range) => range.End.Value - range.Start.Value + 1;

    public static Enumerator GetEnumerator(this Range range) => new(range);
    
    public static int[] ToArrayInclusive(this Range range)
        => Enumerable.Range(range.Start.Value, range.End.Value).ToArray();

    public static IEnumerable<T> MapInclusive<T>(this Range range, Func<int, T> func)
        => range.ToArrayInclusive().Select(func);

    public struct Enumerator
    {
        private readonly int start;
        private readonly int end;

        public Enumerator(Range range)
        {
            start = Current = range.Start.Value - 1;
            end = range.End.Value - 1;
        }

        public bool MoveNext()
        {
            if (Current >= end)
            {
                return false;
            }

            Current++;
            return true;
        }

        public void Reset()
        {
            Current = start;
        }

        public int Current { get; private set; }
    }
}
