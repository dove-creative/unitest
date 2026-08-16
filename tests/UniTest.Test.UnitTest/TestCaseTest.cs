using System.Reflection;
using NUnit.Framework;
using UniTest;

public class TestCaseTest
{
    (bool include, object[] definitions)[] GetTestCases(TestCase tc)
    {
        return ((bool, object[])[])typeof(TestCase)
            .GetProperty("TestCases", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(tc);
    }


    [Test]
    public void Append_WithEmpty()
    {
        // Arrange
        var tc = new TestCase();

        // Act
        tc = tc.Append(0);

        // Assert
        var actual = GetTestCases(tc);
        Assert.That(actual.Length, Is.EqualTo(1));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));
    }
    [Test]
    public void Append_WithNotEmpth()
    {
        // Arrange
        var tc = new TestCase(0);

        // Act
        tc = tc.Append(1);

        // Assert
        var actual = GetTestCases(tc);
        Assert.That(actual.Length, Is.EqualTo(2));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.True);
        Assert.That(new[] { 1 }, Is.EquivalentTo(actual[1].definitions));
    }



    [Test]
    public void Confine_Last()
    {
        // Arrange
        var tc = new TestCase(0, 1);

        // Act
        tc = tc.Confine(1, 10);

        // Assert
        var actual = GetTestCases(tc);
        Assert.That(actual.Length, Is.EqualTo(2));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.True);
        Assert.That(new[] { 10 }, Is.EquivalentTo(actual[1].definitions));
    }
    [Test]
    public void Confine_First()
    {
        // Arrange
        var tc = new TestCase(0, 1);

        // Act
        tc = tc.Confine(0, 10);

        // Assert
        var actual = GetTestCases(tc);
        Assert.That(actual.Length, Is.EqualTo(2));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 10 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.True);
        Assert.That(new[] { 1 }, Is.EquivalentTo(actual[1].definitions));
    }
    [Test]
    public void Confine_Extend()
    {
        // Arrange
        var tc = new TestCase(0);

        // Act
        tc = tc.Confine(1, 10);

        // Assert
        var actual = GetTestCases(tc);
        Assert.That(actual.Length, Is.EqualTo(2));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.True);
        Assert.That(new[] { 10 }, Is.EquivalentTo(actual[1].definitions));
    }



    [Test]
    public void Include_New()
    {
        // Arrange
        var tc = new TestCase(0);

        // Act
        tc = tc.Include(1, 1);

        // Assert
        var actual = GetTestCases(tc);
        Assert.That(actual.Length, Is.EqualTo(2));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.True);
        Assert.That(new[] { 1 }, Is.EquivalentTo(actual[1].definitions));
    }
    [Test]
    public void Include_Override_Positive()
    {
        // Arrange
        var tc = new TestCase(0, 1);

        // Act
        tc = tc.Include(1, 10);

        // Assert
        var actual = GetTestCases(tc);
        Assert.That(actual.Length, Is.EqualTo(2));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.True);
        Assert.That(new[] { 1, 10 }, Is.EquivalentTo(actual[1].definitions));
    }
    [Test]
    public void Include_Override_Negative()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (false, new object[] { 1, 10 }) });

        // Act
        tc = tc.Include(1, 10);

        // Assert
        var actual = GetTestCases(tc);
        Assert.That(actual.Length, Is.EqualTo(2));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.False);
        Assert.That(new[] { 1 }, Is.EquivalentTo(actual[1].definitions));
    }



    [Test]
    public void Exclude_New()
    {
        // Arrange
        var tc = new TestCase(0);

        // Act
        tc = tc.Exclude(1, 1);

        // Assert
        var actual = GetTestCases(tc);
        Assert.That(actual.Length, Is.EqualTo(2));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.False);
        Assert.That(new[] { 1 }, Is.EquivalentTo(actual[1].definitions));
    }
    [Test]
    public void Exclude_Override_Positive()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (false, new object[] { 1 }) });

        // Act
        tc = tc.Exclude(1, 10);

        // Assert
        var actual = GetTestCases(tc);
        Assert.That(actual.Length, Is.EqualTo(2));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.False);
        Assert.That(new[] { 1, 10 }, Is.EquivalentTo(actual[1].definitions));
    }
    [Test]
    public void Exclude_Override_Negative()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (true, new object[] { 1, 10 }) });

        // Act
        tc = tc.Exclude(1, 10);

        // Assert
        var actual = GetTestCases(tc);
        Assert.That(actual.Length, Is.EqualTo(2));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.True);
        Assert.That(new[] { 1 }, Is.EquivalentTo(actual[1].definitions));
    }



    [Test]
    public void Confineable_SingleParams_Positive_1()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (true, new object[] { 1, 10 }) });
        

        // Act
        var confineable = tc.Confineable(1, out var _tc, 10);


        // Assert
        Assert.That(confineable, Is.True);

        var actual = GetTestCases(_tc);
        Assert.That(actual.Length, Is.EqualTo(2));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.True);
        Assert.That(new[] { 10 }, Is.EquivalentTo(actual[1].definitions));
    }
    [Test]
    public void Confineable_SingleParams_Positive_2()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (true, new object[] { 1, 10 }) });


        // Act
        var confineable = tc.Confineable(1, out var _tc, -1);

        // Assert
        Assert.That(confineable, Is.False);
    }
    [Test]
    public void Confineable_SingleParams_Positive_3()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (true, new object[] { 1, 10 }) });


        // Act
        var confineable = tc.Confineable(2, out var _tc, 2);


        // Assert
        Assert.That(confineable, Is.True);

        var actual = GetTestCases(_tc);
        Assert.That(actual.Length, Is.EqualTo(3));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.True);
        Assert.That(new[] { 1, 10 }, Is.EquivalentTo(actual[1].definitions));

        Assert.That(actual[2].include, Is.True);
        Assert.That(new[] { 2 }, Is.EquivalentTo(actual[2].definitions));
    }



    [Test]
    public void Confineable_SingleParams_Negative_1()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (false, new object[] { 1, 10 }) });


        // Act
        var confineable = tc.Confineable(1, out var _tc, 2);


        // Assert
        Assert.That(confineable, Is.True);

        var actual = GetTestCases(_tc);
        Assert.That(actual.Length, Is.EqualTo(2));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.True);
        Assert.That(new[] { 2 }, Is.EquivalentTo(actual[1].definitions));
    }
    [Test]
    public void Confineable_SingleParams_Negative_2()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (false, new object[] { 1, 10 }) });


        // Act
        var confineable = tc.Confineable(1, out var _tc, 1);


        // Assert
        Assert.That(confineable, Is.False);
    }
    [Test]
    public void Confineable_SingleParams_Negative_3()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (false, new object[] { 1, 10 }) });


        // Act
        var confineable = tc.Confineable(2, out var _tc, 2);


        // Assert
        Assert.That(confineable, Is.True);

        var actual = GetTestCases(_tc);
        Assert.That(actual.Length, Is.EqualTo(3));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.False);
        Assert.That(new[] { 1, 10 }, Is.EquivalentTo(actual[1].definitions));

        Assert.That(actual[2].include, Is.True);
        Assert.That(new[] { 2 }, Is.EquivalentTo(actual[2].definitions));
    }



    [Test]
    public void Confineable_MultiParams_Positive()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (true, new object[] { 1, 10 }) });


        // Act & Asserts
        Assert.That(tc.Confineable(1, 1, 10), Is.True);
        Assert.That(tc.Confineable(1, 1, 2), Is.False);
        Assert.That(tc.Confineable(2, 100, 200), Is.True);
    }

    [Test]
    public void Confineable_MultiParams_Negative()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (false, new object[] { 1, 10 }) });


        // Act & Asserts
        Assert.That(tc.Confineable(1, 2, 3), Is.True);
        Assert.That(tc.Confineable(1, 2, 10), Is.False);
        Assert.That(tc.Confineable(2, 100, 200), Is.True);
    }



    [Test]
    public void ConfineableExcept_Positive_1()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (true, new object[] { 1, 10 }) });


        // Act
        var confineable = tc.ConfineableExcept(1, out var _tc, 2, 3);


        // Assert
        Assert.That(confineable, Is.True);

        var actual = GetTestCases(_tc);
        Assert.That(actual.Length, Is.EqualTo(2));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.True);
        Assert.That(new[] { 1, 10 }, Is.EquivalentTo(actual[1].definitions));
    }
    [Test]
    public void ConfineableExcept_Positive_2()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (true, new object[] { 1, 10 }) });


        // Act
        var confineable = tc.ConfineableExcept(1, out var _tc, 1, 2);


        // Assert
        Assert.That(confineable, Is.True);

        var actual = GetTestCases(_tc);
        Assert.That(actual.Length, Is.EqualTo(2));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.True);
        Assert.That(new[] { 10 }, Is.EquivalentTo(actual[1].definitions));
    }
    [Test]
    public void ConfineableExcept_Positive_3()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (true, new object[] { 1, 10 }) });


        // Act
        var confineable = tc.ConfineableExcept(1, out _, 1, 10, 100);

        // Assert
        Assert.That(confineable, Is.False);
    }
    [Test]
    public void ConfineableExcept_Positive_4()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (true, new object[] { 1, 10 }) });


        // Act
        var confineable = tc.ConfineableExcept(2, out var _tc, 3, 4);


        // Assert
        Assert.That(confineable, Is.True);

        var actual = GetTestCases(_tc);
        Assert.That(actual.Length, Is.EqualTo(3));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.True);
        Assert.That(new[] { 1, 10 }, Is.EquivalentTo(actual[1].definitions));

        Assert.That(actual[2].include, Is.False);
        Assert.That(new[] { 3, 4 }, Is.EquivalentTo(actual[2].definitions));
    }



    [Test]
    public void ConfineableExcept_Negative_1()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (false, new object[] { 1, 10 }) });


        // Act
        var confineable = tc.ConfineableExcept(1, out var _tc, 20, 30);


        // Assert
        Assert.That(confineable, Is.True);

        var actual = GetTestCases(_tc);
        Assert.That(actual.Length, Is.EqualTo(2));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.False);
        Assert.That(new[] { 1, 10, 20, 30 }, Is.EquivalentTo(actual[1].definitions));
    }
    [Test]
    public void ConfineableExcept_Negative_2()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (false, new object[] { 1, 10 }) });


        // Act
        var confineable = tc.ConfineableExcept(1, out var _tc, 1, 2);


        // Assert
        Assert.That(confineable, Is.True);

        var actual = GetTestCases(_tc);
        Assert.That(actual.Length, Is.EqualTo(2));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.False);
        Assert.That(new[] { 1, 2, 10 }, Is.EquivalentTo(actual[1].definitions));
    }
    [Test]
    public void ConfineableExcept_Negative_3()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (false, new object[] { 1, 10 }) });


        // Act
        var confineable = tc.ConfineableExcept(1, out var _tc, 1);

        // Assert
        Assert.That(confineable, Is.True);

        var actual = GetTestCases(_tc);
        Assert.That(actual.Length, Is.EqualTo(2));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.False);
        Assert.That(new[] { 1, 10 }, Is.EquivalentTo(actual[1].definitions));
    }
    [Test]
    public void ConfineableExcept_Negative_4()
    {
        // Arrange
        var tc = new TestCase(
            new[] { (true, new object[] { 0 }), (false, new object[] { 1, 10 }) });


        // Act
        var confineable = tc.ConfineableExcept(2, out var _tc, 3, 4);


        // Assert
        Assert.That(confineable, Is.True);

        var actual = GetTestCases(_tc);
        Assert.That(actual.Length, Is.EqualTo(3));

        Assert.That(actual[0].include, Is.True);
        Assert.That(new[] { 0 }, Is.EquivalentTo(actual[0].definitions));

        Assert.That(actual[1].include, Is.False);
        Assert.That(new[] { 1, 10 }, Is.EquivalentTo(actual[1].definitions));

        Assert.That(actual[2].include, Is.False);
        Assert.That(new[] { 3, 4 }, Is.EquivalentTo(actual[2].definitions));
    }
}
