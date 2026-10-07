// Derived from AndroidX Arrangement/Keylines/KeylineList/Strategy/KeylineSnapPosition,
// commit 11ece46a49d485c7644e53cb0684a611d7a0ec10, Copyright Android Open Source Project.
// Licensed under Apache-2.0; pinned source provenance is recorded in docs/components/m3-18.md.
namespace Avalonia.Material3.Controls;

// The item remains measured at its full focal width. Only its centered mask and translation change.
internal sealed class MaterialCarouselStrategy
{
    private readonly record struct Keyline(double Size, double Offset, double Unadjusted, bool Focal, bool Anchor, bool Pivot, double Cutoff);
    private readonly Keyline[] _default;
    private readonly List<Keyline[]> _start, _end;
    private readonly double[] _startPoints, _endPoints;
    private readonly Keyline[] _interpolated;
    private readonly double _startDistance, _endDistance, _space, _gap;
    internal double ItemWidth { get; }
    private static int FirstFocal(Keyline[] list) => Array.FindIndex(list, key => key.Focal);
    private static int LastFocal(Keyline[] list) => Array.FindLastIndex(list, key => key.Focal);
    private static int Pivot(Keyline[] list) => Array.FindIndex(list, key => key.Pivot);

    private MaterialCarouselStrategy(Keyline[] keylines, double space, double gap)
    {
        _default = keylines; _space = space; _gap = gap;
        ItemWidth = keylines[FirstFocal(keylines)].Size;
        _start = ShiftSteps(keylines, space, gap, true);
        _end = ShiftSteps(keylines, space, gap, false);
        _startDistance = Math.Max(0, _start[^1][0].Unadjusted - _start[0][0].Unadjusted);
        _endDistance = Math.Max(0, _end[0][^1].Unadjusted - _end[^1][^1].Unadjusted);
        _startPoints = ShiftPoints(_start, _startDistance, true);
        _endPoints = ShiftPoints(_end, _endDistance, false);
        _interpolated = new Keyline[keylines.Length];
    }

    internal static MaterialCarouselStrategy Create(MaterialCarouselLayout layout, double space, double preferred, double gap, int count)
    {
        // Zero-sized hosts retain a finite parked plan until a real viewport arrives.
        space = Math.Max(.001, space);
        var anchor = 10d;
        if (layout == MaterialCarouselLayout.Uncontained)
        {
            var large = Math.Min(preferred + gap, space);
            var largeCount = Math.Max(1, (int)Math.Floor(space / large));
            var remaining = space - largeCount * large;
            var medium = Math.Max(remaining * 1.5, anchor);
            if (medium > large * .85) medium = Math.Min(Math.Max(large * .85, remaining * 1.2), large);
            var arrangement = new Arrangement(0, 0, 0, medium, remaining > 0 ? 1 : 0, large, largeCount);
            return new(Aligned(space, gap, arrangement, Math.Max(Math.Min(anchor, preferred), medium * .5), anchor), space, gap);
        }
        var targetLarge = layout == MaterialCarouselLayout.Hero ? space : Math.Min(preferred, space);
        var targetSmall = Math.Clamp(targetLarge / 3, 40, 56);
        var targetMedium = (targetLarge + targetSmall) / 2;
        var smallCounts = new[] { layout == MaterialCarouselLayout.Hero && count <= 1 || space < (layout == MaterialCarouselLayout.Hero ? 90 : 80) ? 0 : 1 };
        var mediumCounts = layout == MaterialCarouselLayout.Hero ? new[] { 0 } : new[] { 1, 0 };
        var minimumSpace = space - (layout == MaterialCarouselLayout.Hero ? 40 * smallCounts.Max() : targetMedium * mediumCounts.Max() + 56 * smallCounts.Max());
        var minimumLarge = Math.Max(1, (int)Math.Floor(minimumSpace / targetLarge));
        var maximumLarge = Math.Max(minimumLarge, (int)Math.Ceiling(space / targetLarge));
        var largeCounts = Enumerable.Range(minimumLarge, maximumLarge - minimumLarge + 1).Reverse().ToArray();
        var fitted = FindArrangement(space, gap, targetSmall, smallCounts, targetMedium, mediumCounts, targetLarge, largeCounts);
        if (layout == MaterialCarouselLayout.MultiBrowse && fitted.Count > count)
        {
            var surplus = fitted.Count - count; var small = fitted.SmallCount; var medium = fitted.MediumCount;
            while (surplus-- > 0) { if (small > 0) small--; else if (medium > 1) medium--; }
            fitted = FindArrangement(space, gap, targetSmall, [small], targetMedium, [medium], targetLarge, largeCounts);
        }
        return new(Aligned(space, gap, fitted, anchor, anchor), space, gap);
    }

