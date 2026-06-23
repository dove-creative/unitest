using System.Linq;
using NUnit.Framework;
using UniTest;

public class LabExtensionTest
{
    int executionOrder;
    int lab_1_setMD, lab_1_arrange, lab_1_act, lab_1_assert;
    int lab_2_setMD, lab_2_arrange, lab_2_act, lab_2_assert;
    int lab_3_setMD, lab_3_arrange, lab_3_act, lab_3_assert;
    int lab_4_setMD, lab_4_arrange, lab_4_act, lab_4_assert;


    [SetUp]
    public void SetUp()
    {
        executionOrder = -1;

        lab_1_setMD = lab_1_arrange = lab_1_act = lab_1_assert = executionOrder;
        lab_2_setMD = lab_2_arrange = lab_2_act = lab_2_assert = executionOrder;
        lab_3_setMD = lab_3_arrange = lab_3_act = lab_3_assert = executionOrder;
        lab_4_setMD = lab_4_arrange = lab_4_act = lab_4_assert = executionOrder;
    }



    [Test]
    public void ExtendLab_FromLab()
    {
        // Arrnage
        var originalLab = new Lab<Model>
        {
            SetMetadata = _ => lab_1_setMD = ++executionOrder,
            Arranger = (_, _) => lab_1_arrange = ++executionOrder,
            Actor = (_, _) => lab_1_act = ++executionOrder,
            Asserter = (_, _) => lab_1_assert = ++executionOrder
        };
        var extendingLab = new Lab<Model>
        {
            SetMetadata = _ => lab_2_setMD = ++executionOrder,
            Arranger = (_, _) => lab_2_arrange = ++executionOrder,
            Actor = (_, _) => lab_2_act = ++executionOrder,
            Asserter = (_, _) => lab_2_assert = ++executionOrder
        };


        // Act
        var extendedLab = extendingLab.Extend(new[] { originalLab });
        extendedLab.First().Execute(new Model(), out var ex);

        // Assert
        Assert.IsNull(ex);

        Assert.AreEqual(0, lab_1_setMD);
        Assert.AreEqual(1, lab_1_arrange);
        Assert.AreEqual(2, lab_2_setMD);
        Assert.AreEqual(3, lab_2_arrange);

        Assert.AreEqual(4, lab_1_act);
        Assert.AreEqual(-1, lab_2_act);

        Assert.AreEqual(5, lab_2_assert);
        Assert.AreEqual(6, lab_1_assert);
    }
    [Test]
    public void ExtendLab_FromCompositeLab()
    {
        // Arrnage
        var sourceLab_uppder = new Lab<Model>
        {
            SetMetadata = _ => lab_1_setMD = ++executionOrder,
            Arranger = (_, _) => lab_1_arrange = ++executionOrder,
            Actor = (_, _) => lab_1_act = ++executionOrder,
            Asserter = (_, _) => lab_1_assert = ++executionOrder
        };
        var sourceLab_lower = new Lab<Model>
        {
            SetMetadata = _ => lab_2_setMD = ++executionOrder,
            Arranger = (_, _) => lab_2_arrange = ++executionOrder,
            Actor = (_, _) => lab_2_act = ++executionOrder,
            Asserter = (_, _) => lab_2_assert = ++executionOrder
        };
        var originalLab = new CompositeLab<Model>(sourceLab_uppder, sourceLab_lower);

        var extendingLab = new Lab<Model>
        {
            SetMetadata = _ => lab_3_setMD = ++executionOrder,
            Arranger = (_, _) => lab_3_arrange = ++executionOrder,
            Actor = (_, _) => lab_3_act = ++executionOrder,
            Asserter = (_, _) => lab_3_assert = ++executionOrder
        };


        // Act
        var extendedLab = extendingLab.Extend(new[] { originalLab });
        extendedLab.First().Execute(new Model(), out var ex);

        // Assert
        Assert.IsNull(ex);

        Assert.AreEqual(0, lab_1_setMD);
        Assert.AreEqual(1, lab_1_arrange);
        Assert.AreEqual(2, lab_2_setMD);
        Assert.AreEqual(3, lab_2_arrange);
        Assert.AreEqual(4, lab_3_setMD);
        Assert.AreEqual(5, lab_3_arrange);

        Assert.AreEqual(6, lab_1_act);
        Assert.AreEqual(-1, lab_2_act);
        Assert.AreEqual(-1, lab_3_act);

        Assert.AreEqual(7, lab_3_assert);
        Assert.AreEqual(8, lab_2_assert);
        Assert.AreEqual(9, lab_1_assert);
    }
    [Test]
    public void ExtendLab_Multiple()
    {
        // Arrnage
        var originalLab_normal = new Lab<Model>
        {
            SetMetadata = _ => lab_1_setMD = ++executionOrder,
            Arranger = (_, _) => lab_1_arrange = ++executionOrder,
            Actor = (_, _) => lab_1_act = ++executionOrder,
            Asserter = (_, _) => lab_1_assert = ++executionOrder
        };

        var sourceLab_uppder = new Lab<Model>
        {
            SetMetadata = _ => lab_2_setMD = ++executionOrder,
            Arranger = (_, _) => lab_2_arrange = ++executionOrder,
            Actor = (_, _) => lab_2_act = ++executionOrder,
            Asserter = (_, _) => lab_2_assert = ++executionOrder
        };
        var sourceLab_lower = new Lab<Model>
        {
            SetMetadata = _ => lab_3_setMD = ++executionOrder,
            Arranger = (_, _) => lab_3_arrange = ++executionOrder,
            Actor = (_, _) => lab_3_act = ++executionOrder,
            Asserter = (_, _) => lab_3_assert = ++executionOrder
        };
        var originalLab_composite = new CompositeLab<Model>(sourceLab_uppder, sourceLab_lower);

        var extendingLab = new Lab<Model>
        {
            SetMetadata = _ => lab_4_setMD = ++executionOrder,
            Arranger = (_, _) => lab_4_arrange = ++executionOrder,
            Actor = (_, _) => lab_4_act = ++executionOrder,
            Asserter = (_, _) => lab_4_assert = ++executionOrder
        };


        // Act 1
        var extendedLab = extendingLab.Extend(new ILab<Model>[] { originalLab_normal, originalLab_composite });
        extendedLab.First().Execute(new Model(), out var ex);

        // Assert 1
        Assert.IsNull(ex);

        Assert.AreEqual(0, lab_1_setMD);
        Assert.AreEqual(1, lab_1_arrange);
        Assert.AreEqual(2, lab_4_setMD);
        Assert.AreEqual(3, lab_4_arrange);

        Assert.AreEqual(4, lab_1_act);
        Assert.AreEqual(-1, lab_4_act);

        Assert.AreEqual(5, lab_4_assert);
        Assert.AreEqual(6, lab_1_assert);


        // Act 2
        SetUp();
        extendedLab.Last().Execute(new Model(), out ex);

        // Assert 2
        Assert.IsNull(ex);

        Assert.AreEqual(0, lab_2_setMD);
        Assert.AreEqual(1, lab_2_arrange);
        Assert.AreEqual(2, lab_3_setMD);
        Assert.AreEqual(3, lab_3_arrange);
        Assert.AreEqual(4, lab_4_setMD);
        Assert.AreEqual(5, lab_4_arrange);

        Assert.AreEqual(6, lab_2_act);
        Assert.AreEqual(-1, lab_3_act);
        Assert.AreEqual(-1, lab_4_act);

        Assert.AreEqual(7, lab_4_assert);
        Assert.AreEqual(8, lab_3_assert);
        Assert.AreEqual(9, lab_2_assert);
    }



