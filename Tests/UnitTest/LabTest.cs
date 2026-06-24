using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UniTest;

public class LabTest
{
    Lab<Model> _lab;

    Model _model;
    object _metadata;


    [TearDown]
    public void TearDown()
    {
        _lab = null;

        _model = null;
        _metadata = null;
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
        _lab = setMetadataByConstructor
            ? new Lab<Model>(
            expectedExceptionType: typeof(ProbeException),
            toUnsustainable: true,
            toUncontinuable: true,
            remainingExecutionCount: 10)
            : new Lab<Model>();

        _model = new Model();
        

        if (setMetadata != Condition.None)
        {
            _metadata = new object();
            _lab.SetMetadata = m =>
            {
                Assert.That(m, Is.SameAs(_model));

                if (setMetadata == Condition.Failure)
                    throw new ProbeException("Set Metadata Failure");

                return _metadata;
            };
        }

        if (arranger != Condition.None)
            _lab.Arranger = (m, md) =>
            {
                Assert.That(m, Is.SameAs(_model));
                Assert.That(md.Metadata, Is.SameAs(_metadata));

                if (arranger == Condition.Failure)
                    throw new ProbeException("Arranger Failure");
            };

        if (actor != Condition.None)
            _lab.Actor = (m, md) =>
            {
                Assert.That(m, Is.SameAs(_model));
                Assert.That(md.Metadata, Is.SameAs(_metadata));

                if (actor == Condition.Failure)
                    throw new ProbeException("Actor Failure");
            };

        if (asserter != Condition.None)
            _lab.Asserter = (m, md) =>
            {
                Assert.That(m, Is.SameAs(_model));
                Assert.That(md.Metadata, Is.SameAs(_metadata));

                if (asserter == Condition.Failure)
                    throw new ProbeException("Asserter Failure");
            };
    }



    [Test]
    public void Execute_NullModel()
    {
        // Arrange
        _lab = new Lab<Model>();

        // Act
        _lab.Execute(null, out var ex);

        // Assert
        Assert.That(ex, Is.AssignableTo<ExecutionException>());
        Assert.That(ex.Message, Is.EqualTo("Model setting failed"));
    }

