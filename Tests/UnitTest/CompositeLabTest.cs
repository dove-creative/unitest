using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UniTest;

public class CompositeLabTest
{
    Lab<Model> _lab1;
    Lab<Model> _lab2;
    Lab<Model> _lab3;
    CompositeLab<Model> _compositeLab;

    Model _model;

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

    public struct Order
    {
        public bool[] lab_1_conditions;
        public bool[] lab_2_conditions;
    }
    public static IEnumerable<TestCaseData> GetTestCases()
    {
        yield return new TestCaseData(new Order
        {
            lab_1_conditions = new[] { true, true, true },
            lab_2_conditions = new[] { false, true, true }
        }).SetName("[1].Arranger Failure");
        yield return new TestCaseData(new Order
        {
            lab_1_conditions = new[] { false, true, true },
            lab_2_conditions = new[] { true, true, true }
        }).SetName("[0].Arranger Failure"); ;

        yield return new TestCaseData(new Order
        {
            lab_1_conditions = new[] { true, true, true },
            lab_2_conditions = new[] { true, false, true }
        }).SetName("[1].Actor Failure");
        yield return new TestCaseData(new Order
        {
            lab_1_conditions = new[] { true, false, true },
            lab_2_conditions = new[] { true, true, true }
        }).SetName("[0].Actor Failure");

        yield return new TestCaseData(new Order
        {
            lab_1_conditions = new[] { true, true, true },
            lab_2_conditions = new[] { true, true, false }
        }).SetName("[1].Asserter Failure");
        yield return new TestCaseData(new Order
        {
            lab_1_conditions = new[] { true, true, false },
            lab_2_conditions = new[] { true, true, true }
        }).SetName("[0].Asserter Failure");
        yield return new TestCaseData(new Order
        {
            lab_1_conditions = new[] { true, true, true },
            lab_2_conditions = new[] { true, true, true }
        }).SetName("All Succeeded");
    }
    public void SetLabs(Order order, Lab<Model> lab_1, Lab<Model> lab_2)
    {
        lab_1.Arranger = (_, _) =>
        {
            if (!order.lab_1_conditions[0])
                throw new ProbeException($"{lab_1.ID}.Arranger Failure");
        };
        lab_1.Actor = (_, _) =>
        {
            if (!order.lab_1_conditions[1])
                throw new ProbeException($"{lab_1.ID}.Actor Failure");
        };
        lab_1.Asserter = (_, _) =>
        {
            if (!order.lab_1_conditions[2])
                throw new ProbeException($"{lab_1.ID}.Asserter Failure");
        };

        lab_2.Arranger = (_, _) =>
        {
            if (!order.lab_2_conditions[0])
                throw new ProbeException($"{lab_2.ID}.Arranger Failure");
        };
        lab_2.Actor = (_, _) =>
        {
            if (!order.lab_2_conditions[1])
                throw new ProbeException($"{lab_2.ID}.Actor Failure");
        };
        lab_2.Asserter = (_, _) =>
        {
            if (!order.lab_2_conditions[2])
                throw new ProbeException($"{lab_2.ID}.Asserter Failure");
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
        if (!order.lab_2_conditions[0])
        {
            Assert.That(ex, Is.AssignableTo<ExecutionException>());

            ex = ex.InnerException;
            Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
            Assert.That(ex.Message, Is.EqualTo($"{_lab2.ID}.Arranger Failure"));

            Assert.That(_model.Continuable, Is.False);
            return;
        }
        if (!order.lab_1_conditions[0])
        {
            Assert.That(ex, Is.AssignableTo<ExecutionException>());

            ex = ex.InnerException;
            Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
            Assert.That(ex.Message, Is.EqualTo($"{_lab1.ID}.Arranger Failure"));

            Assert.That(_model.Continuable, Is.False);
            return;
        }


        if (!order.lab_2_conditions[1])
        {
            Assert.That(ex, Is.Null);
        }
        if (!order.lab_1_conditions[1])
        {
            Assert.That(ex, Is.AssignableTo<ExecutionException>());

            ex = ex.InnerException;
            Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
            Assert.That(ex.Message, Is.EqualTo($"{_lab1.ID}.Actor Failure"));

            Assert.That(_model.Continuable, Is.False);
            return;
        }


        if (!order.lab_2_conditions[2])
        {
            Assert.That(ex, Is.AssignableTo<ExecutionException>());

            ex = ex.InnerException;
            Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
            Assert.That(ex.Message, Is.EqualTo($"{_lab2.ID}.Asserter Failure"));

            Assert.That(_model.Continuable, Is.False);
            return;
        }
        if (!order.lab_1_conditions[2])
        {
            Assert.That(ex, Is.AssignableTo<ExecutionException>());

            ex = ex.InnerException;
            Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
            Assert.That(ex.Message, Is.EqualTo($"{_lab1.ID}.Asserter Failure"));

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
        if (!order.lab_2_conditions[0])
        {
            Assert.That(ex, Is.AssignableTo<ExecutionException>());

            ex = ex.InnerException;
            Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
            Assert.That(ex.Message, Is.EqualTo($"{lab_b.ID}.Arranger Failure"));

            Assert.That(_model.Continuable, Is.False);
            return;
        }
        if (!order.lab_1_conditions[0])
        {
            Assert.That(ex, Is.AssignableTo<ExecutionException>());

            ex = ex.InnerException;
            Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
            Assert.That(ex.Message, Is.EqualTo($"{lab_a.ID}.Arranger Failure"));

            Assert.That(_model.Continuable, Is.False);
            return;
        }


        if (!order.lab_1_conditions[1] && lab_a == _lab1)
        {
            Assert.That(ex, Is.AssignableTo<ExecutionException>());

            ex = ex.InnerException;
            Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
            Assert.That(ex.Message, Is.EqualTo($"{lab_a.ID}.Actor Failure"));

            Assert.That(_model.Continuable, Is.False);
            return;
        }


        if (!order.lab_2_conditions[2])
        {
            Assert.That(ex, Is.AssignableTo<ExecutionException>());

            ex = ex.InnerException;
            Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
            Assert.That(ex.Message, Is.EqualTo($"{lab_b.ID}.Asserter Failure"));

            Assert.That(_model.Continuable, Is.False);
            return;
        }
        if (!order.lab_1_conditions[2])
        {
            Assert.That(ex, Is.AssignableTo<ExecutionException>());

            ex = ex.InnerException;
            Assert.That(ex, Is.AssignableTo<ProbeException>(), ex?.Message ?? "-");
            Assert.That(ex.Message, Is.EqualTo($"{lab_a.ID}.Asserter Failure"));

            Assert.That(_model.Continuable, Is.False);
            return;
        }

        Assert.That(ex, Is.Null);
        Assert.That(_model.Continuable, Is.True);
    }
}