    [Test]
    public void ExtendLab_AsActor_Single()
    {
        // Arrange
        var assertionLab = new Lab<Model>
        {
            Arranger = (_, _) => lab_1_arrange = ++executionOrder,
            Asserter = (_, _) => lab_1_assert = ++executionOrder
        };

        var actorLab = new Lab<Model>("Actor")
        {
            SetMetadata = _ => lab_2_setMD = ++executionOrder,
            Actor = (_, _) => lab_2_act = ++executionOrder
        };


        // Act
        var extended = assertionLab.Extend(new[] { actorLab });
        extended.First().Execute(new Model(), out var ex);


        // Assert
        Assert.IsNull(ex);

        Assert.AreEqual(-1, lab_1_setMD);
        Assert.AreEqual(0, lab_2_setMD);

        Assert.AreEqual(1, lab_1_arrange);

        Assert.AreEqual(-1, lab_1_act);
        Assert.AreEqual(2, lab_2_act);

        Assert.AreEqual(3, lab_1_assert);
    }
    [Test]
    public void ExtendLab_AsActor_Multiple()
    {
        // Arrange
        var assertionLab = new Lab<Model>
        {
            Arranger = (_, _) => lab_1_arrange = ++executionOrder,
            Asserter = (_, _) => lab_1_assert = ++executionOrder
        };

        var actorLab_1 = new Lab<Model>("Actor_1")
        {
            SetMetadata = _ => lab_2_setMD = ++executionOrder,
            Actor = (_, _) => lab_2_act = ++executionOrder
        };
        var actorLab_2 = new Lab<Model>("Actor_2")
        {
            SetMetadata = _ => lab_3_setMD = ++executionOrder,
            Actor = (_, _) => lab_3_act = ++executionOrder
        };


        // Act 1
        var extended = assertionLab.Extend(new[] { actorLab_1, actorLab_2 });
        extended.First().Execute(new Model(), out var ex);

        // Assert 1
        Assert.IsNull(ex);

        Assert.AreEqual(-1, lab_1_setMD);
        Assert.AreEqual(0, lab_2_setMD);

        Assert.AreEqual(1, lab_1_arrange);

        Assert.AreEqual(-1, lab_1_act);
        Assert.AreEqual(2, lab_2_act);

        Assert.AreEqual(3, lab_1_assert);


        // Act 2
        SetUp();
        extended.Last().Execute(new Model(), out ex);

        // Assert 1
        Assert.IsNull(ex);

        Assert.AreEqual(-1, lab_1_setMD);
        Assert.AreEqual(0, lab_3_setMD);

        Assert.AreEqual(1, lab_1_arrange);

        Assert.AreEqual(-1, lab_1_act);
        Assert.AreEqual(2, lab_3_act);

        Assert.AreEqual(3, lab_1_assert);
    }
}
