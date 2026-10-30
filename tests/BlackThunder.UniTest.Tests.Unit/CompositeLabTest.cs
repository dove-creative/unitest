using System;
using System.Collections.Generic;
using System.Linq;
using BlackThunder.UniTest;
using NUnit.Framework;

namespace BlackThunder.UniTest.Tests.Unit
{
    public class CompositeLabTest
    {
        private Lab<Model> _lab1;
        private Lab<Model> _lab2;
        private Lab<Model> _lab3;
        private CompositeLab<Model> _compositeLab;

        private Model _model;

        public struct Order
        {
            public bool[] Lab1Conditions;
            public bool[] Lab2Conditions;
        }

        [SetUp]
        public void SetUp()
        {
            _lab1 = new Lab<Model>("lab_1");
            _lab2 = new Lab<Model>("lab_2");
            _lab3 = new Lab<Model>("lab_3");
            _compositeLab = new CompositeLab<Model>(_lab1, _lab2);

            _model = new();
        }

        [TearDown]
        public void TearDown()
        {
            _lab1 = null;
            _lab2 = null;
            _lab3 = null;
            _compositeLab = null;

            _model = null;
        }
        public static IEnumerable<TestCaseData> GetTestCases()
        {
            yield return new TestCaseData(new Order
            {
                Lab1Conditions = new[] { true, true, true },
                Lab2Conditions = new[] { false, true, true }
            }).SetName("[1].Arranger Failure");
            yield return new TestCaseData(new Order
            {
                Lab1Conditions = new[] { false, true, true },
                Lab2Conditions = new[] { true, true, true }
            }).SetName("[0].Arranger Failure"); ;

            yield return new TestCaseData(new Order
            {
                Lab1Conditions = new[] { true, true, true },
                Lab2Conditions = new[] { true, false, true }
            }).SetName("[1].Actor Failure");
            yield return new TestCaseData(new Order
            {
                Lab1Conditions = new[] { true, false, true },
                Lab2Conditions = new[] { true, true, true }
            }).SetName("[0].Actor Failure");

            yield return new TestCaseData(new Order
            {
                Lab1Conditions = new[] { true, true, true },
                Lab2Conditions = new[] { true, true, false }
            }).SetName("[1].Asserter Failure");
            yield return new TestCaseData(new Order
            {
                Lab1Conditions = new[] { true, true, false },
                Lab2Conditions = new[] { true, true, true }
            }).SetName("[0].Asserter Failure");
            yield return new TestCaseData(new Order
            {
                Lab1Conditions = new[] { true, true, true },
                Lab2Conditions = new[] { true, true, true }
            }).SetName("All Succeeded");
        }
        public void SetLabs(Order order, Lab<Model> lab_1, Lab<Model> lab_2)
        {
            lab_1.Arranger = (_, _) =>
            {
                if (!order.Lab1Conditions[0])
                    throw new ProbeException($"{lab_1.Id}.Arranger Failure");
            };
            lab_1.Actor = (_, _) =>
            {
                if (!order.Lab1Conditions[1])
                    throw new ProbeException($"{lab_1.Id}.Actor Failure");
            };
            lab_1.Asserter = (_, _) =>
            {
                if (!order.Lab1Conditions[2])
                    throw new ProbeException($"{lab_1.Id}.Asserter Failure");
            };

            lab_2.Arranger = (_, _) =>
            {
                if (!order.Lab2Conditions[0])
                    throw new ProbeException($"{lab_2.Id}.Arranger Failure");
            };
            lab_2.Actor = (_, _) =>
            {
                if (!order.Lab2Conditions[1])
                    throw new ProbeException($"{lab_2.Id}.Actor Failure");
            };
            lab_2.Asserter = (_, _) =>
            {
                if (!order.Lab2Conditions[2])
                    throw new ProbeException($"{lab_2.Id}.Asserter Failure");
            };
        }


        public static IEnumerable<TestCaseData> GetProperties()
        {
            var ft = new[] { false, true };
            var rec = new[] { 10, 0, -1 };

            return
                from lab_1_ts in ft
                from lab_1_tu in ft
                from lab_1_rec in rec
                from lab_2_tu in ft
                from lab_2_ts in ft
                from lab_2_rec in rec
                select new TestCaseData(lab_1_ts, lab_1_tu, lab_1_rec, lab_2_ts, lab_2_tu, lab_2_rec);
        }


