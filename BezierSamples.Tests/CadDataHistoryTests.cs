using WpfCad2Lib.Wpf;
using Xunit;

namespace BezierSamples.Tests;

public class CadDataHistoryTests
{
    [Theory]
    [InlineData(new int[] { 0 })]
    [InlineData(new int[] { 2 })]
    [InlineData(new int[] { 4 })]
    [InlineData(new int[] { 1, 3 })]
    [InlineData(new int[] { 3, 1 })]
    [InlineData(new int[] { 0, 1, 2, 3, 4 })]
    [InlineData(new int[] { 4, 3, 2, 1, 0 })]
    public void Remove_UndoRedo_RestoresReferencesAndOrder(int[] removedIndices)
    {
        StaThread.Run(() => {
            Figure[] original = CreateFigures();
            var data = CreateData(original);
            var remaining = original.Where((_, index) => !removedIndices.Contains(index)).ToArray();
            data.StartCommand();
            foreach (var index in removedIndices)
                data.Remove(original[index]);
            AssertFigures(data, remaining);

            for (var cycle = 0; cycle < 3; cycle++) {
                data.Undo();
                AssertFigures(data, original);
                Assert.True(data.CanRedo);
                data.Redo();
                AssertFigures(data, remaining);
                Assert.False(data.CanRedo);
            }
        });
    }

    [Fact]
    public void RemoveAll_UndoRedo_RestoresReferencesAndOrder()
    {
        StaThread.Run(() => {
            Figure[] original = CreateFigures();
            var data = CreateData(original);
            data.StartCommand();
            data.RemoveAll();
            Assert.True(data.IsEmpty);

            for (var cycle = 0; cycle < 3; cycle++) {
                data.Undo();
                AssertFigures(data, original);
                data.Redo();
                Assert.True(data.IsEmpty);
            }
        });
    }

    [Fact]
    public void Remove_InSeparateGroups_UndoRedo_StopsAtEachBoundary()
    {
        StaThread.Run(() => {
            Figure[] original = CreateFigures();
            var data = CreateData(original);
            data.StartCommand();
            data.Remove(original[1]);
            data.StartCommand();
            data.Remove(original[3]);

            data.Undo();
            AssertFigures(data, original[0], original[2], original[3], original[4]);
            data.Undo();
            AssertFigures(data, original);
            data.Redo();
            AssertFigures(data, original[0], original[2], original[3], original[4]);
            data.Redo();
            AssertFigures(data, original[0], original[2], original[4]);
        });
    }

    [Fact]
    public void MixedAddAndRemoveGroup_UndoRedo_UsesIndicesAtEachRemoval()
    {
        StaThread.Run(() => {
            Figure[] original = CreateFigures();
            var data = CreateData(original);
            Figure added = new LineFigure();
            data.StartCommand();
            data.Remove(original[3]);
            data.Add(added);
            data.Remove(original[1]);
            data.Remove(added);
            data.Remove(original[0]);

            for (var cycle = 0; cycle < 3; cycle++) {
                AssertFigures(data, original[2], original[4]);
                data.Undo();
                AssertFigures(data, original);
                data.Redo();
            }
            AssertFigures(data, original[2], original[4]);
        });
    }

    [Fact]
    public void Remove_MissingFigure_DoesNotCreateHistoryOrInvalidateRedo()
    {
        StaThread.Run(() => {
            var data = new CadData();
            Figure missing = new LineFigure();
            data.Remove(missing);
            Assert.False(data.CanUndo);
            data.Undo();
            Assert.True(data.IsEmpty);

            Figure added = new LineFigure();
            data.Add(added);
            data.Undo();
            Assert.True(data.CanRedo);
            data.Remove(missing);
            Assert.False(data.CanUndo);
            Assert.True(data.CanRedo);
            data.Redo();
            AssertFigures(data, added);
        });
    }

