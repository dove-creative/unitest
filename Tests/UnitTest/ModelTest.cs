using System;
using System.Reflection;
using NUnit.Framework;
using UniTest;

public class ModelTest
{
    readonly string _id = "Test";
    Model _model;

    [SetUp]
    public void SetUp()
    {
        _model = new Model();
    }

    [TearDown]
    public void TearDown()
    {
        _model = null;
    }
    
    void DoExecute(string labID)
    {
        try
        {
            var method = typeof(Model).GetMethod("DoExecute", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(_model, new[] { labID });
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException;
        }
    }
    void CheckModel(bool sustainable, bool continuable, int? remainingExecutionCount)
    {
        Assert.That(_model.Sustainable, Is.EqualTo(sustainable));
        Assert.That(_model.Continuable, Is.EqualTo(continuable));
        Assert.That(_model.RemainingExecutionCount, Is.EqualTo(remainingExecutionCount));
    }



    [Test]
    public void StateTransition_ToNonsustainable()
    {
        // Act
        _model.Sustainable = false;
        
        // Assert
        CheckModel(false, true, null);
    }
    [Test]
    public void StateTransition_ToNoncontinuable()
    {
        // Act
        _model.Continuable = false;

        // Assert
        CheckModel(false, false, null);
    }
    [Test]
    public void StateTransition_SetRemainingExecutionCount()
    {
        // Arrange
        var count = 1;

        // Act
        _model.RemainingExecutionCount = count;

        // Assert
        CheckModel(false, true, count);
    }



    #region Do Execute
    [Test]
    public void DoExecute_Idle_Single()
    {
        // Act
        DoExecute(_id);

        // Assert
        Assert.That(_model.GetExecutionHistory(), Is.EqualTo(_id));
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
        Assert.That(_model.GetExecutionHistory(), Is.EqualTo(string.Join(Model.Separator, id)));
        CheckModel(true, true, null);
    }


    [Test]
    public void DoExecute_Nonsustainable_Single()
    {
        // Arange
        _model.Sustainable = false;

        // Act
        DoExecute(_id);

        // Assert
        Assert.That(_model.GetExecutionHistory(), Is.EqualTo(_id));
        CheckModel(false, true, null);
    }
    [Test]
    public void DoExecute_Nonsustainable_Multi()
    {
        // Arrange
        _model.Sustainable = false;

        var id = new[] { "Test01", "Test02" };
        DoExecute(id[0]);

        // Act
        DoExecute(id[1]);

        // Assert
        Assert.That(_model.GetExecutionHistory(), Is.EqualTo(string.Join(Model.Separator, id)));
        CheckModel(false, true, null);
    }


    [Test]
    public void DoExecute_Limited_Continuable()
    {
        // Arrange
        var count = 10;
        _model.RemainingExecutionCount = count;

        // Act
        DoExecute(_id);

        // Assert
        Assert.That(_model.GetExecutionHistory(), Is.EqualTo(_id));
        CheckModel(false, true, count - 1);
    }
    [Test]
    public void DoExecute_Limited_Noncontinuable()
    {
        // Arrange
        var count = 1;
        _model.RemainingExecutionCount = count;

        // Act
        DoExecute(_id);

        // Assert
        Assert.That(_model.GetExecutionHistory(), Is.EqualTo(_id));
        CheckModel(false, false, count - 1);
    }


    [Test]
    public void DoExecute_Noncontinuable()
    {
        // Arrange
        _model.RemainingExecutionCount = 0;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => DoExecute(_id));
    }
    #endregion