        [Test]
        public void Execute_NullModel()
        {
            // Act
            _compositeLab.Execute(null, out var ex);

            // Assert
            Assert.That(ex, Is.AssignableTo<ExecutionException>());
            Assert.That(ex.Message, Is.EqualTo("Model setting failed"));
        }


        [TestCaseSource(nameof(GetTestCases))]
        public void Execute_Model(Order order)
        {
            // Arrange
            SetLabs(order, _lab1, _lab2);
            Exception ex;

            // Act
            _compositeLab.Execute(_model, out var _ex);
            ex = _ex;

            // Assert
            if (!order.Lab2Conditions[0])
            {
                Assert.That(ex, Is.AssignableTo<ExecutionException>());

                ex = ex.InnerException;
                Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
                Assert.That(ex.Message, Is.EqualTo($"{_lab2.Id}.Arranger Failure"));

                Assert.That(_model.Continuable, Is.False);
                return;
            }
            if (!order.Lab1Conditions[0])
            {
                Assert.That(ex, Is.AssignableTo<ExecutionException>());

                ex = ex.InnerException;
                Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
                Assert.That(ex.Message, Is.EqualTo($"{_lab1.Id}.Arranger Failure"));

                Assert.That(_model.Continuable, Is.False);
                return;
            }


            if (!order.Lab2Conditions[1])
            {
                Assert.That(ex, Is.Null);
            }
            if (!order.Lab1Conditions[1])
            {
                Assert.That(ex, Is.AssignableTo<ExecutionException>());

                ex = ex.InnerException;
                Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
                Assert.That(ex.Message, Is.EqualTo($"{_lab1.Id}.Actor Failure"));

                Assert.That(_model.Continuable, Is.False);
                return;
            }


            if (!order.Lab2Conditions[2])
            {
                Assert.That(ex, Is.AssignableTo<ExecutionException>());

                ex = ex.InnerException;
                Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
                Assert.That(ex.Message, Is.EqualTo($"{_lab2.Id}.Asserter Failure"));

                Assert.That(_model.Continuable, Is.False);
                return;
            }
            if (!order.Lab1Conditions[2])
            {
                Assert.That(ex, Is.AssignableTo<ExecutionException>());

                ex = ex.InnerException;
                Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
                Assert.That(ex.Message, Is.EqualTo($"{_lab1.Id}.Asserter Failure"));

                Assert.That(_model.Continuable, Is.False);
                return;
            }

            Assert.That(ex, Is.Null);
            Assert.That(_model.Continuable, Is.True);
        }

        [Test]
        public void Execute_ExtensionExpectedException()
        {
            // Arrange
            _lab1.Actor = (_, _) => { };
            _lab2 = new Lab<Model>(
                "lab_2",
                expectedExceptionType: typeof(ProbeException));
            _compositeLab = new CompositeLab<Model>(_lab1, _lab2);

            // Act
            _compositeLab.Execute(_model, out var ex);

            // Assert
            Assert.That(ex, Is.AssignableTo<ExecutionException>());
            Assert.That(ex.InnerException, Is.AssignableTo<MissingExpectedException>());
            Assert.That(((MissingExpectedException)ex.InnerException).ExpectedExceptionType, Is.EqualTo(typeof(ProbeException)));
            Assert.That(_model.Continuable, Is.False);
        }


