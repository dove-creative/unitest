using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UniTest;

public class ToUncontinuableTest
{
    [TestCase("Template")]
    [TestCase("Arrange")]
    [TestCase("Act")]
    [TestCase("Assert")]
    public void LabCompletesAssertBeforeStopping(string setAt)
    {
        var steps = new List<string>();
        var lab = new Lab<Model>("terminal", toUncontinuable: setAt == "Template",
            arranger: (_, md) =>
            {
                steps.Add("Arrange");
                if (setAt == "Arrange") md.ToUncontinuable = true;
            },
            actor: (_, md) =>
            {
                steps.Add("Act");
                if (setAt == "Act") md.ToUncontinuable = true;
            },
            asserter: (model, md) =>
            {
                steps.Add("Assert");
                Assert.That(model.Continuable, Is.True, "Apply the flag after validation.");
                if (setAt == "Assert") md.ToUncontinuable = true;
            });
        var node = new Node<Model>(lab);

        node.Execute();

        Assert.That(steps, Is.EqualTo(new[] { "Arrange", "Act", "Assert" }));
        Assert.That(node.Status, Is.EqualTo(Node<Model>.NodeStatus.Success));
        Assert.That(node.Exception, Is.Null);
        Assert.That(node.Model.Continuable, Is.False);
        Assert.That(node.Model.Sustainable, Is.False);
    }

    [Test]
    public void CompactLabCompletesAssertBeforeStopping()
    {
        var steps = new List<string>();
        var lab = new CompactLab<Model>("terminal",
            arranger: _ => steps.Add("Arrange"),
            actor: _ => steps.Add("Act"),
            asserter: model =>
            {
                steps.Add("Assert");
                Assert.That(model.Continuable, Is.True);
            },
            toUncontinuable: true).Build();
        var model = new Model();

        lab.Execute(model, out var error);

        Assert.That(error, Is.Null);
        Assert.That(steps, Is.EqualTo(new[] { "Arrange", "Act", "Assert" }));
        Assert.That(model.Continuable, Is.False);
    }

    [TestCase(0, false)]
    [TestCase(1, false)]
    [TestCase(2, false)]
    [TestCase(0, true)]
    [TestCase(1, true)]
    [TestCase(2, true)]
    public void CompositeRunsEveryAssertInReverseOrder(int terminalLayer, bool setDuringAssert)
    {
        var steps = new List<string>();
        var labs = Enumerable.Range(0, 3).Select(index => new Lab<Model>($"layer {index}",
            toUncontinuable: index == terminalLayer && !setDuringAssert,
            arranger: (_, _) => steps.Add($"Arrange {index}"),
            actor: (_, _) => steps.Add($"Act {index}"),
            asserter: (model, md) =>
            {
                steps.Add($"Assert {index}");
                Assert.That(model.Continuable, Is.True);
                if (index == terminalLayer && setDuringAssert)
                    md.ToUncontinuable = true;
            })).ToArray();
        var composite = new CompositeLab<Model>(labs[0], labs[1]).Extend(labs[2]);
        var model = new Model();

        composite.Execute(model, out var error);

        Assert.That(error, Is.Null);
        Assert.That(steps, Is.EqualTo(new[]
        {
            "Arrange 0", "Arrange 1", "Arrange 2", "Act 0", "Assert 2", "Assert 1", "Assert 0"
        }));
        Assert.That(model.Continuable, Is.False);
        Assert.That(model.Sustainable, Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AssertFailureIsReportedAndStopsRemainingAssertions(bool composite)
    {
        var expected = new InvalidOperationException("validation failed");
        int otherAsserts = 0;
        var failingLab = new Lab<Model>("failing", toUncontinuable: true,
            asserter: (_, _) => throw expected);
        ILab<Model> lab = composite
            ? new CompositeLab<Model>(new Lab<Model>("base", asserter: (_, _) => otherAsserts++), failingLab)
            : failingLab;
        var node = new Node<Model>(lab);

        node.Execute();

        Assert.That(node.Status, Is.EqualTo(Node<Model>.NodeStatus.Failure));
        Assert.That(node.Exception, Is.TypeOf<ExecutionException>());
        Assert.That(node.Exception.InnerException, Is.SameAs(expected));
        Assert.That(node.Exception.Message, Does.StartWith("Assert failed"));
        Assert.That(node.Model.Continuable, Is.False);
        Assert.That(otherAsserts, Is.Zero);
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void ExpectedActorExceptionStillRunsAssertions(bool composite, bool expectationOnExtension)
    {
        int assertions = 0;
        var original = new Lab<Model>("original",
            actor: (_, _) => throw new InvalidOperationException("expected"),
            asserter: (_, _) => assertions++,
            expectedExceptionType: expectationOnExtension ? null : typeof(InvalidOperationException),
            toUncontinuable: true);
        ILab<Model> lab = composite
            ? new CompositeLab<Model>(original, new Lab<Model>("extension",
                asserter: (_, _) => assertions++,
                expectedExceptionType: expectationOnExtension ? typeof(InvalidOperationException) : null))
            : original;
        var model = new Model();

        lab.Execute(model, out var error);

        Assert.That(error, Is.Null);
        Assert.That(assertions, Is.EqualTo(composite ? 2 : 1));
        Assert.That(model.Continuable, Is.False);
    }

    [TestCase(ExecutionMode.ThreadPool, "Lab")]
    [TestCase(ExecutionMode.Inline, "Lab")]
    [TestCase(ExecutionMode.ThreadPool, "Compact")]
    [TestCase(ExecutionMode.Inline, "Compact")]
    [TestCase(ExecutionMode.ThreadPool, "Composite")]
    [TestCase(ExecutionMode.Inline, "Composite")]
    public async Task ProjectValidatesTerminalNodeWithoutExpandingIt(ExecutionMode mode, string kind)
    {
        int designs = 0;
        int assertions = 0;
        ILab<Model> terminal = kind switch
        {
            "Compact" => new CompactLab<Model>("terminal", asserter: _ => assertions++,
                toUncontinuable: true).Build(),
            "Composite" => new CompositeLab<Model>(
                new Lab<Model>("base", asserter: (_, _) => assertions++),
                new Lab<Model>("terminal", asserter: (_, _) => assertions++, toUncontinuable: true)),
            _ => new Lab<Model>("terminal", asserter: (_, _) => assertions++, toUncontinuable: true)
        };
        var project = new ProjectMock(mode, _ =>
        {
            designs++;
            return new[] { terminal };
        });

        var root = await project.Execute(5).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(designs, Is.EqualTo(1));
        Assert.That(assertions, Is.EqualTo(kind == "Composite" ? 2 : 1));
        Assert.That(project.ProcessCount, Is.EqualTo(1));
        Assert.That(root.Afters.Count, Is.EqualTo(1));
        var leaf = root.Afters.Single();
        Assert.That(leaf.Afters, Is.Empty);
        Assert.That(leaf.Status, Is.EqualTo(Node<Model>.NodeStatus.Success));
        Assert.That(leaf.Exception, Is.Null);
        Assert.That(leaf.Model.Continuable, Is.False);
    }
}
