using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BlackThunder.UniTest;
using NUnit.Framework;

namespace BlackThunder.UniTest.Tests.Recursion
{
    public class ModelTest
    {

        public class Model : BlackThunder.UniTest.Model
        {
            // Front --------------------
            // Subject
            public new BlackThunder.UniTest.Model Subject
            {
                get => (BlackThunder.UniTest.Model)base.Subject;
                set => base.Subject = value;
            }

            // Mock
            public bool ExpectedSustainable = true;
            public bool ExpectedContinuable = true;
            public int? ExpectedRemainingExecutionCount = null;
            public List<string> ExecutedLabs { get; } = new();

            public string[] Labs { get; } = new[] { 0, 1, 2, 3, 4 }.Select(i => $"Lab_{i}").ToArray();
            public Random Rand { get; } = new(0);
            public string RandNextLab => Labs[Rand.Next(Labs.Length)];
        }

        private class Project : Project<Model>
        {
            private readonly int _initialRemainingCount;


            public Project(ExecutionMode mode, int initialRemainingCount = 10) : base(mode)
            {
                _initialRemainingCount = initialRemainingCount;
            }

            private enum State
            {
                None,
                Idle,
                Nonsustainable,
                LimitedExecutionCount,
                Noncontinuable
            }
            private State GetState(Model model)
            {
                if (model.Subject == null)
                    return State.None;

                if (!model.Subject.Continuable)
                    return State.Noncontinuable;

                if (model.Subject.RemainingExecutionCount.HasValue)
                    return State.LimitedExecutionCount;

                if (!model.Subject.Sustainable)
                    return State.Nonsustainable;

                return State.Idle;
            }

            private void CheckModel(Model model, SubjectMetadata _)
            {
                if (model.Subject == null)
                    return;

                Assert.That(model.Subject.Sustainable, Is.EqualTo(model.ExpectedSustainable));
                Assert.That(model.Subject.Continuable, Is.EqualTo(model.ExpectedContinuable));
                Assert.That(model.Subject.RemainingExecutionCount, Is.EqualTo(model.ExpectedRemainingExecutionCount));

                Assert.That(model.Subject.ExecutionCount, Is.EqualTo(model.ExecutedLabs.Count));
                Assert.That(
                    model.Subject.GetExecutionHistory(),
                    Is.EqualTo(string.Join(Model.Separator, model.ExecutedLabs)));
            }

