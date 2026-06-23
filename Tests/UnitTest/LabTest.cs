using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UniTest;

public class LabTest
{
    Lab<Model> lab;

    Model model;
    object metadata;


    [TearDown]
    public void TearDown()
    {
        lab = null;

        model = null;
        metadata = null;
    }

    public enum Condition
    {
        None,
        Success,
        Failure
    }
    public static IEnumerable<TestCaseData> GetTestCases()
    {
        var cond = new[] { Condition.None, Condition.Success, Condition.Failure };

        IEnumerable<Condition[]> GetSettings()
        {
            // Set Metadata Failure
            yield return new[] { Condition.Failure, Condition.None, Condition.None, Condition.None };

            // Arranger Failure
            foreach (var c1 in cond.Where(c => c != Condition.Failure))
                yield return new[] { c1, Condition.Failure, Condition.None, Condition.None };

            // Actor Failure
            foreach (var c1 in cond.Where(c => c != Condition.Failure))
            foreach (var c2 in cond.Where(c => c != Condition.Failure))
                yield return new[] { c1, c2, Condition.Failure, Condition.None };

            // Asserter Failure & All Success
            foreach (var c1 in cond.Where(c => c != Condition.Failure))
            foreach (var c2 in cond.Where(c => c != Condition.Failure))
            foreach (var c3 in cond.Where(c => c != Condition.Failure))
            foreach (var c4 in cond)
                 yield return new[] { c1, c2, c3, c4 };
        }


        foreach (var setting in GetSettings())
        {
            yield return new TestCaseData(false, setting[0], setting[1], setting[2], setting[3]);
            yield return new TestCaseData(true, setting[0], setting[1], setting[2], setting[3]);
        }
    }
    void CreateLab(bool setMetadataByConstructor, Condition setMetadata, Condition arranger, Condition actor, Condition asserter)
    {
        lab = setMetadataByConstructor
            ? new Lab<Model>(
            expectedExceptionType: typeof(ProbeException),
            toUnsustainable: true,
            toUncontinuable: true,
            remainingExecutionCount: 10)
            : new Lab<Model>();

        model = new Model();
        

        if (setMetadata != Condition.None)
        {
            metadata = new object();
            lab.SetMetadata = m =>
            {
                Assert.AreSame(model, m);

                if (setMetadata == Condition.Failure)
                    throw new ProbeException("Set Metadata Failure");

                return metadata;
            };
        }

        if (arranger != Condition.None)
            lab.Arranger = (m, md) =>
            {
                Assert.AreSame(model, m);
                Assert.AreSame(metadata, md.Metadata);

                if (arranger == Condition.Failure)
                    throw new ProbeException("Arranger Failure");
            };

        if (actor != Condition.None)
            lab.Actor = (m, md) =>
            {
                Assert.AreSame(model, m);
                Assert.AreSame(metadata, md.Metadata);

                if (actor == Condition.Failure)
                    throw new ProbeException("Actor Failure");
            };

        if (asserter != Condition.None)
            lab.Asserter = (m, md) =>
            {
                Assert.AreSame(model, m);
                Assert.AreSame(metadata, md.Metadata);

                if (asserter == Condition.Failure)
                    throw new ProbeException("Asserter Failure");
            };
    }



    [Test]
    public void Execute_NullModel()
    {
        // Arrange
        lab = new Lab<Model>();

        // Act
        lab.Execute(null, out var ex);

        // Assert
        Assert.IsAssignableFrom<ExecutionException>(ex);
        Assert.AreEqual("Model setting failed", ex.Message);
    }

    [TestCaseSource(nameof(GetTestCases))]
    public void Execute_Model(bool setMetadataByConstructor, Condition setMetadata, Condition arranger, Condition actor, Condition asserter)
    {
        // Arrange
        CreateLab(setMetadataByConstructor, setMetadata, arranger, actor, asserter);
        Exception ex;

        // Act
        lab.Execute(model, out var _ex);
        ex = _ex;

        // Assert
        if (setMetadata == Condition.Failure)
        {
            Assert.IsAssignableFrom<ExecutionException>(ex);
            Assert.AreEqual("Arrange failed", ex.Message);

            ex = ex.InnerException;
            Assert.IsAssignableFrom<ProbeException>(ex, ex?.Message ?? "-");
            Assert.AreEqual("Set Metadata Failure", ex.Message);

            Assert.IsFalse(model.Continuable);
            return;
        }
        if (arranger == Condition.Failure)
        {
            Assert.IsAssignableFrom<ExecutionException>(ex);
            Assert.AreEqual("Arrange failed", ex.Message);

            ex = ex.InnerException;
            Assert.IsAssignableFrom<ProbeException>(ex, ex?.Message ?? "-");
            Assert.AreEqual("Arranger Failure", ex.Message);

            Assert.IsFalse(model.Continuable);
            return;
        }
        if (actor == Condition.Failure && !setMetadataByConstructor)
        {
            Assert.IsAssignableFrom<ExecutionException>(ex);
            Assert.AreEqual("Act failed", ex.Message);

            ex = ex.InnerException;
            Assert.IsAssignableFrom<ProbeException>(ex, ex?.Message ?? "-");
            Assert.AreEqual("Actor Failure", ex.Message);

            Assert.IsFalse(model.Continuable);
            return;
        }
        if (actor != Condition.Failure && setMetadataByConstructor)
        {
            Assert.IsAssignableFrom<ExecutionException>(ex);
            Assert.AreEqual("Act failed", ex.Message);

            Assert.IsFalse(model.Continuable);
            return;
        }
        if (asserter == Condition.Failure)
        {
            Assert.IsAssignableFrom<ExecutionException>(ex);
            Assert.AreEqual("Assert failed", ex.Message);

            ex = ex.InnerException;
            Assert.IsAssignableFrom<ProbeException>(ex, ex?.Message ?? "-");
            Assert.AreEqual("Asserter Failure", ex.Message);

            Assert.IsFalse(model.Continuable);
            return;
        }
         
        Assert.IsNull(ex);

        Assert.AreEqual(setMetadataByConstructor, !model.Continuable);
        Assert.AreEqual(setMetadataByConstructor ? 10 : null, model.RemainingExecutionCount);
    }



