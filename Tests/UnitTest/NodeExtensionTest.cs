using System.Linq;
using System.Threading;
using NUnit.Framework;
using UniTest;

public class NodeExtensionTest
{
    /* [0] ┬ [1] - [2]
     *     ├ [3] ┬ [4]
     *     │     └ [5]
     *     └ [6]
     */

    Node<Model>[] nodes;


    [SetUp]
    public void SetUp()
    {
        nodes = new Node<Model>[7];

        var lab = new Lab<Model>();
        var ct = new CancellationToken();


        nodes[0] = new Node<Model>(null);

        nodes[1] = nodes[0].Append(lab, ct);
        nodes[1].Execute();
        nodes[2] = nodes[1].Append(lab, ct);

        nodes[3] = nodes[0].Append(lab, ct);
        nodes[3].Execute();
        nodes[4] = nodes[3].Append(lab, ct);
        nodes[5] = nodes[3].Append(lab, ct);

        nodes[6] = nodes[0].Append(lab, ct);
    }

    [TearDown]
    public void Teardown()
    {
        nodes = null;
    }



    [Test]
    public void Count_Root()
    {
        // Act
        var count = nodes[0].GetCount();

        // Assert
        Assert.AreEqual(7, count);
    }
    [Test]
    public void Count_NotRoot()
    {
        // Act
        var count = nodes[3].GetCount();

        // Assert
        Assert.AreEqual(3, count);
    }
    [Test]
    public void Count_Every()
    {
        // Act
        var counts = nodes.Select(n => n.GetCount());

        // Assert
        CollectionAssert.AreEqual(new[] { 7, 2, 1, 3, 1, 1, 1 }, counts);
    }



    [Test]
    public void AllSucceed_Succeed_NotCancelled()
    {
        // Act
        var succeed = nodes[0].AllSucceed(out var cancelled);

        // Assert
        Assert.IsFalse(cancelled);
        Assert.IsTrue(succeed);
    }
    [Test]
    public void AllSucceed_Succeded_Cancelled()
    {
        // Arrange
        nodes[3].SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Cancelled);

        // Act
        var succeed = nodes[0].AllSucceed(out var cancelled);

        // Assert
        Assert.IsTrue(cancelled);
        Assert.IsTrue(succeed);
    }
    [Test]
    public void AllSucceed_Failed()
    {
        // Arrange
        nodes[3].SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Failure);

        // Act
        var succeed = nodes[0].AllSucceed(out _);

        // Assert
        Assert.IsFalse(succeed);
    }



    [Test]
    public void GetFailedReports_None()
    {
        // Act
        var failedReport = nodes[0].GetFailedReports();
        var failedReports = failedReport.ChildNodes
            .OfType<System.Xml.XmlElement>();

        // Assert
        CollectionAssert.IsEmpty(failedReports);
    }
    [Test]
    public void GetFailedReports_Single()
    {
        // Arrange
        nodes[3].SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Failure);

        // Act
        var failedReport = nodes[0].GetFailedReports();
        var failedReports = failedReport.ChildNodes
            .OfType<System.Xml.XmlElement>()
            .Select(n => n.OuterXml);

        // Assert
        CollectionAssert.AreEquivalent(new[] { nodes[3].Report.OuterXml }, failedReports);
    }
    [Test]
    public void GetFailedReports_Multiple()
    {
        // Arrange
        nodes[3].SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Failure);
        nodes[5].SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Failure);
        nodes[6].SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Failure);

        // Act
        var failedReport = nodes[0].GetFailedReports();
        var failedReports = failedReport.ChildNodes
            .OfType<System.Xml.XmlElement>()
            .Select(n => n.OuterXml);

        // Assert
        CollectionAssert.AreEquivalent(
            new[] { nodes[3], nodes[6] }.Select(n => n.Report.OuterXml), failedReports);
    }
}