            private void DoExecute(Model model, string labId)
            {
                try
                {
                    var method = typeof(Model).GetMethod("DoExecute", BindingFlags.Instance | BindingFlags.NonPublic);
                    method.Invoke(model.Subject, new[] { labId });
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }


            public override IEnumerable<ILab<Model>> CreateLabs(Model model)
            {
                var state = GetState(model);


                // Ignite
                if (state == State.None)
                {
                    yield return new Lab<Model>("Ignite")
                    {
                        SetMetadata = _ => new Model(),
                        Actor = (m, md) => m.Subject = (Model)md.Metadata,
                        Asserter = (m, md) => Assert.That(md.Metadata, Is.SameAs(m.Subject))
                    };

                    yield break;
                }


                // Do Execute
                var template = new Lab<Model>("DoExecute")
                {
                    Arranger = (m, _) => m.ExecutedLabs.Add(m.RandNextLab),
                    Actor = (m, _) => DoExecute(m, m.ExecutedLabs[^1]),
                    Asserter = CheckModel
                };

                switch (state)
                {
                    case State.Idle:
                        yield return new Lab<Model>("Idle").Merge(template);
                        break;

                    case State.Nonsustainable:
                        yield return new Lab<Model>("Nonsustainable").Merge(template);
                        break;

                    case State.LimitedExecutionCount:
                        yield return new Lab<Model>("LimitedExecutionCount")
                        {
                            Arranger = (m, _) =>
                            {
                                if (--m.ExpectedRemainingExecutionCount <= 0)
                                    m.ExpectedContinuable = false;
                            }
                        }.Merge(template);

                        break;

                    case State.Noncontinuable:
                        yield return new Lab<Model>(
                            "Noncontinuable",
                            expectedExceptionType: typeof(InvalidOperationException),
                            toUncontinuable: true)
                            .Merge(template, useAsserter: false);
                        break;
                }


                // To Nonsustainable
                template = new Lab<Model>("ToNonsustainable")
                {
                    Arranger = (m, _) => m.ExpectedSustainable = false,
                    Actor = (m, _) => m.Subject.Sustainable = false,
                    Asserter = CheckModel
                };

                switch (state)
                {
                    case State.Idle:
                        yield return new Lab<Model>("Idle").Merge(template);
                        break;

                    case State.Nonsustainable:
                        yield return new Lab<Model>("Nonsustainable").Merge(template, useArranger: false);
                        break;

                    case State.LimitedExecutionCount:
                        yield return new Lab<Model>("LimitedExecutionCount").Merge(template, useArranger: false);
                        break;

                    case State.Noncontinuable:
                        yield return new Lab<Model>("Noncontinuable").Merge(template, useArranger: false);
                        break;
                }


                // To Noncontinuable
                var temp_true = new Lab<Model>("ToNoncontinuable_true")
                {
                    Arranger = (m, _) =>
                    {
                        m.ExpectedContinuable = false;
                        m.ExpectedSustainable = false;
                    },
                    Actor = (m, _) => m.Subject.Continuable = false,
                    Asserter = CheckModel
                };

                var temp_false = new Lab<Model>(
                    "ToNoncontinuable_false",
                    expectedExceptionType: typeof(InvalidOperationException),
                    toUncontinuable: true)
                {
                    Actor = (m, _) => m.Subject.Continuable = true
                };

                switch (state)
                {
                    case State.Idle:
                        yield return new Lab<Model>("Idle")
                        {
                            Arranger = temp_true.Arranger,
                            Actor = temp_true.Actor,
                            Asserter = temp_true.Asserter
                        };

                        yield return new Lab<Model>("Idle").Merge(temp_false);
                        break;

                    case State.Nonsustainable:
                        yield return new Lab<Model>("Nonsustainable").Merge(temp_true);
                        yield return new Lab<Model>("Nonsustainable").Merge(temp_false);
                        break;

                    case State.LimitedExecutionCount:
                        yield return new Lab<Model>("LimitedExecutionCount").Merge(temp_true);
                        yield return new Lab<Model>("LimitedExecutionCount").Merge(temp_false);
                        break;

                    case State.Noncontinuable:
                        yield return new Lab<Model>("Noncontinuable").Merge(temp_true);
                        yield return new Lab<Model>("Noncontinuable").Merge(temp_false, useArranger: false);
                        break;
                }


                // Set Remaining Execution Count
                var temp_null = new Lab<Model>(
                    "SetRemainingExecutionCount_null",
                    expectedExceptionType: typeof(InvalidOperationException),
                    toUncontinuable: true)
                {
                    Actor = (m, _) => m.Subject.RemainingExecutionCount = null
                };

                var temp_larger = new Lab<Model>("SetRemainingExecutionCount_larger")
                {
                    SetMetadata = m => m.ExpectedRemainingExecutionCount + 1,
                    Actor = (m, md) => m.Subject.RemainingExecutionCount = (int)md.Metadata,
                    Asserter = CheckModel
                };

                var temp_smaller = new Lab<Model>("SetRemainingExecutionCount_smaller")
                {
                    SetMetadata = m =>
                        m.ExpectedRemainingExecutionCount.HasValue
                            ? m.ExpectedRemainingExecutionCount - 1 : _initialRemainingCount,
                    Arranger = (m, md) =>
                    {
                        m.ExpectedSustainable = false;

                        if ((int)md.Metadata <= 0) m.ExpectedContinuable = false;

                        m.ExpectedRemainingExecutionCount = (int)md.Metadata;
                    },
                    Actor = (m, md) => m.Subject.RemainingExecutionCount = (int)md.Metadata,
                    Asserter = CheckModel
                };

                var temp_negative = new Lab<Model>("SetRemainingExecutionCount_negative")
                {
                    SetMetadata = _ => -1,
                    Arranger = (m, md) =>
                    {
                        m.ExpectedSustainable = false;
                        m.ExpectedContinuable = false;

                        m.ExpectedRemainingExecutionCount = (int)md.Metadata;
                    },
                    Actor = (m, md) => m.Subject.RemainingExecutionCount = (int)md.Metadata,
                    Asserter = CheckModel
                };

                switch (state)
                {
                    case State.Idle:
                        yield return new Lab<Model>("Idle").Merge(temp_null);
                        yield return new Lab<Model>("Idle").Merge(temp_smaller);
                        yield return new Lab<Model>("Idle").Merge(temp_negative);
                        break;

                    case State.Nonsustainable:
                        yield return new Lab<Model>("Nonsustainable").Merge(temp_null);
                        yield return new Lab<Model>("Nonsustainable").Merge(temp_smaller);
                        yield return new Lab<Model>("Nonsustainable").Merge(temp_negative);
                        break;

                    case State.LimitedExecutionCount:
                        yield return new Lab<Model>("LimitedExecutionCount").Merge(temp_null);
                        yield return new Lab<Model>("LimitedExecutionCount").Merge(temp_larger);
                        yield return new Lab<Model>("LimitedExecutionCount").Merge(temp_smaller);

                        yield return new Lab<Model>("LimitedExecutionCount")
                        {
                            Arranger = (m, md) =>
                            {
                                m.ExpectedContinuable = false;
                                m.ExpectedRemainingExecutionCount = Math.Min(
                                    (int)md.Metadata,
                                    m.ExpectedRemainingExecutionCount.Value);
                            }
                        }.Merge(temp_negative, useArranger: false);

                        break;

                    case State.Noncontinuable:
                        yield return new Lab<Model>("Noncontinuable").Merge(temp_null);

                        if (model.ExpectedRemainingExecutionCount.HasValue)
                            yield return new Lab<Model>("Noncontinuable").Merge(temp_larger);

                        yield return new Lab<Model>("Noncontinuable").Merge(temp_smaller);

                        yield return new Lab<Model>("Noncontinuable")
                        {
                            Arranger = (m, md) =>
                            {
                                m.ExpectedRemainingExecutionCount = Math.Min(
                                    (int)md.Metadata,
                                    m.ExpectedRemainingExecutionCount ?? int.MaxValue);
                            }
                        }.Merge(temp_negative, useArranger: false);

                        break;
                }
            }
        }
        // M04-M05: these candidate IDs are independent of CreateLabs and its actual output.
        private static readonly Dictionary<string, string[]> _expectedIdsByState = new()
        {
            ["Idle"] = new[]
            {
                "Idle_DoExecute", "Idle_ToNonsustainable", "Idle", "Idle_ToNoncontinuable_false",
                "Idle_SetRemainingExecutionCount_null", "Idle_SetRemainingExecutionCount_smaller",
                "Idle_SetRemainingExecutionCount_negative"
            },
            ["Nonsustainable"] = new[]
            {
                "Nonsustainable_DoExecute", "Nonsustainable_ToNonsustainable", "Nonsustainable_ToNoncontinuable_true",
                "Nonsustainable_ToNoncontinuable_false", "Nonsustainable_SetRemainingExecutionCount_null",
                "Nonsustainable_SetRemainingExecutionCount_smaller",
                "Nonsustainable_SetRemainingExecutionCount_negative"
            },
            ["LimitedExecutionCount"] = new[]
            {
                "LimitedExecutionCount_DoExecute", "LimitedExecutionCount_ToNonsustainable",
                "LimitedExecutionCount_ToNoncontinuable_true", "LimitedExecutionCount_ToNoncontinuable_false",
                "LimitedExecutionCount_SetRemainingExecutionCount_null",
                "LimitedExecutionCount_SetRemainingExecutionCount_larger",
                "LimitedExecutionCount_SetRemainingExecutionCount_smaller",
                "LimitedExecutionCount_SetRemainingExecutionCount_negative"
            },
            ["Noncontinuable"] = new[]
            {
                "Noncontinuable_DoExecute", "Noncontinuable_ToNonsustainable", "Noncontinuable_ToNoncontinuable_true",
                "Noncontinuable_ToNoncontinuable_false", "Noncontinuable_SetRemainingExecutionCount_null",
                "Noncontinuable_SetRemainingExecutionCount_larger", "Noncontinuable_SetRemainingExecutionCount_smaller",
                "Noncontinuable_SetRemainingExecutionCount_negative"
            }
        };