    [Fact]
    public void Update_ExposesOnlyInternallyOwnedTwoFigureOverload()
    {
        var method = typeof(CadData).GetMethod(nameof(CadData.Update), [typeof(Figure), typeof(Figure)]);
        Assert.NotNull(method);
        Assert.Null(typeof(CadData).GetMethod(nameof(CadData.Update),
            [typeof(IList<Figure>), typeof(Figure), typeof(Figure)]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public void Update_UndoRedo_PreservesInternalReferencesAndOrder(int index)
    {
        StaThread.Run(() => {
            Figure[] original = CreateFigures();
            var data = CreateData(original);
            Figure replacement = new LineFigure();
            var updated = original.ToArray();
            updated[index] = replacement;
            data.StartCommand();

            Assert.True(data.Update(original[index], replacement));
            AssertFigures(data, updated);
            for (var cycle = 0; cycle < 3; cycle++) {
                data.Undo();
                AssertFigures(data, original);
                data.Redo();
                AssertFigures(data, updated);
            }
        });
    }

    [Fact]
    public void Update_MissingFigure_DoesNotChangeDataOrInvalidateRedo()
    {
        StaThread.Run(() => {
            Figure[] original = CreateFigures();
            var data = CreateData(original);
            Figure added = new LineFigure();
            data.StartCommand();
            data.Add(added);
            data.Undo();
            Assert.True(data.CanRedo);

            Assert.False(data.Update(new LineFigure(), new LineFigure()));
            AssertFigures(data, original);
            Assert.True(data.CanRedo);
            data.Redo();
            AssertFigures(data, [.. original, added]);
        });
    }

    [Fact]
    public void MixedAddRemoveAndUpdateGroup_UndoRedo_RestoresOriginalAndFinalOrder()
    {
        StaThread.Run(() => {
            Figure[] original = CreateFigures();
            var data = CreateData(original);
            Figure added = new LineFigure();
            Figure replacement = new LineFigure();
            data.StartCommand();
            data.Remove(original[1]);
            data.Add(added);
            Assert.True(data.Update(original[3], replacement));
            AssertFigures(data, original[0], original[2], replacement, original[4], added);
            data.Remove(original[0]);
            data.Remove(replacement);

            for (var cycle = 0; cycle < 3; cycle++) {
                AssertFigures(data, original[2], original[4], added);
                data.Undo();
                AssertFigures(data, original);
                data.Redo();
            }
            AssertFigures(data, original[2], original[4], added);
        });
    }

    [Theory]
    [InlineData("Add")]
    [InlineData("Remove")]
    [InlineData("Update")]
    public void NewOperationAfterUndo_InvalidatesRedo(string operation)
    {
        StaThread.Run(() => {
            Figure[] original = CreateFigures();
            var data = CreateData(original);
            Figure discarded = new LineFigure();
            Figure replacement = new LineFigure();
            data.StartCommand();
            data.Add(discarded);
            data.Undo();
            Assert.True(data.CanRedo);
            data.StartCommand();

            Figure[] expected;
            switch (operation) {
                case "Add":
                    data.Add(replacement);
                    expected = [.. original, replacement];
                    break;
                case "Remove":
                    data.Remove(original[1]);
                    expected = [original[0], original[2], original[3], original[4]];
                    break;
                default:
                    Assert.True(data.Update(original[1], replacement));
                    expected = [original[0], replacement, original[2], original[3], original[4]];
                    break;
            }

            AssertFigures(data, expected);
            Assert.False(data.CanRedo);
            data.Redo();
            AssertFigures(data, expected);
            data.Undo();
            AssertFigures(data, original);
            data.Redo();
            AssertFigures(data, expected);
        });
    }

    [Fact]
    public void UndoRedo_ConsumesEachGroupOnlyOnce()
    {
        StaThread.Run(() => {
            Figure first = new LineFigure();
            Figure second = new LineFigure();
            Figure third = new LineFigure();
            var data = new CadData();
            data.Add(first);
            data.Add(second);
            data.StartCommand();
            data.Add(third);

            data.Undo();
            AssertFigures(data, first, second);
            data.Undo();
            Assert.True(data.IsEmpty);
            Assert.False(data.CanUndo);
            data.Undo();
            Assert.True(data.IsEmpty);
            data.Redo();
            AssertFigures(data, first, second);
            data.Redo();
            AssertFigures(data, first, second, third);
            Assert.False(data.CanRedo);
            data.Redo();
            AssertFigures(data, first, second, third);
        });
    }

    static Figure[] CreateFigures()
        => [new LineFigure(), new LineFigure(), new LineFigure(), new LineFigure(), new LineFigure()];

    static CadData CreateData(params Figure[] figures)
    {
        var data = new CadData();
        foreach (var figure in figures)
            data.Add(figure);
        return data;
    }

    static void AssertFigures(CadData data, params Figure[] expected)
    {
        var actual = data.ToArray();
        Assert.Equal(expected.Length, data.Count);
        Assert.Equal(expected.Length, actual.Length);
        for (var index = 0; index < expected.Length; index++)
            Assert.Same(expected[index], actual[index]);
    }
}