    private sealed record Arrangement(int Priority, double Small, int SmallCount, double Medium, int MediumCount, double Large, int LargeCount)
    {
        internal int Count => SmallCount + MediumCount + LargeCount;
        internal double Cost(double target) => LargeCount > 0 && SmallCount > 0 && (Large <= Small || MediumCount > 0 && (Large <= Medium || Medium <= Small))
            ? double.MaxValue : Math.Abs(target - Large) * Priority;
    }
    private static Arrangement FindArrangement(double space, double gap, double small, int[] smallCounts, double medium, int[] mediumCounts, double large, int[] largeCounts)
    {
        Arrangement? best = null; var priority = 1;
        foreach (var largeCount in largeCounts)
        foreach (var mediumCount in mediumCounts)
        foreach (var smallCount in smallCounts)
        {
            var available = space - (largeCount + mediumCount + smallCount - 1) * gap;
            var fittedSmall = Math.Clamp(small, 40, 56);
            var delta = available - large * largeCount - medium * mediumCount - fittedSmall * smallCount;
            if (smallCount > 0) fittedSmall += delta > 0 ? Math.Min(delta / smallCount, 56 - fittedSmall) : Math.Max(delta / smallCount, 40 - fittedSmall);
            else fittedSmall = 0;
            var fittedLarge = (available - (smallCount + mediumCount / 2d) * fittedSmall) / (largeCount + mediumCount / 2d);
            var fittedMedium = (fittedLarge + fittedSmall) / 2;
            if (mediumCount > 0 && fittedLarge != large)
            {
                var adjustment = (large - fittedLarge) * largeCount;
                var distributed = Math.Min(Math.Abs(adjustment), fittedMedium * .1 * mediumCount) * Math.Sign(adjustment);
                fittedMedium -= distributed / mediumCount; fittedLarge += distributed / largeCount;
            }
            var candidate = new Arrangement(priority++, fittedSmall, smallCount, fittedMedium, mediumCount, fittedLarge, largeCount);
            if (best is null || candidate.Cost(large) < best.Cost(large)) best = candidate;
            if (best.Cost(large) == 0) return best;
        }
        return best!;
    }
    private static Keyline[] Aligned(double space, double gap, Arrangement arrangement, double leftAnchor, double rightAnchor)
    {
        var list = new List<(double Size, bool Anchor)> { (leftAnchor, true) };
        for (var i = 0; i < arrangement.LargeCount; i++) list.Add((arrangement.Large, false));
        for (var i = 0; i < arrangement.MediumCount; i++) list.Add((arrangement.Medium, false));
        for (var i = 0; i < arrangement.SmallCount; i++) list.Add((arrangement.Small, false));
        list.Add((rightAnchor, true));
        return Build(list, space, gap, 1, arrangement.Large / 2);
    }
    private static Keyline[] Build(IReadOnlyList<(double Size, bool Anchor)> list, double space, double gap, int pivot, double pivotOffset)
    {
        var largest = list.Where(key => !key.Anchor).Max(key => key.Size);
        var first = Enumerable.Range(0, list.Count).First(i => !list[i].Anchor && list[i].Size == largest);
        var last = first;
        while (last < list.Count - 1 && list[last + 1].Size == largest) last++;
        var result = new Keyline[list.Count];
        static bool CutLeft(double size, double offset) => offset - size / 2 < 0 && offset + size / 2 > 0;
        static bool CutRight(double size, double offset, double extent) => offset - size / 2 < extent && offset + size / 2 > extent;
        var pivotSize = list[pivot].Size;
        var cutoff = CutLeft(pivotSize, pivotOffset) ? pivotOffset - pivotSize / 2 : CutRight(pivotSize, pivotOffset, space) ? pivotOffset + pivotSize / 2 - space : 0;
        result[pivot] = new(pivotSize, pivotOffset, pivotOffset, pivot >= first && pivot <= last, list[pivot].Anchor, true, cutoff);
        var offset = pivotOffset - largest / 2 - gap; var unadjusted = offset;
        for (var i = pivot - 1; i >= 0; i--)
        {
            var center = offset - list[i].Size / 2; var rawCenter = unadjusted - largest / 2;
            result[i] = new(list[i].Size, center, rawCenter, i >= first && i <= last, list[i].Anchor, false,
                CutLeft(list[i].Size, center) ? Math.Abs(center - list[i].Size / 2) : 0);
            offset -= list[i].Size + gap; unadjusted -= largest + gap;
        }
        offset = pivotOffset + largest / 2 + gap; unadjusted = offset;
        for (var i = pivot + 1; i < list.Count; i++)
        {
            var center = offset + list[i].Size / 2; var rawCenter = unadjusted + largest / 2;
            result[i] = new(list[i].Size, center, rawCenter, i >= first && i <= last, list[i].Anchor, false,
                CutRight(list[i].Size, center, space) ? center + list[i].Size / 2 - space : 0);
            offset += list[i].Size + gap; unadjusted += largest + gap;
        }
        return result;
    }
    private static List<Keyline[]> ShiftSteps(Keyline[] original, double space, double gap, bool start)
    {
        var steps = new List<Keyline[]> { original };
        var first = Array.FindIndex(original, key => !key.Anchor); var last = Array.FindLastIndex(original, key => !key.Anchor);
        var firstFocal = FirstFocal(original); var lastFocal = LastFocal(original);
        if (start ? original[firstFocal].Offset - original[firstFocal].Size / 2 >= 0 && firstFocal == first
            : original[lastFocal].Offset + original[lastFocal].Size / 2 <= space && lastFocal == last) return steps;
        var count = start ? firstFocal - first : last - lastFocal;
        if (count <= 0 && (start ? original[firstFocal] : original[lastFocal]).Cutoff > 0)
        { steps.Add(Move(original, 0, 0, space, gap)); return steps; }
        for (var i = 0; i < count; i++)
        {
            var previous = steps[^1];
            var source = start ? first : last;
            var originalIndex = start ? first + i : last - i;
            var destination = start ? original.Length - 1 : 0;
            if (start && originalIndex > 0)
            {
                var neighborSize = original[originalIndex - 1].Size;
                var match = Array.FindIndex(previous, LastFocal(previous), key => key.Size == neighborSize);
                destination = (match >= 0 ? match : previous.Length - 1) - 1;
            }
            else if (!start && originalIndex < original.Length - 1)
            {
                var neighborSize = original[originalIndex + 1].Size;
                var match = -1;
                for (var j = FirstFocal(previous) - 1; j >= 0; j--) if (previous[j].Size == neighborSize) { match = j; break; }
                destination = (match >= 0 ? match : 0) + 1;
            }
            steps.Add(Move(previous, source, destination, space, gap));
        }
        return steps;
    }
    private static Keyline[] Move(Keyline[] from, int source, int destination, double space, double gap)
    {
        var direction = source > destination ? 1 : -1;
        var pivotIndex = Pivot(from);
        var pivotOffset = from[pivotIndex].Offset + (from[source].Size - from[source].Cutoff + gap) * direction;
        var list = from.Select(key => (key.Size, key.Anchor)).ToList();
        var moved = list[source]; list.RemoveAt(source); list.Insert(destination, moved);
        return Build(list, space, gap, pivotIndex + direction, pivotOffset);
    }
    private static double[] ShiftPoints(List<Keyline[]> steps, double distance, bool start)
    {
        var result = new double[steps.Count];
        if (distance == 0) return result;
        for (var i = 1; i < result.Length; i++)
        {
            var delta = start ? steps[i][0].Unadjusted - steps[i - 1][0].Unadjusted : steps[i - 1][^1].Unadjusted - steps[i][^1].Unadjusted;
            result[i] = i == result.Length - 1 ? 1 : result[i - 1] + delta / distance;
        }
        return result;
    }
    private double SnapOffset(int index, int count)
    {
        var offset = _default[FirstFocal(_default)].Unadjusted - ItemWidth / 2;
        if (index < _start.Count)
        {
            var keylines = _start[Math.Clamp(_start.Count - 1 - index, 0, _start.Count - 1)];
            offset = keylines[FirstFocal(keylines)].Unadjusted - ItemWidth / 2;
        }
        if (index >= count - _end.Count && count > LastFocal(_default) - FirstFocal(_default) + 1)
        {
            var keylines = _end[Math.Clamp(_end.Count - 1 - (count - 1 - index), 0, _end.Count - 1)];
            offset = keylines[LastFocal(keylines)].Unadjusted - ItemWidth / 2;
        }
        return Math.Floor(offset + .5); // Kotlin Float.roundToInt, including negative offsets.
    }
    private Keyline[] Keylines(double scroll, double maximum)
    {
        scroll = Math.Max(0, scroll);
        var endOffset = Math.Max(0, maximum - _endDistance);
        if (scroll >= _startDistance && scroll <= endOffset) return _default;
        var steps = _start; var points = _startPoints;
        var progress = Range(1, 0, 0, _startDistance, scroll);
        var special = false;
        if (scroll > endOffset)
        {
            steps = _end; points = _endPoints;
            progress = Range(0, 1, endOffset, maximum, scroll);
            special = endOffset < .01 && _start.Count == 2 && _end.Count == 2;
        }
        if (special) { Mix(_start[^1], _end[^1], progress, _interpolated); return _interpolated; }
        for (var i = 1; i < steps.Count; i++)
            if (progress <= points[i])
            { Mix(steps[i - 1], steps[i], Range(0, 1, points[i - 1], points[i], progress), _interpolated); return _interpolated; }
        return steps[^1];
    }
    private static double Range(double from, double to, double minimum, double maximum, double value)
        => value <= minimum ? from : value >= maximum ? to : from + (to - from) * ((value - minimum) / (maximum - minimum));
    private static Keyline Mix(Keyline from, Keyline to, double t) => new(
        from.Size + (to.Size - from.Size) * t, from.Offset + (to.Offset - from.Offset) * t,
        from.Unadjusted + (to.Unadjusted - from.Unadjusted) * t, t < .5 ? from.Focal : to.Focal,
        t < .5 ? from.Anchor : to.Anchor, t < .5 ? from.Pivot : to.Pivot, from.Cutoff + (to.Cutoff - from.Cutoff) * t);
    private static void Mix(Keyline[] from, Keyline[] to, double t, Keyline[] output)
    { for (var i = 0; i < output.Length; i++) output[i] = Mix(from[i], to[i], t); }

