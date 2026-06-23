using System;
using System.Reflection;
using NUnit.Framework;
using UniTest;

public class ModelTest
{
    readonly string id = "Test";
    Model model;

    [SetUp]
    public void SetUp()
    {
        model = new Model();
    }

    [TearDown]
    public void TearDown()
    {
        model = null;
    }
    
    void DoExecute(string labID)
    {
        try
        {
            var method = typeof(Model).GetMethod("DoExecute", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(model, new[] { labID });
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException;
        }
    }
    void CheckModel(bool sustainable, bool continuable, int? remainingExecutionCount)
    {
        Assert.AreEqual(sustainable, model.Sustainable);
        Assert.AreEqual(continuable, model.Continuable);
        Assert.AreEqual(remainingExecutionCount, model.RemainingExecutionCount);
    }



    [Test]
    public void StateTransition_ToNonsustainable()
    {
        // Act
        model.Sustainable = false;
        
        // Assert
        CheckModel(false, true, null);
    }
    [Test]
    public void StateTransition_ToNoncontinuable()
    {
        // Act
        model.Continuable = false;

        // Assert
        CheckModel(false, false, null);
    }
    [Test]
    public void StateTransition_SetRemainingExecutionCount()
    {
        // Arrange
        var count = 1;

        // Act
        model.RemainingExecutionCount = count;

        // Assert
        CheckModel(false, true, count);
    }



    #region Do Execute
    [Test]
    public void DoExecute_Idle_Single()
    {
        // Act
        DoExecute(id);

        // Assert
        Assert.AreEqual(id, model.GetExecutionHistory());
        CheckModel(true, true, null);
    }
    [Test]
    public void DoExecute_Idle_Multi()
    {
        // Arrange
        var id = new[] { "Test01", "Test02" };
        DoExecute(id[0]);

        // Act
        DoExecute(id[1]);

        // Assert
        Assert.AreEqual(string.Join(Model.Separator, id), model.GetExecutionHistory());
        CheckModel(true, true, null);
    }


    [Test]
    public void DoExecute_Nonsustainable_Single()
    {
        // Arange
        model.Sustainable = false;

        // Act
        DoExecute(id);

        // Assert
        Assert.AreEqual(id, model.GetExecutionHistory());
        CheckModel(false, true, null);
    }
    [Test]
    public void DoExecute_Nonsustainable_Multi()
    {
        // Arrange
        model.Sustainable = false;

        var id = new[] { "Test01", "Test02" };
        DoExecute(id[0]);

        // Act
        DoExecute(id[1]);

        // Assert
        Assert.AreEqual(string.Join(Model.Separator, id), model.GetExecutionHistory());
        CheckModel(false, true, null);
    }


    [Test]
    public void DoExecute_Limited_Continuable()
    {
        // Arrange
        var count = 10;
        model.RemainingExecutionCount = count;

        // Act
        DoExecute(id);

        // Assert
        Assert.AreEqual(id, model.GetExecutionHistory());
        CheckModel(false, true, count - 1);
    }
    [Test]
    public void DoExecute_Limited_Noncontinuable()
    {
        // Arrange
        var count = 1;
        model.RemainingExecutionCount = count;

        // Act
        DoExecute(id);

        // Assert
        Assert.AreEqual(id, model.GetExecutionHistory());
        CheckModel(false, false, count - 1);
    }


    [Test]
    public void DoExecute_Noncontinuable()
    {
        // Arrange
        model.RemainingExecutionCount = 0;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => DoExecute(id));
    }
    #endregion