    [Test]
    public void SetMetadata_ByConstructor()
    {
        // Arrange
        SubjectMetadata template = new()
        {
            ExpectedExceptionType = typeof(ProbeException),
            ToUncontinuable = true,
            RemainingExecutionCount = 10
        };
        SubjectMetadata metadata = null;

        lab = new Lab<Model>(
            expectedExceptionType: template.ExpectedExceptionType,
            toUncontinuable: template.ToUncontinuable,
            remainingExecutionCount: template.RemainingExecutionCount)
        {
            Arranger = (_, md) => metadata = md
        };


        // Act
        lab.Execute(new Model(), out var ex);

        // Assert
        Assert.IsAssignableFrom<ExecutionException>(ex);
        Assert.AreNotSame(template, metadata);

        Assert.AreEqual(template.ExpectedExceptionType, metadata.ExpectedExceptionType);
        Assert.AreEqual(template.ToUncontinuable, metadata.ToUncontinuable);
        Assert.AreEqual(template.RemainingExecutionCount, metadata.RemainingExecutionCount);
    }
    [Test]
    public void SetMetadata_ByDelegate()
    {
        // Arrange
        SubjectMetadata template = new()
        {
            Metadata = new object(),
            ExpectedExceptionType = typeof(ProbeException),
            ToUncontinuable = true,
            RemainingExecutionCount = 10
        };
        SubjectMetadata metadata = null;

        lab = new Lab<Model>()
        {
            Arranger = (_, md) =>
            {
                md.Metadata = template.Metadata;
                md.ExpectedExceptionType = template.ExpectedExceptionType;
                md.ToUncontinuable = template.ToUncontinuable;
                md.RemainingExecutionCount = template.RemainingExecutionCount;
            },
            Actor = (_, md) => metadata = md
        };

        
        // Act
        lab.Execute(new Model(), out var ex);

        // Assert
        Assert.IsAssignableFrom<ExecutionException>(ex);
        Assert.AreNotSame(template, metadata);

        Assert.AreSame(template.Metadata, metadata.Metadata);
        Assert.AreEqual(template.ExpectedExceptionType, metadata.ExpectedExceptionType);
        Assert.AreEqual(template.ToUncontinuable, metadata.ToUncontinuable);
        Assert.AreEqual(template.RemainingExecutionCount, metadata.RemainingExecutionCount);
    }


    [TestCase(-1, -1, -1)]
    [TestCase(-1, 3, 3)]
    [TestCase(3, -1, 3)]
    [TestCase(1, 10, 1)]
    [TestCase(10, 1, 1)]
    [TestCase(0, 5, 0)]
    public void MergeMetadata_ChooseRemainingExecutionCount(
        int currentRemainingExecutionCount,
        int templateRemainingExecutionCount,
        int expectedRemainingExecutionCount)
    {
        // Arrange
        var current = new SubjectMetadata
        {
            RemainingExecutionCount = currentRemainingExecutionCount
        };
        var template = new SubjectMetadata
        {
            RemainingExecutionCount = templateRemainingExecutionCount
        };

        // Act
        current.Merge(template);

        // Assert
        Assert.AreEqual(expectedRemainingExecutionCount, current.RemainingExecutionCount);
    }



    [TestCaseSource(nameof(GetTestCases))]
    public void Copy(bool setMetadataByConstructor, Condition setMetadata, Condition arranger, Condition actor, Condition asserter)
    {
        // Arrange
        var copiedID = "Copied";
        CreateLab(setMetadataByConstructor, setMetadata, arranger, actor, asserter);

        // Act
        var copied = lab.Copy(copiedID);

        // Assert
        Assert.AreEqual(copiedID, copied.ID);
        Assert.AreSame(lab.SetMetadata, copied.SetMetadata);
        Assert.AreSame(lab.Arranger, copied.Arranger);
        Assert.AreSame(lab.Actor, copied.Actor);
        Assert.AreSame(lab.Asserter, copied.Asserter);

        var metadataTemplate_original = lab.MetadataTemplate;
        var metadataTemplate_copied = copied.MetadataTemplate;

        Assert.AreEqual(metadataTemplate_original.ExpectedExceptionType, metadataTemplate_copied.ExpectedExceptionType);
        Assert.AreEqual(metadataTemplate_original.ToUnsustainable, metadataTemplate_copied.ToUnsustainable);
        Assert.AreEqual(metadataTemplate_original.ToUncontinuable, metadataTemplate_copied.ToUncontinuable);
        Assert.AreEqual(metadataTemplate_original.RemainingExecutionCount, metadataTemplate_copied.RemainingExecutionCount);
    }
}
