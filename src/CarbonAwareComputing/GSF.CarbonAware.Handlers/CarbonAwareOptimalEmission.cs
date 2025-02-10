using CarbonAware.Model;

namespace GSF.CarbonAware.Handlers;

internal static class CarbonAwareOptimalEmission
{
    public static IReadOnlyCollection<EmissionsData> GetOptimalEmissions(IReadOnlyCollection<EmissionsData> emissionsData)
    {
        var bestResult = emissionsData.MinBy(x => x.Rating);
        if (bestResult != null)
        {
            return emissionsData.Where(x => x.Rating == bestResult.Rating).ToArray();
        }

        return [];
    }

    static TSource? MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer = null)
    {
        comparer ??= Comparer<TKey>.Default;

        using IEnumerator<TSource> e = source.GetEnumerator();

        if (!e.MoveNext())
        {
            if (default(TSource) is null)
            {
                return default;
            }
            else
            {
                throw new InvalidOperationException("Struct element sequence is empty");
            }
        }

        var value = e.Current;
        var key = keySelector(value);

        if (default(TKey) is null)
        {
            while (key == null)
            {
                if (!e.MoveNext())
                {
                    return value;
                }

                value = e.Current;
                key = keySelector(value);
            }

            while (e.MoveNext())
            {
                var nextValue = e.Current;
                var nextKey = keySelector(nextValue);
                if (nextKey != null && comparer.Compare(nextKey, key) < 0)
                {
                    key = nextKey;
                    value = nextValue;
                }
            }
        }
        else
        {
            if (comparer == Comparer<TKey>.Default)
            {
                while (e.MoveNext())
                {
                    var nextValue = e.Current;
                    var nextKey = keySelector(nextValue);
                    if (Comparer<TKey>.Default.Compare(nextKey, key) < 0)
                    {
                        key = nextKey;
                        value = nextValue;
                    }
                }
            }
            else
            {
                while (e.MoveNext())
                {
                    var nextValue = e.Current;
                    var nextKey = keySelector(nextValue);
                    if (comparer.Compare(nextKey, key) < 0)
                    {
                        key = nextKey;
                        value = nextValue;
                    }
                }
            }
        }

        return value;
    }
}