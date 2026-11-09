using System.Collections;

namespace BBTAG_IM.Core.Meter;

internal sealed class LaneView(Func<int, MeterCell> cellAt, int count) : IReadOnlyList<MeterCell>
{
    public int Count => count;

    public MeterCell this[int index] => cellAt(index);

    public IEnumerator<MeterCell> GetEnumerator()
    {
        for (var index = 0; index < count; index++)
            yield return cellAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