    #region To Noncontinuable
    [Test]
    public void ToNonsustainable_Idle()
    {
        // Act
        _model.Sustainable = false;

        // Assert
        CheckModel(false, true, null);
    }
    [Test]
    public void ToNonsustainable_Nonsustainable()
    {
        // Arrange
        _model.Sustainable = false;

        // Act
        _model.Sustainable = false;

        // Assert
        CheckModel(false, true, null);
    }
    [Test]
    public void ToNonsustainable_Limited()
    {
        // Arrange
        var count = 10;
        _model.RemainingExecutionCount = count;

        // Act
        _model.Sustainable = false;

        // Assert
        CheckModel(false, true, count);
    }
    [Test]
    public void ToNonsustainable_Noncontinuable()
    {
        // Arrange
        _model.Continuable = false;

        // Act
        _model.Sustainable = false;

        // Assert
        CheckModel(false, false, null);
    }
    #endregion


    
    #region To Noncontinuable
    [Test]
    public void ToNoncontinuable_Idle()
    {
        // Act
        _model.Continuable = false;

        // Assert
        CheckModel(false, false, null);
    }
    [Test]
    public void ToNoncontinuable_Nonsustainable()
    {
        // Arrange
        _model.Sustainable = false;

        // Act
        _model.Continuable = false;

        // Assert
        CheckModel(false, false, null);
    }
    [Test]
    public void ToNoncontinuable_Limited()
    {
        // Arrange
        var count = 10;
        _model.RemainingExecutionCount = count;

        // Act
        _model.Continuable = false;

        // Assert
        CheckModel(false, false, count);
    }
    [Test]
    public void ToNoncontinuable_Noncontinuable()
    {
        // Arrange
        _model.Continuable = false;

        // Act
        _model.Continuable = false;

        // Assert
        CheckModel(false, false, null);
    }


    [Test]
    public void ToNoncontinuable_Idle_False()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _model.Continuable = true);
    }
    [Test]
    public void ToNoncontinuable_Nonsustainable_False()
    {
        // Arrange
        _model.Sustainable = false;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _model.Continuable = true);
    }
    [Test]
    public void ToNoncontinuable_Limited_False()
    {
        // Arrange
        _model.RemainingExecutionCount = 10;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _model.Continuable = true);
    }
    [Test]
    public void ToNoncontinuable_Noncontinuable_False()
    {
        // Arrange
        _model.Continuable = false;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _model.Continuable = true);
    }
    #endregion



    #region Set Remaining Execution Count
    [Test]
    public void SetRemainingExecutionCount_Idle_Null()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _model.RemainingExecutionCount = null);
    }
    [Test]
    public void SetRemainingExecutionCount_Nonsustainable_Null()
    {
        // Arrange
        _model.Sustainable = false;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _model.RemainingExecutionCount = null);
    }
    [Test]
    public void SetRemainingExecutionCount_Limited_Null()
    {
        // Arrange
        _model.RemainingExecutionCount = 10;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _model.RemainingExecutionCount = null);
    }
    [Test]
    public void SetRemainingExecutionCount_Noncontinuable_Null()
    {
        // Arrange
        _model.Continuable = false;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _model.RemainingExecutionCount = null);
    }


    [TestCase(10), TestCase(0)]
    public void SetRemainingExecutionCount_Idle(int count)
    {
        // Act
        _model.RemainingExecutionCount = count;

        // Assert
        CheckModel(false, count > 0, count);
    }
    [TestCase(10), TestCase(0)]
    public void SetRemainingExecutionCount_Nonsustainable(int count)
    {
        // Arrange
        _model.Sustainable = false;

        // Act
        _model.RemainingExecutionCount = count;

        // Assert
        CheckModel(false, count > 0, count);
    }
    [Test]
    public void SetRemainingExecutionCount_Limited()
    {
        // Arrange
        var count = 10;
        _model.RemainingExecutionCount = count;

        // Act
        _model.RemainingExecutionCount = count + 1;

        // Assert
        CheckModel(false, true, count);
    }
    [Test]
    public void SetRemainingExecutionCount_Noncontinuable()
    {
        // Arrange
        var count = 10;
        _model.Continuable = false;

        // Act
        _model.RemainingExecutionCount = count;

        // Assert
        CheckModel(false, false, count);
    }


    [TestCase(5), TestCase(0)]
    public void SetRemainingExecutionCount_Limited_Smaller(int count)
    {
        // Arrange
        _model.RemainingExecutionCount = 10;

        // Act
        _model.RemainingExecutionCount = count;

        // Assert
        CheckModel(false, count > 0, count);
    }
    [TestCase(5), TestCase(0)]
    public void SetRemainingExecutionCount_Limited_Smaller_Noncontinuable(int count)
    {
        // Arrange
        _model.RemainingExecutionCount = 10;

        // Act
        _model.RemainingExecutionCount = count;

        // Assert
        CheckModel(false, count > 0, count);
    }
    #endregion
}
