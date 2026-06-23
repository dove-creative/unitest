using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UniTest;

public class CompositeLabTest
{
    Lab<Model> lab_1;
    Lab<Model> lab_2;
    Lab<Model> lab_3;
    CompositeLab<Model> compositeLab;

    Model model;

    [SetUp]
    public void SetUp()
    {
        lab_1 = new Lab<Model>("lab_1");
        lab_2 = new Lab<Model>("lab_2");
        lab_3 = new Lab<Model>("lab_3");
        compositeLab = new CompositeLab<Model>(lab_1, lab_2);

        model = new();
    }

    [TearDown]
    public void TearDown()
    {
        lab_1 = null;
        lab_2 = null;
        lab_3 = null;
        compositeLab = null;

        model = null;
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
        compositeLab.Execute(null, out var ex);

        // Assert
        Assert.IsAssignableFrom<ExecutionException>(ex);
        Assert.AreEqual("Model setting failed", ex.Message);
    }




    [TestCaseSource(nameof(GetTestCases))]
    public void Execute_Model(Order order)
    {
        // Arrange
        SetLabs(order, lab_1, lab_2);
        Exception ex;

        // Act
        compositeLab.Execute(model, out var _ex);
        ex = _ex;

        // Assert
        if (!order.lab_2_conditions[0])
        {
            Assert.IsAssignableFrom<ExecutionException>(ex);

            ex = ex.InnerException;
            Assert.IsAssignableFrom<ProbeException>(ex, ex?.Message ?? "-");
            Assert.AreEqual($"{lab_2.ID}.Arranger Failure", ex.Message);

            Assert.IsFalse(model.Continuable);
            return;
        }
        if (!order.lab_1_conditions[0])
        {
            Assert.IsAssignableFrom<ExecutionException>(ex);

            ex = ex.InnerException;
            Assert.IsAssignableFrom<ProbeException>(ex, ex?.Message ?? "-");
            Assert.AreEqual($"{lab_1.ID}.Arranger Failure", ex.Message);

            Assert.IsFalse(model.Continuable);
            return;
        }


        if (!order.lab_2_conditions[1])
        {
            Assert.IsNull(ex);
        }
        if (!order.lab_1_conditions[1])
        {
            Assert.IsAssignableFrom<ExecutionException>(ex);

            ex = ex.InnerException;
            Assert.IsAssignableFrom<ProbeException>(ex, ex?.Message ?? "-");
            Assert.AreEqual($"{lab_1.ID}.Actor Failure", ex.Message);

            Assert.IsFalse(model.Continuable);
            return;
        }


        if (!order.lab_2_conditions[2])
        {
            Assert.IsAssignableFrom<ExecutionException>(ex);

            ex = ex.InnerException;
            Assert.IsAssignableFrom<ProbeException>(ex, ex?.Message ?? "-");
            Assert.AreEqual($"{lab_2.ID}.Asserter Failure", ex.Message);

            Assert.IsFalse(model.Continuable);
            return;
        }
        if (!order.lab_1_conditions[2])
        {
            Assert.IsAssignableFrom<ExecutionException>(ex);

            ex = ex.InnerException;
            Assert.IsAssignableFrom<ProbeException>(ex, ex?.Message ?? "-");
            Assert.AreEqual($"{lab_1.ID}.Asserter Failure", ex.Message);

            Assert.IsFalse(model.Continuable);
            return;
        }

        Assert.IsNull(ex);
        Assert.IsTrue(model.Continuable);
    }

    [Test]
    public void Execute_ExtensionExpectedException()
    {
        // Arrange
        lab_1.Actor = (_, _) => { };
        lab_2 = new Lab<Model>(
            "lab_2",
            expectedExceptionType: typeof(ProbeException));
        compositeLab = new CompositeLab<Model>(lab_1, lab_2);

        // Act
        compositeLab.Execute(model, out var ex);

        // Assert
        Assert.IsAssignableFrom<ExecutionException>(ex);
        Assert.IsAssignableFrom<MissingExpectedException>(ex.InnerException);
        Assert.AreEqual(typeof(ProbeException),
            ((MissingExpectedException)ex.InnerException).ExpectedExceptionType);
        Assert.IsFalse(model.Continuable);
    }


