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
            public Random Rand { get; } = new Random(0);
            public string RandNextLab => Labs[Rand.Next(Labs.Length)];
        }

        private class Project : Project<Model>
        {
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
                Assert.That(model.Subject.GetExecutionHistory(), Is.EqualTo(string.Join(Model.Separator, model.ExecutedLabs)));
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
                        yield return new Lab<Model>("Noncontinuable",
                            expectedExceptionType: typeof(InvalidOperationException), toUncontinuable: true)
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
                var temp_false = new Lab<Model>("ToNoncontinuable_false",
                    expectedExceptionType: typeof(InvalidOperationException), toUncontinuable: true)
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
                var temp_null = new Lab<Model>("SetRemainingExecutionCount_null",
                    expectedExceptionType: typeof(InvalidOperationException), toUncontinuable: true)
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
                    SetMetadata = m => m.ExpectedRemainingExecutionCount.HasValue ? m.ExpectedRemainingExecutionCount - 1 : 10,
                    Arranger = (m, md) =>
                    {
                        m.ExpectedSustainable = false;
                        if ((int)md.Metadata < 0) m.ExpectedContinuable = false;

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
                                m.ExpectedRemainingExecutionCount = Math.Min((int)md.Metadata, m.ExpectedRemainingExecutionCount.Value);
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
                                m.ExpectedRemainingExecutionCount = Math.Min((int)md.Metadata, m.ExpectedRemainingExecutionCount ?? int.MaxValue);
                            }
                        }.Merge(temp_negative, useArranger: false);
                        break;
                }
            }
        }
        private static void AssertSucceed(Node<Model> result)
        {
            Assert.That(result.AllSucceed(out var cancelled), Is.True, result.GetFailedReports().ToJson());
            Assert.That(cancelled, Is.False);
        }


        [Test]
        public async Task Test()
        {
            // Arrange
            var project = new Project();

            // Act
            var result = await project.Execute(6);

            // Assert
            AssertSucceed(result);
        }

        [TestCase(0), TestCase(1), TestCase(2)]
        public async Task ContinuousTest(int seed)
        {
            // Arrange
            var project = new Project();

            // Act
            var result = await project.ExecuteContinuously(100, seed);

            // Assert
            AssertSucceed(result);
        }

        //[Test]
        //public async Task Debug()
        //{
        //    var project = new Project();
        //    var result = await project.Execute("Ignite/Idle_SetRemainingExecutionCount_smaller/LimitedExecutionCount_SetRemainingExecutionCount_negative");

        //    result = result.GetLastNode().DetachAndRestore();
        //    result.Execute();
        //}
    }
}
