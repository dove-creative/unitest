using System.Linq;
using NUnit.Framework;
using UniTest;

public class LabExtensionTest
{
    int _executionOrder;
    int _lab1SetMd, _lab1Arrange, _lab1Act, _lab1Assert;
    int _lab2SetMd, _lab2Arrange, _lab2Act, _lab2Assert;
    int _lab3SetMd, _lab3Arrange, _lab3Act, _lab3Assert;
    int _lab4SetMd, _lab4Arrange, _lab4Act, _lab4Assert;


    [SetUp]
    public void SetUp()
    {
        _executionOrder = -1;

        _lab1SetMd = _lab1Arrange = _lab1Act = _lab1Assert = _executionOrder;
        _lab2SetMd = _lab2Arrange = _lab2Act = _lab2Assert = _executionOrder;
        _lab3SetMd = _lab3Arrange = _lab3Act = _lab3Assert = _executionOrder;
        _lab4SetMd = _lab4Arrange = _lab4Act = _lab4Assert = _executionOrder;
    }



    [Test]
    public void ExtendLab_FromLab()
    {
        // Arrnage
        var originalLab = new Lab<Model>
        {
            SetMetadata = _ => _lab1SetMd = ++_executionOrder,
            Arranger = (_, _) => _lab1Arrange = ++_executionOrder,
            Actor = (_, _) => _lab1Act = ++_executionOrder,
            Asserter = (_, _) => _lab1Assert = ++_executionOrder
        };
        var extendingLab = new Lab<Model>
        {
            SetMetadata = _ => _lab2SetMd = ++_executionOrder,
            Arranger = (_, _) => _lab2Arrange = ++_executionOrder,
            Actor = (_, _) => _lab2Act = ++_executionOrder,
            Asserter = (_, _) => _lab2Assert = ++_executionOrder
        };


        // Act
        var extendedLab = extendingLab.Extend(new[] { originalLab });
        extendedLab.First().Execute(new Model(), out var ex);

        // Assert
        Assert.That(ex, Is.Null);

        Assert.That(_lab1SetMd, Is.EqualTo(0));
        Assert.That(_lab1Arrange, Is.EqualTo(1));
        Assert.That(_lab2SetMd, Is.EqualTo(2));
        Assert.That(_lab2Arrange, Is.EqualTo(3));

        Assert.That(_lab1Act, Is.EqualTo(4));
        Assert.That(_lab2Act, Is.EqualTo(-1));

        Assert.That(_lab2Assert, Is.EqualTo(5));
        Assert.That(_lab1Assert, Is.EqualTo(6));
    }
    [Test]
    public void ExtendLab_FromCompositeLab()
    {
        // Arrnage
        var sourceLab_uppder = new Lab<Model>
        {
            SetMetadata = _ => _lab1SetMd = ++_executionOrder,
            Arranger = (_, _) => _lab1Arrange = ++_executionOrder,
            Actor = (_, _) => _lab1Act = ++_executionOrder,
            Asserter = (_, _) => _lab1Assert = ++_executionOrder
        };
        var sourceLab_lower = new Lab<Model>
        {
            SetMetadata = _ => _lab2SetMd = ++_executionOrder,
            Arranger = (_, _) => _lab2Arrange = ++_executionOrder,
            Actor = (_, _) => _lab2Act = ++_executionOrder,
            Asserter = (_, _) => _lab2Assert = ++_executionOrder
        };
        var originalLab = new CompositeLab<Model>(sourceLab_uppder, sourceLab_lower);

        var extendingLab = new Lab<Model>
        {
            SetMetadata = _ => _lab3SetMd = ++_executionOrder,
            Arranger = (_, _) => _lab3Arrange = ++_executionOrder,
            Actor = (_, _) => _lab3Act = ++_executionOrder,
            Asserter = (_, _) => _lab3Assert = ++_executionOrder
        };


        // Act
        var extendedLab = extendingLab.Extend(new[] { originalLab });
        extendedLab.First().Execute(new Model(), out var ex);

        // Assert
        Assert.That(ex, Is.Null);

        Assert.That(_lab1SetMd, Is.EqualTo(0));
        Assert.That(_lab1Arrange, Is.EqualTo(1));
        Assert.That(_lab2SetMd, Is.EqualTo(2));
        Assert.That(_lab2Arrange, Is.EqualTo(3));
        Assert.That(_lab3SetMd, Is.EqualTo(4));
        Assert.That(_lab3Arrange, Is.EqualTo(5));

        Assert.That(_lab1Act, Is.EqualTo(6));
        Assert.That(_lab2Act, Is.EqualTo(-1));
        Assert.That(_lab3Act, Is.EqualTo(-1));

        Assert.That(_lab3Assert, Is.EqualTo(7));
        Assert.That(_lab2Assert, Is.EqualTo(8));
        Assert.That(_lab1Assert, Is.EqualTo(9));
    }
    [Test]
    public void ExtendLab_Multiple()
    {
        // Arrnage
        var originalLab_normal = new Lab<Model>
        {
            SetMetadata = _ => _lab1SetMd = ++_executionOrder,
            Arranger = (_, _) => _lab1Arrange = ++_executionOrder,
            Actor = (_, _) => _lab1Act = ++_executionOrder,
            Asserter = (_, _) => _lab1Assert = ++_executionOrder
        };

        var sourceLab_uppder = new Lab<Model>
        {
            SetMetadata = _ => _lab2SetMd = ++_executionOrder,
            Arranger = (_, _) => _lab2Arrange = ++_executionOrder,
            Actor = (_, _) => _lab2Act = ++_executionOrder,
            Asserter = (_, _) => _lab2Assert = ++_executionOrder
        };
        var sourceLab_lower = new Lab<Model>
        {
            SetMetadata = _ => _lab3SetMd = ++_executionOrder,
            Arranger = (_, _) => _lab3Arrange = ++_executionOrder,
            Actor = (_, _) => _lab3Act = ++_executionOrder,
            Asserter = (_, _) => _lab3Assert = ++_executionOrder
        };
        var originalLab_composite = new CompositeLab<Model>(sourceLab_uppder, sourceLab_lower);

        var extendingLab = new Lab<Model>
        {
            SetMetadata = _ => _lab4SetMd = ++_executionOrder,
            Arranger = (_, _) => _lab4Arrange = ++_executionOrder,
            Actor = (_, _) => _lab4Act = ++_executionOrder,
            Asserter = (_, _) => _lab4Assert = ++_executionOrder
        };


        // Act 1
        var extendedLab = extendingLab.Extend(new ILab<Model>[] { originalLab_normal, originalLab_composite });
        extendedLab.First().Execute(new Model(), out var ex);

        // Assert 1
        Assert.That(ex, Is.Null);

        Assert.That(_lab1SetMd, Is.EqualTo(0));
        Assert.That(_lab1Arrange, Is.EqualTo(1));
        Assert.That(_lab4SetMd, Is.EqualTo(2));
        Assert.That(_lab4Arrange, Is.EqualTo(3));

        Assert.That(_lab1Act, Is.EqualTo(4));
        Assert.That(_lab4Act, Is.EqualTo(-1));

        Assert.That(_lab4Assert, Is.EqualTo(5));
        Assert.That(_lab1Assert, Is.EqualTo(6));


        // Act 2
        SetUp();
        extendedLab.Last().Execute(new Model(), out ex);

        // Assert 2
        Assert.That(ex, Is.Null);

        Assert.That(_lab2SetMd, Is.EqualTo(0));
        Assert.That(_lab2Arrange, Is.EqualTo(1));
        Assert.That(_lab3SetMd, Is.EqualTo(2));
        Assert.That(_lab3Arrange, Is.EqualTo(3));
        Assert.That(_lab4SetMd, Is.EqualTo(4));
        Assert.That(_lab4Arrange, Is.EqualTo(5));

        Assert.That(_lab2Act, Is.EqualTo(6));
        Assert.That(_lab3Act, Is.EqualTo(-1));
        Assert.That(_lab4Act, Is.EqualTo(-1));

        Assert.That(_lab4Assert, Is.EqualTo(7));
        Assert.That(_lab3Assert, Is.EqualTo(8));
        Assert.That(_lab2Assert, Is.EqualTo(9));
    }