    [TestCaseSource(nameof(GetProperties))]
    public void CheckMetadata(
        bool lab_1_ts, bool lab_1_tu, int lab_1_rec, bool lab_2_ts, bool lab_2_tu, int lab_2_rec)
    {
        // Arrange
        lab_1.Arranger = (_, md) =>
        {
            md.ToUnsustainable = lab_1_ts;
            md.ToUncontinuable = lab_1_tu;
            md.RemainingExecutionCount = lab_1_rec;
        };
        lab_2.Arranger = (_, md) =>
        {
            md.ToUnsustainable = lab_2_ts;
            md.ToUncontinuable = lab_2_tu;
            md.RemainingExecutionCount = lab_2_rec;
        };

        // Act
        compositeLab.Execute(model, out var ex);

        // Assert
        Assert.IsNull(ex);
        Assert.AreEqual((lab_1_ts || lab_2_ts) || (lab_1_tu || lab_2_tu) || (lab_1_rec >= 0 || lab_2_rec >= 0), !model.Sustainable);
        Assert.AreEqual((lab_1_tu || lab_2_tu) || (lab_1_rec == 0 || lab_2_rec == 0), !model.Continuable);

        if (lab_1_rec >= 0 || lab_2_rec >= 0)
        {
            Assert.AreEqual(
                Math.Min(lab_1_rec >= 0 ? lab_1_rec : int.MaxValue, lab_2_rec >= 0 ? lab_2_rec : int.MaxValue),
                model.RemainingExecutionCount);
        }
        else
            Assert.IsNull(model.RemainingExecutionCount);
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
        compositeLab = compositeLab.Extend(lab_3);
        Exception ex;

        Lab<Model> lab_a;
        Lab<Model> lab_b;

        switch (testOrder)
        {
            case 0:
                lab_a = lab_1;
                lab_b = lab_2;
                break;

            case 1:
                lab_a = lab_2;
                lab_b = lab_3;
                break;

            case 2:
                lab_a = lab_1;
                lab_b = lab_3;
                break;

            default:
                throw new Exception();
        }

        SetLabs(order, lab_a, lab_b);

        // Act
        compositeLab.Execute(model, out var _ex);
        ex = _ex;

        // Assert
        if (!order.lab_2_conditions[0])
        {
            Assert.IsAssignableFrom<ExecutionException>(ex);

            ex = ex.InnerException;
            Assert.IsAssignableFrom<ProbeException>(ex, ex?.Message ?? "-");
            Assert.AreEqual($"{lab_b.ID}.Arranger Failure", ex.Message); 

            Assert.IsFalse(model.Continuable);
            return;
        }
        if (!order.lab_1_conditions[0])
        {
            Assert.IsAssignableFrom<ExecutionException>(ex);

            ex = ex.InnerException;
            Assert.IsAssignableFrom<ProbeException>(ex, ex?.Message ?? "-");
            Assert.AreEqual($"{lab_a.ID}.Arranger Failure", ex.Message);

            Assert.IsFalse(model.Continuable);
            return;
        }


        if (!order.lab_1_conditions[1] && lab_a == lab_1)
        {
            Assert.IsAssignableFrom<ExecutionException>(ex);

            ex = ex.InnerException;
            Assert.IsAssignableFrom<ProbeException>(ex, ex?.Message ?? "-");
            Assert.AreEqual($"{lab_a.ID}.Actor Failure", ex.Message);

            Assert.IsFalse(model.Continuable);
            return;
        }


        if (!order.lab_2_conditions[2])
        {
            Assert.IsAssignableFrom<ExecutionException>(ex);

            ex = ex.InnerException;
            Assert.IsAssignableFrom<ProbeException>(ex, ex?.Message ?? "-");
            Assert.AreEqual($"{lab_b.ID}.Asserter Failure", ex.Message);

            Assert.IsFalse(model.Continuable);
            return;
        }
        if (!order.lab_1_conditions[2])
        {
            Assert.IsAssignableFrom<ExecutionException>(ex);

            ex = ex.InnerException;
            Assert.IsAssignableFrom<ProbeException>(ex, ex?.Message ?? "-");
            Assert.AreEqual($"{lab_a.ID}.Asserter Failure", ex.Message);

            Assert.IsFalse(model.Continuable);
            return;
        }

        Assert.IsNull(ex);
        Assert.IsTrue(model.Continuable);
    }
}