        private static IEnumerable<string> ExpectedChildIds(Model model)
        {
            if (model.ExecutionCount == 0)
                return new[] { "Ignite" };

            var state = !model.ExpectedContinuable ? "Noncontinuable"
                : model.ExpectedRemainingExecutionCount.HasValue ? "LimitedExecutionCount"
                : !model.ExpectedSustainable ? "Nonsustainable" : "Idle";
            return _expectedIdsByState[state].Where(id =>
                model.ExpectedRemainingExecutionCount.HasValue || !id.EndsWith("_larger", StringComparison.Ordinal));
        }

        private static bool EndsHarnessPath(string id) =>
            id == "Noncontinuable_DoExecute"
                || id.EndsWith("_ToNoncontinuable_false", StringComparison.Ordinal)
                || id.EndsWith("_SetRemainingExecutionCount_null", StringComparison.Ordinal);

        private static List<Node<Model>> AssertCompletedNodes(Node<Model> result)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.AllSucceed(out var cancelled), Is.True, result.GetFailedReports().ToJson());
            Assert.That(cancelled, Is.False);

            var nodes = new List<Node<Model>>();
            Visit(result, "", 0);
            Assert.That(nodes.Count, Is.GreaterThan(1));
            return nodes;

            void Visit(Node<Model> node, string path, int depth)
            {
                nodes.Add(node);
                Assert.That(node.Depth, Is.EqualTo(depth), path);
                Assert.That(node.Model.ExecutionCount, Is.EqualTo(depth), path);
                Assert.That(node.Model.GetExecutionHistory(), Is.EqualTo(path));
                Assert.That(node.Exception, Is.Null, path);
                Assert.That(
                    node.Status,
                    Is.EqualTo(depth == 0 ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success),
                    path);
                Assert.That(node.Model.Continuable, Is.EqualTo(!EndsHarnessPath(node.Id)), path);

                foreach (var child in node.Afters)
                    Visit(child, path.Length == 0 ? child.Id : path + Model.Separator + child.Id, depth + 1);
            }
        }