        [TestCaseSource(nameof(GetProperties))]
        public void CheckMetadata(
            bool lab_1_ts, bool lab_1_tu, int lab_1_rec, bool lab_2_ts, bool lab_2_tu, int lab_2_rec)
        {
            // Arrange
            _lab1.Arranger = (_, md) =>
            {
                md.ToUnsustainable = lab_1_ts;
                md.ToUncontinuable = lab_1_tu;
                md.RemainingExecutionCount = lab_1_rec;
            };
            _lab2.Arranger = (_, md) =>
            {
                md.ToUnsustainable = lab_2_ts;
                md.ToUncontinuable = lab_2_tu;
                md.RemainingExecutionCount = lab_2_rec;
            };

            // Act
            _compositeLab.Execute(_model, out var ex);

            // Assert
            Assert.That(ex, Is.Null);
            Assert.That(!_model.Sustainable, Is.EqualTo((lab_1_ts || lab_2_ts) || (lab_1_tu || lab_2_tu) || (lab_1_rec >= 0 || lab_2_rec >= 0)));
            Assert.That(!_model.Continuable, Is.EqualTo((lab_1_tu || lab_2_tu) || (lab_1_rec == 0 || lab_2_rec == 0)));

            if (lab_1_rec >= 0 || lab_2_rec >= 0)
            {
                Assert.That(_model.RemainingExecutionCount, Is.EqualTo(Math.Min(lab_1_rec >= 0 ? lab_1_rec : int.MaxValue, lab_2_rec >= 0 ? lab_2_rec : int.MaxValue)));
            }
            else
                Assert.That(_model.RemainingExecutionCount, Is.Null);
        }


        public static IEnumerable<TestCaseData> GetTestCases_Ordered()
        {
            foreach (var order in GetTestCases())
            {
                yield return new TestCaseData(order.OriginalArguments[0], 0).SetName(order.TestName + " case 0");
                yield return new TestCaseData(order.OriginalArguments[0], 1).SetName(order.TestName + " case 1");
                yield return new TestCaseData(order.OriginalArguments[0], 2).SetName(order.TestName + " case 2");
            }
        }

        [TestCaseSource(nameof(GetTestCases_Ordered))]
        public void Extend(Order order, int testOrder)
        {
            // Arrange
            _compositeLab = _compositeLab.Extend(_lab3);
            Exception ex;

            Lab<Model> lab_a;
            Lab<Model> lab_b;

            switch (testOrder)
            {
                case 0:
                    lab_a = _lab1;
                    lab_b = _lab2;
                    break;

                case 1:
                    lab_a = _lab2;
                    lab_b = _lab3;
                    break;

                case 2:
                    lab_a = _lab1;
                    lab_b = _lab3;
                    break;

                default:
                    throw new Exception();
            }

            SetLabs(order, lab_a, lab_b);

            // Act
            _compositeLab.Execute(_model, out var _ex);
            ex = _ex;

            // Assert
            if (!order.Lab2Conditions[0])
            {
                Assert.That(ex, Is.AssignableTo<ExecutionException>());

                ex = ex.InnerException;
                Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
                Assert.That(ex.Message, Is.EqualTo($"{lab_b.Id}.Arranger Failure"));

                Assert.That(_model.Continuable, Is.False);
                return;
            }
            if (!order.Lab1Conditions[0])
            {
                Assert.That(ex, Is.AssignableTo<ExecutionException>());

                ex = ex.InnerException;
                Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
                Assert.That(ex.Message, Is.EqualTo($"{lab_a.Id}.Arranger Failure"));

                Assert.That(_model.Continuable, Is.False);
                return;
            }


            if (!order.Lab1Conditions[1] && lab_a == _lab1)
            {
                Assert.That(ex, Is.AssignableTo<ExecutionException>());

                ex = ex.InnerException;
                Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
                Assert.That(ex.Message, Is.EqualTo($"{lab_a.Id}.Actor Failure"));

                Assert.That(_model.Continuable, Is.False);
                return;
            }


            if (!order.Lab2Conditions[2])
            {
                Assert.That(ex, Is.AssignableTo<ExecutionException>());

                ex = ex.InnerException;
                Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
                Assert.That(ex.Message, Is.EqualTo($"{lab_b.Id}.Asserter Failure"));

                Assert.That(_model.Continuable, Is.False);
                return;
            }
            if (!order.Lab1Conditions[2])
            {
                Assert.That(ex, Is.AssignableTo<ExecutionException>());

                ex = ex.InnerException;
                Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
                Assert.That(ex.Message, Is.EqualTo($"{lab_a.Id}.Asserter Failure"));

                Assert.That(_model.Continuable, Is.False);
                return;
            }

            Assert.That(ex, Is.Null);
            Assert.That(_model.Continuable, Is.True);
        }
    }
}