    [TestCaseSource(nameof(GetTestCases))]
    public void Execute_Model(bool setMetadataByConstructor, Condition setMetadata, Condition arranger, Condition actor, Condition asserter)
    {
        // Arrange
        CreateLab(setMetadataByConstructor, setMetadata, arranger, actor, asserter);
        Exception ex;

        // Act
        _lab.Execute(_model, out var _ex);
        ex = _ex;

        // Assert
        if (setMetadata == Condition.Failure)
        {
            Assert.That(ex, Is.AssignableTo<ExecutionException>());
            Assert.That(ex.Message, Is.EqualTo("Arrange failed"));

            ex = ex.InnerException;
            Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
            Assert.That(ex.Message, Is.EqualTo("Set Metadata Failure"));

            Assert.That(_model.Continuable, Is.False);
            return;
        }
        if (arranger == Condition.Failure)
        {
            Assert.That(ex, Is.AssignableTo<ExecutionException>());
            Assert.That(ex.Message, Is.EqualTo("Arrange failed"));

            ex = ex.InnerException;
            Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
            Assert.That(ex.Message, Is.EqualTo("Arranger Failure"));

            Assert.That(_model.Continuable, Is.False);
            return;
        }
        if (actor == Condition.Failure && !setMetadataByConstructor)
        {
            Assert.That(ex, Is.AssignableTo<ExecutionException>());
            Assert.That(ex.Message, Is.EqualTo("Act failed"));

            ex = ex.InnerException;
            Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
            Assert.That(ex.Message, Is.EqualTo("Actor Failure"));

            Assert.That(_model.Continuable, Is.False);
            return;
        }
        if (actor != Condition.Failure && setMetadataByConstructor)
        {
            Assert.That(ex, Is.AssignableTo<ExecutionException>());
            Assert.That(ex.Message, Is.EqualTo("Act failed"));

            Assert.That(_model.Continuable, Is.False);
            return;
        }
        if (asserter == Condition.Failure)
        {
            Assert.That(ex, Is.AssignableTo<ExecutionException>());
            Assert.That(ex.Message, Is.EqualTo("Assert failed"));

            ex = ex.InnerException;
            Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
            Assert.That(ex.Message, Is.EqualTo("Asserter Failure"));

            Assert.That(_model.Continuable, Is.False);
            return;
        }
         
        Assert.That(ex, Is.Null);

        Assert.That(!_model.Continuable, Is.EqualTo(setMetadataByConstructor));
        Assert.That(_model.RemainingExecutionCount, Is.EqualTo(setMetadataByConstructor ? 10 : null));
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

        _lab = new Lab<Model>(
            expectedExceptionType: template.ExpectedExceptionType,
            toUncontinuable: template.ToUncontinuable,
            remainingExecutionCount: template.RemainingExecutionCount)
        {
            Arranger = (_, md) => metadata = md
        };


        // Act
        _lab.Execute(new Model(), out var ex);

        // Assert
        Assert.That(ex, Is.AssignableTo<ExecutionException>());
        Assert.That(metadata, Is.Not.SameAs(template));

        Assert.That(metadata.ExpectedExceptionType, Is.EqualTo(template.ExpectedExceptionType));
        Assert.That(metadata.ToUncontinuable, Is.EqualTo(template.ToUncontinuable));
        Assert.That(metadata.RemainingExecutionCount, Is.EqualTo(template.RemainingExecutionCount));
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

        _lab = new Lab<Model>()
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
        _lab.Execute(new Model(), out var ex);

        // Assert
        Assert.That(ex, Is.AssignableTo<ExecutionException>());
        Assert.That(metadata, Is.Not.SameAs(template));

        Assert.That(metadata.Metadata, Is.SameAs(template.Metadata));
        Assert.That(metadata.ExpectedExceptionType, Is.EqualTo(template.ExpectedExceptionType));
        Assert.That(metadata.ToUncontinuable, Is.EqualTo(template.ToUncontinuable));
        Assert.That(metadata.RemainingExecutionCount, Is.EqualTo(template.RemainingExecutionCount));
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
        Assert.That(current.RemainingExecutionCount, Is.EqualTo(expectedRemainingExecutionCount));
    }



    [TestCaseSource(nameof(GetTestCases))]
    public void Copy(bool setMetadataByConstructor, Condition setMetadata, Condition arranger, Condition actor, Condition asserter)
    {
        // Arrange
        var copiedID = "Copied";
        CreateLab(setMetadataByConstructor, setMetadata, arranger, actor, asserter);

        // Act
        var copied = _lab.Copy(copiedID);

        // Assert
        Assert.That(copied.ID, Is.EqualTo(copiedID));
        Assert.That(copied.SetMetadata, Is.SameAs(_lab.SetMetadata));
        Assert.That(copied.Arranger, Is.SameAs(_lab.Arranger));
        Assert.That(copied.Actor, Is.SameAs(_lab.Actor));
        Assert.That(copied.Asserter, Is.SameAs(_lab.Asserter));

        var metadataTemplate_original = _lab.MetadataTemplate;
        var metadataTemplate_copied = copied.MetadataTemplate;

        Assert.That(metadataTemplate_copied.ExpectedExceptionType, Is.EqualTo(metadataTemplate_original.ExpectedExceptionType));
        Assert.That(metadataTemplate_copied.ToUnsustainable, Is.EqualTo(metadataTemplate_original.ToUnsustainable));
        Assert.That(metadataTemplate_copied.ToUncontinuable, Is.EqualTo(metadataTemplate_original.ToUncontinuable));
        Assert.That(metadataTemplate_copied.RemainingExecutionCount, Is.EqualTo(metadataTemplate_original.RemainingExecutionCount));
    }
}