        private static void AssertRun(Project project, Node<Model> result, int depth, bool continuous)
        {
            var nodes = AssertCompletedNodes(result);
            Assert.That(nodes.Max(node => node.Depth), Is.EqualTo(depth));
            Assert.That(project.ProcessCount, Is.EqualTo(nodes.Count - 1));

            foreach (var node in nodes)
            {
                var mayExpand = node.Depth < depth && !EndsHarnessPath(node.Id);

                if (continuous && node.Afters.Count == 0)
                    continue;

                Assert.That(
                    node.Afters.Select(child => child.Id),
                    Is.EquivalentTo(mayExpand ? ExpectedChildIds(node.Model) : Array.Empty<string>()),
                    node.Model.GetExecutionHistory());
            }

            if (continuous)
            {
                Assert.That(nodes.Count(node => node.Afters.Count > 0), Is.EqualTo(depth));
            }
            else
            {
                var expected = _expectedIdsByState.Values.SelectMany(ids => ids).Concat(new[] { "Ignite" });
                Assert.That(expected.Count(), Is.EqualTo(31));
                Assert.That(nodes.Skip(1).Select(node => node.Id).Distinct(), Is.EquivalentTo(expected));
            }
        }


        // M04: every required scenario and every expanded state must complete in both modes.
        [TestCase(ExecutionMode.ThreadPool), TestCase(ExecutionMode.Inline)]
        public async Task Test(ExecutionMode mode)
        {
            // Arrange
            var project = new Project(mode);

            // Act
            var result = await project.Execute(6);

            // Assert
            AssertRun(project, result, 6, false);
        }

        // M05: every selected state expands all expected candidates through the requested depth.
        [TestCase(ExecutionMode.ThreadPool, 0), TestCase(ExecutionMode.ThreadPool, 1)]
        [TestCase(ExecutionMode.ThreadPool, 2), TestCase(ExecutionMode.Inline, 0)]
        [TestCase(ExecutionMode.Inline, 1), TestCase(ExecutionMode.Inline, 2)]
        public async Task ContinuousTest(ExecutionMode mode, int seed)
        {
            // Arrange
            var project = new Project(mode);

            // Act
            var result = await project.ExecuteContinuously(100, seed);

            // Assert
            AssertRun(project, result, 100, true);
        }

        // M01: explicitly replay the 1-to-0 boundary without increasing the exhaustive depth.
        [TestCase(ExecutionMode.ThreadPool), TestCase(ExecutionMode.Inline)]
        public async Task ZeroRemainingCountReplay(ExecutionMode mode)
        {
            // Arrange
            var project = new Project(mode, initialRemainingCount: 1);
            var path = "Ignite/Idle_SetRemainingExecutionCount_smaller/"
                + "LimitedExecutionCount_SetRemainingExecutionCount_smaller";

            // Act
            var result = await project.Execute(path);

            // Assert
            var nodes = AssertCompletedNodes(result);
            Assert.That(nodes.Count, Is.EqualTo(4));
            var before = nodes.Single(node => node.Depth == 2).Model;
            Assert.That(before.Subject.RemainingExecutionCount, Is.EqualTo(1));
            Assert.That(before.Subject.Continuable, Is.True);
            var after = nodes.Single(node => node.Depth == 3).Model;
            Assert.That(after.GetExecutionHistory(), Is.EqualTo(path));
            Assert.That(after.Subject.RemainingExecutionCount, Is.Zero);
            Assert.That(after.Subject.Sustainable, Is.False);
            Assert.That(after.Subject.Continuable, Is.False);
            Assert.That(after.ExpectedRemainingExecutionCount, Is.Zero);
            Assert.That(after.ExpectedContinuable, Is.False);
            Assert.That(after.Subject.ExecutionCount, Is.Zero);
        }
    }
}