    #region To Noncontinuable
    [Test]
    public void ToNonsustainable_Idle()
    {
        // Act
        model.Sustainable = false;

        // Assert
        CheckModel(false, true, null);
    }
    [Test]
    public void ToNonsustainable_Nonsustainable()
    {
        // Arrange
        model.Sustainable = false;

        // Act
        model.Sustainable = false;

        // Assert
        CheckModel(false, true, null);
    }
    [Test]
    public void ToNonsustainable_Limited()
    {
        // Arrange
        var count = 10;
        model.RemainingExecutionCount = count;

        // Act
        model.Sustainable = false;

        // Assert
        CheckModel(false, true, count);
    }
    [Test]
    public void ToNonsustainable_Noncontinuable()
    {
        // Arrange
        model.Continuable = false;

        // Act
        model.Sustainable = false;

        // Assert
        CheckModel(false, false, null);
    }
    #endregion


    
    #region To Noncontinuable
    [Test]
    public void ToNoncontinuable_Idle()
    {
        // Act
        model.Continuable = false;

        // Assert
        CheckModel(false, false, null);
    }
    [Test]
    public void ToNoncontinuable_Nonsustainable()
    {
        // Arrange
        model.Sustainable = false;

        // Act
        model.Continuable = false;

        // Assert
        CheckModel(false, false, null);
    }
    [Test]
    public void ToNoncontinuable_Limited()
    {
        // Arrange
        var count = 10;
        model.RemainingExecutionCount = count;

        // Act
        model.Continuable = false;

        // Assert
        CheckModel(false, false, count);
    }
    [Test]
    public void ToNoncontinuable_Noncontinuable()
    {
        // Arrange
        model.Continuable = false;

        // Act
        model.Continuable = false;

        // Assert
        CheckModel(false, false, null);
    }


    [Test]
    public void ToNoncontinuable_Idle_False()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => model.Continuable = true);
    }
    [Test]
    public void ToNoncontinuable_Nonsustainable_False()
    {
        // Arrange
        model.Sustainable = false;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => model.Continuable = true);
    }
    [Test]
    public void ToNoncontinuable_Limited_False()
    {
        // Arrange
        model.RemainingExecutionCount = 10;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => model.Continuable = true);
    }
    [Test]
    public void ToNoncontinuable_Noncontinuable_False()
    {
        // Arrange
        model.Continuable = false;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => model.Continuable = true);
    }
    #endregion



    #region Set Remaining Execution Count
    [Test]
    public void SetRemainingExecutionCount_Idle_Null()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => model.RemainingExecutionCount = null);
    }
    [Test]
    public void SetRemainingExecutionCount_Nonsustainable_Null()
    {
        // Arrange
        model.Sustainable = false;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => model.RemainingExecutionCount = null);
    }
    [Test]
    public void SetRemainingExecutionCount_Limited_Null()
    {
        // Arrange
        model.RemainingExecutionCount = 10;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => model.RemainingExecutionCount = null);
    }
    [Test]
    public void SetRemainingExecutionCount_Noncontinuable_Null()
    {
        // Arrange
        model.Continuable = false;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => model.RemainingExecutionCount = null);
    }


    [TestCase(10), TestCase(0)]
    public void SetRemainingExecutionCount_Idle(int count)
    {
        // Act
        model.RemainingExecutionCount = count;

        // Assert
        CheckModel(false, count > 0, count);
    }
    [TestCase(10), TestCase(0)]
    public void SetRemainingExecutionCount_Nonsustainable(int count)
    {
        // Arrange
        model.Sustainable = false;

        // Act
        model.RemainingExecutionCount = count;

        // Assert
        CheckModel(false, count > 0, count);
    }
    [Test]
    public void SetRemainingExecutionCount_Limited()
    {
        // Arrange
        var count = 10;
        model.RemainingExecutionCount = count;

        // Act
        model.RemainingExecutionCount = count + 1;

        // Assert
        CheckModel(false, true, count);
    }
    [Test]
    public void SetRemainingExecutionCount_Noncontinuable()
    {
        // Arrange
        var count = 10;
        model.Continuable = false;

        // Act
        model.RemainingExecutionCount = count;

        // Assert
        CheckModel(false, false, count);
    }


    [TestCase(5), TestCase(0)]
    public void SetRemainingExecutionCount_Limited_Smaller(int count)
    {
        // Arrange
        model.RemainingExecutionCount = 10;

        // Act
        model.RemainingExecutionCount = count;

        // Assert
        CheckModel(false, count > 0, count);
    }
    [TestCase(5), TestCase(0)]
    public void SetRemainingExecutionCount_Limited_Smaller_Noncontinuable(int count)
    {
        // Arrange
        model.RemainingExecutionCount = 10;

        // Act
        model.RemainingExecutionCount = count;

        // Assert
        CheckModel(false, count > 0, count);
    }
    #endregion
}