    internal void Fill(double position, Size viewport, Rect[] output)
    {
        var count = output.Length;
        var stride = ItemWidth + _gap;
        var maximum = Math.Max(0, ItemWidth * count + _gap * (count - 1) - _space);
        var index = Math.Clamp((int)Math.Floor(position), 0, count - 1);
        var next = Math.Min(index + 1, count - 1);
        var startScroll = index * stride - SnapOffset(index, count);
        var endScroll = next * stride - SnapOffset(next, count);
        var scroll = Math.Clamp(startScroll + (endScroll - startScroll) * (position - index), 0, maximum);
        var keylines = Keylines(scroll, maximum);
        for (var i = 0; i < count; i++)
        {
            var rawCenter = i * stride + ItemWidth / 2 - scroll;
            var before = keylines[0]; var after = keylines[^1];
            for (var j = keylines.Length - 1; j >= 0; j--)
                if (keylines[j].Unadjusted < rawCenter) { before = keylines[j]; break; }
            for (var j = 0; j < keylines.Length; j++)
                if (keylines[j].Unadjusted >= rawCenter) { after = keylines[j]; break; }
            var t = before == after ? 1 : (rawCenter - before.Unadjusted) / (after.Unadjusted - before.Unadjusted);
            var keyline = Mix(before, after, t);
            var center = keyline.Offset;
            if (before == after) center += (rawCenter - keyline.Unadjusted) / keyline.Size;
            output[i] = new Rect(center - keyline.Size / 2, 0, Math.Max(0, keyline.Size), viewport.Height);
        }
    }
}
