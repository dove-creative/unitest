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

    Node<Model>[] _nodes;


    [SetUp]
    public void SetUp()
    {
        _nodes = new Node<Model>[7];

        var lab = new Lab<Model>();
        var ct = new CancellationToken();


        _nodes[0] = new Node<Model>(null);

        _nodes[1] = _nodes[0].Append(lab, ct);
        _nodes[1].Execute();
        _nodes[2] = _nodes[1].Append(lab, ct);

        _nodes[3] = _nodes[0].Append(lab, ct);
        _nodes[3].Execute();
        _nodes[4] = _nodes[3].Append(lab, ct);
        _nodes[5] = _nodes[3].Append(lab, ct);

        _nodes[6] = _nodes[0].Append(lab, ct);
    }

    [TearDown]
    public void Teardown()
    {
        _nodes = null;
    }



    [Test]
    public void Count_Root()
    {
        // Act
        var count = _nodes[0].GetCount();

        // Assert
        Assert.That(count, Is.EqualTo(7));
    }
    [Test]
    public void Count_NotRoot()
    {
        // Act
        var count = _nodes[3].GetCount();

        // Assert
        Assert.That(count, Is.EqualTo(3));
    }
    [Test]
    public void Count_Every()
    {
        // Act
        var counts = _nodes.Select(n => n.GetCount());

        // Assert
        Assert.That(counts, Is.EqualTo(new[] { 7, 2, 1, 3, 1, 1, 1 }));
    }



    [Test]
    public void AllSucceed_Succeed_NotCancelled()
    {
        // Act
        var succeed = _nodes[0].AllSucceed(out var cancelled);

        // Assert
        Assert.That(cancelled, Is.False);
        Assert.That(succeed, Is.True);
    }
    [Test]
    public void AllSucceed_Succeded_Cancelled()
    {
        // Arrange
        _nodes[3].SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Cancelled);

        // Act
        var succeed = _nodes[0].AllSucceed(out var cancelled);

        // Assert
        Assert.That(cancelled, Is.True);
        Assert.That(succeed, Is.True);
    }
    [Test]
    public void AllSucceed_Failed()
    {
        // Arrange
        _nodes[3].SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Failure);

        // Act
        var succeed = _nodes[0].AllSucceed(out _);

        // Assert
        Assert.That(succeed, Is.False);
    }



    [Test]
    public void GetFailedReports_None()
    {
        // Act
        var failedReport = _nodes[0].GetFailedReports();
        var failedReports = failedReport.ChildNodes
            .OfType<System.Xml.XmlElement>();

        // Assert
        Assert.That(failedReports, Is.Empty);
    }
    [Test]
    public void GetFailedReports_Single()
    {
        // Arrange
        _nodes[3].SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Failure);

        // Act
        var failedReport = _nodes[0].GetFailedReports();
        var failedReports = failedReport.ChildNodes
            .OfType<System.Xml.XmlElement>()
            .Select(n => n.OuterXml);

        // Assert
        Assert.That(failedReports, Is.EquivalentTo(new[] { _nodes[3].Report.OuterXml }));
    }
    [Test]
    public void GetFailedReports_Multiple()
    {
        // Arrange
        _nodes[3].SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Failure);
        _nodes[5].SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Failure);
        _nodes[6].SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Failure);

        // Act
        var failedReport = _nodes[0].GetFailedReports();
        var failedReports = failedReport.ChildNodes
            .OfType<System.Xml.XmlElement>()
            .Select(n => n.OuterXml);

        // Assert
        Assert.That(failedReports, Is.EquivalentTo(new[] { _nodes[3], _nodes[6] }.Select(n => n.Report.OuterXml)));
    }
}