    [Test]
    public void ExtendLab_AsActor_Single()
    {
        // Arrange
        var assertionLab = new Lab<Model>
        {
            Arranger = (_, _) => _lab1Arrange = ++_executionOrder,
            Asserter = (_, _) => _lab1Assert = ++_executionOrder
        };

        var actorLab = new Lab<Model>("Actor")
        {
            SetMetadata = _ => _lab2SetMd = ++_executionOrder,
            Actor = (_, _) => _lab2Act = ++_executionOrder
        };


        // Act
        var extended = assertionLab.Extend(new[] { actorLab });
        extended.First().Execute(new Model(), out var ex);


        // Assert
        Assert.That(ex, Is.Null);

        Assert.That(_lab1SetMd, Is.EqualTo(-1));
        Assert.That(_lab2SetMd, Is.EqualTo(0));

        Assert.That(_lab1Arrange, Is.EqualTo(1));

        Assert.That(_lab1Act, Is.EqualTo(-1));
        Assert.That(_lab2Act, Is.EqualTo(2));

        Assert.That(_lab1Assert, Is.EqualTo(3));
    }
    [Test]
    public void ExtendLab_AsActor_Multiple()
    {
        // Arrange
        var assertionLab = new Lab<Model>
        {
            Arranger = (_, _) => _lab1Arrange = ++_executionOrder,
            Asserter = (_, _) => _lab1Assert = ++_executionOrder
        };

        var actorLab_1 = new Lab<Model>("Actor_1")
        {
            SetMetadata = _ => _lab2SetMd = ++_executionOrder,
            Actor = (_, _) => _lab2Act = ++_executionOrder
        };
        var actorLab_2 = new Lab<Model>("Actor_2")
        {
            SetMetadata = _ => _lab3SetMd = ++_executionOrder,
            Actor = (_, _) => _lab3Act = ++_executionOrder
        };


        // Act 1
        var extended = assertionLab.Extend(new[] { actorLab_1, actorLab_2 });
        extended.First().Execute(new Model(), out var ex);

        // Assert 1
        Assert.That(ex, Is.Null);

        Assert.That(_lab1SetMd, Is.EqualTo(-1));
        Assert.That(_lab2SetMd, Is.EqualTo(0));

        Assert.That(_lab1Arrange, Is.EqualTo(1));

        Assert.That(_lab1Act, Is.EqualTo(-1));
        Assert.That(_lab2Act, Is.EqualTo(2));

        Assert.That(_lab1Assert, Is.EqualTo(3));


        // Act 2
        SetUp();
        extended.Last().Execute(new Model(), out ex);

        // Assert 1
        Assert.That(ex, Is.Null);

        Assert.That(_lab1SetMd, Is.EqualTo(-1));
        Assert.That(_lab3SetMd, Is.EqualTo(0));

        Assert.That(_lab1Arrange, Is.EqualTo(1));

        Assert.That(_lab1Act, Is.EqualTo(-1));
        Assert.That(_lab3Act, Is.EqualTo(2));

        Assert.That(_lab1Assert, Is.EqualTo(3));
    }
}
