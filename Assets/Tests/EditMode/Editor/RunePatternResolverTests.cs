using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class RunePatternResolverTests
{
    [Test]
    public void RightwardReadsModifiersToRightUntilAnotherObject()
    {
        List<WordData> sentence = new List<WordData>
        {
            Object(ReadingPattern.Rightward),
            Modifier(),
            Modifier(),
            Object(ReadingPattern.None),
            Modifier()
        };

        CollectionAssert.AreEqual(new[] { 1, 2 }, RunePatternResolver.GetAffectedModifierIndices(sentence, 0));
    }

    [Test]
    public void LeftwardReadsModifiersToLeftUntilAnotherObject()
    {
        List<WordData> sentence = new List<WordData>
        {
            Modifier(),
            Object(ReadingPattern.None),
            Modifier(),
            Modifier(),
            Object(ReadingPattern.Leftward)
        };

        CollectionAssert.AreEqual(new[] { 3, 2 }, RunePatternResolver.GetAffectedModifierIndices(sentence, 4));
    }

    [Test]
    public void SpreadRadius3ReadsWithinThreeSlotsAndStopsAtObjectBoundaries()
    {
        List<WordData> sentence = new List<WordData>
        {
            Modifier(),
            Object(ReadingPattern.None),
            Modifier(),
            Modifier(),
            Object(ReadingPattern.SpreadRadius3),
            Modifier(),
            Modifier(),
            Object(ReadingPattern.None),
            Modifier()
        };

        CollectionAssert.AreEqual(new[] { 3, 2, 5, 6 }, RunePatternResolver.GetAffectedModifierIndices(sentence, 4));
    }

    [Test]
    public void UnlimitedReadsBothDirectionsUntilObjectBoundaries()
    {
        List<WordData> sentence = new List<WordData>
        {
            Modifier(),
            Object(ReadingPattern.None),
            Modifier(),
            Object(ReadingPattern.Unlimited),
            Modifier(),
            Modifier(),
            Object(ReadingPattern.None),
            Modifier()
        };

        CollectionAssert.AreEqual(new[] { 2, 4, 5 }, RunePatternResolver.GetAffectedModifierIndices(sentence, 3));
    }

    [Test]
    public void ForwardOddStepsChecksOnlySteppedIndicesAndSkippedObjectDoesNotStopScan()
    {
        List<WordData> sentence = new List<WordData>
        {
            Object(ReadingPattern.ForwardOddSteps),
            Modifier(),
            Object(ReadingPattern.None),
            Modifier(),
            Object(ReadingPattern.None),
            Modifier()
        };

        CollectionAssert.AreEqual(new[] { 1, 3, 5 }, RunePatternResolver.GetAffectedModifierIndices(sentence, 0));
    }

    [Test]
    public void BackwardOddStepsChecksOnlySteppedIndicesAndSkippedObjectDoesNotStopScan()
    {
        List<WordData> sentence = new List<WordData>
        {
            Modifier(),
            Object(ReadingPattern.None),
            Modifier(),
            Object(ReadingPattern.None),
            Modifier(),
            Object(ReadingPattern.BackwardOddSteps)
        };

        CollectionAssert.AreEqual(new[] { 4, 2, 0 }, RunePatternResolver.GetAffectedModifierIndices(sentence, 5));
    }

    [Test]
    public void NoneReturnsNoModifiers()
    {
        List<WordData> sentence = new List<WordData>
        {
            Modifier(),
            Object(ReadingPattern.None),
            Modifier()
        };

        CollectionAssert.IsEmpty(RunePatternResolver.GetAffectedModifierIndices(sentence, 1));
    }

    private static WordData Object(ReadingPattern readingPattern)
    {
        WordData word = ScriptableObject.CreateInstance<WordData>();
        word.wordType = WordType.Object;
        word.readingPattern = readingPattern;
        return word;
    }

    private static WordData Modifier()
    {
        WordData word = ScriptableObject.CreateInstance<WordData>();
        word.wordType = WordType.Modifier;
        return word;
    }
}
