using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using UniTest;

public class NodeTest
{
    Node<Model> _node;
    CancellationTokenSource _cts;
    const int Delay = 100;

    Lab<Model> _lab1;
    Lab<Model> _lab2;
    Lab<Model> _lab3;

    bool _lab1Success;
    bool _lab2Success;
    bool _lab3Success;

    bool _lab1Executed;
    bool _lab2Executed;
    bool _lab3Executed;


    [SetUp]
    public void SetUp()
    {
        _lab1 = new Lab<Model>("lab_1")
        {
            Arranger = (_, _) => _lab1Executed = true,
            Actor = (_, _) => { if (!_lab1Success) throw new ProbeException(); }
        };
        _lab2 = new Lab<Model>("lab_1")
        {
            Arranger = (_, _) => _lab2Executed = true,
            Actor = (_, _) => { if (!_lab2Success) throw new ProbeException(); }
        };
        _lab3 = new Lab<Model>("lab_3")
        {
            Arranger = (_, _) => _lab3Executed = true,
            Actor = (_, _) => { if (!_lab3Success) throw new ProbeException(); }
        };

        _lab1Success = true;
        _lab2Success = true;
        _lab3Success = true;

        _lab1Executed = false;
        _lab2Executed = false;
        _lab3Executed = false;

        _cts = new();
    }

    [TearDown]
    public void TearDown()
    {
        _node = null;

        _lab1 = null;
        _lab2 = null;
        _lab3 = null;

        _cts.Cancel();
        _cts.Dispose();
    }

    public void CheckNode(Node<Model> node,
        Node<Model>.NodeStatus status, int depth, Node<Model> before, IEnumerable<Node<Model>> afters, Lab<Model> lab, Type exception, bool continuable)
    {
        Assert.That(node.Status, Is.EqualTo(status));
        Assert.That(node.Depth, Is.EqualTo(depth));
        Assert.That(node.Before, Is.SameAs(before));
        Assert.That(node.Afters, Is.EquivalentTo(afters ?? Array.Empty<Node<Model>>()));

        Assert.That(node.Lab, Is.SameAs(lab));
        Assert.That(node.ID, Is.SameAs(lab?.ID ?? "root"));
        Assert.That(node.Model, Is.Not.Null);

        if (exception != null)
            Assert.That(node.Exception, Is.AssignableTo(exception));
        else
            Assert.That(node.Exception, Is.Null);

        Assert.That(node.Continuable, Is.EqualTo(continuable));
    }



    #region State Transition
    [Test]
    public void Create_NullLab()
    {
        // Act
        _node = new Node<Model>(null);

        // Assert
        CheckNode(_node, Node<Model>.NodeStatus.Root, 0, null, null, null, null, true);
    }
    [Test]
    public void Create_Lab()
    {
        // Act
        _node = new Node<Model>(_lab1);

        // Assert
        CheckNode(_node, Node<Model>.NodeStatus.Ready, 0, null, null, _lab1, null, false);
    }


    [Test]
    public void Executed_Continuable()
    {
        // Arrange
        _node = new Node<Model>(_lab1);

        // Act
        _node.Execute();

        // Assert
        CheckNode(_node, Node<Model>.NodeStatus.Success, 0, null, null, _lab1, null, true);
        Assert.True(_lab1Executed);
    }
    [Test]
    public void Executed_NotContinuable()
    {
        // Arrange
        _node = new Node<Model>(_lab1);
        _lab1Success = false;

        // Act
        _node.Execute();

        // Assert
        CheckNode(_node, Node<Model>.NodeStatus.Failure, 0, null, null, _lab1, typeof(ExecutionException), false);
        Assert.True(_lab1Executed);
    }
    #endregion



    #region Execute
    [Test]
    public void Execute_NullLab()
    {
        // Arrange
        _node = new Node<Model>(null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _node.Execute());
    }
    [Test]
    public void Execute_Idle_Success()
    {
        // Arrange
        _node = new Node<Model>(_lab1);

        // Act
        _node.Execute();

        // Assert
        CheckNode(_node, Node<Model>.NodeStatus.Success, 0, null, null, _lab1, null, true);
        Assert.True(_lab1Executed);
    }
    [Test]
    public void Execute_Idle_Failure()
    {
        // Arrange
        _node = new Node<Model>(_lab1);
        _lab1Success = false;

        // Act
        _node.Execute();

        // Assert
        CheckNode(_node, Node<Model>.NodeStatus.Failure, 0, null, null, _lab1, typeof(ExecutionException), false);
        Assert.True(_lab1Executed);
    }
    [Test]
    public void Execute_Executed_Continuable()
    {
        // Arrange
        _node = new Node<Model>(_lab1);
        _node.Execute();

        // Act & Assert
        Assert.True(_lab1Executed);
        Assert.Throws<InvalidOperationException>(() => _node.Execute());
    }
    [Test]
    public void Execute_Executed_NotContinuable()
    {
        // Arrange
        _node = new Node<Model>(_lab1);
        _lab1Success = false;

        _node.Execute();

        // Act & Assert
        Assert.True(_lab1Executed);
        Assert.Throws<InvalidOperationException>(() => _node.Execute());
    }
    #endregion



    #region Set External Exception
    [Test]
    public void SetExternalException_Valid_NullLab()
    {
        // Arrange
        _node = new Node<Model>(null);

        // Act 
        _node.SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Cancelled);

        // Assert
        CheckNode(_node, Node<Model>.NodeStatus.Cancelled, 0, null, null, null, typeof(ProbeException), false);
    }
    [Test]
    public void SetExternalException_Valid_Idle()
    {
        // Arrange
        _node = new Node<Model>(_lab1);

        // Act 
        _node.SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Cancelled);

        // Assert
        CheckNode(_node, Node<Model>.NodeStatus.Cancelled, 0, null, null, _lab1, typeof(ProbeException), false);
    }
    [Test]
    public void SetExternalException_Valid_Executed_Continuable()
    {
        // Arrange
        _node = new Node<Model>(_lab1);
        _node.Execute();

        // Act 
        _node.SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Cancelled);

        // Assert
        Assert.True(_lab1Executed);
        CheckNode(_node, Node<Model>.NodeStatus.Cancelled, 0, null, null, _lab1, typeof(ProbeException), false);
    }
    [Test]
    public void SetExternalException_Valid_Executed_NotContinuable()
    {
        // Arrange
        _node = new Node<Model>(_lab1);
        _lab1Success = false;

        _node.Execute();


        // Act 
        Assert.True(_lab1Executed);
        _node.SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Cancelled);

        // Assert
        CheckNode(_node, Node<Model>.NodeStatus.Cancelled, 0, null, null, _lab1, typeof(ProbeException), false);
    }


    [Test]
    public void SetExternalException_Null_NullLab()
    {
        // Arrange
        _node = new Node<Model>(null);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _node.SetExternalException(null, Node<Model>.NodeStatus.Cancelled));
    }
    [Test]
    public void SetExternalException_Null_Idle()
    {
        // Arrange
        _node = new Node<Model>(_lab1);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _node.SetExternalException(null, Node<Model>.NodeStatus.Cancelled));
    }
    [Test]
    public void SetExternalException_Null_Executed_Continuable()
    {
        // Arrange
        _node = new Node<Model>(_lab1);
        _node.Execute();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _node.SetExternalException(null, Node<Model>.NodeStatus.Cancelled));
    }
    [Test]
    public void SetExternalException_Null_Executed_NotContinuable()
    {
        // Arrange
        _node = new Node<Model>(_lab1);
        _lab1Success = false;

        _node.Execute();


        // Act & Assert
        Assert.True(_lab1Executed);
        Assert.Throws<ArgumentNullException>(() => _node.SetExternalException(null, Node<Model>.NodeStatus.Cancelled));
    }
    #endregion



    #region Append
    [Test]
    public void Append_NullLab_NullLab()
    {
        // Arrange
        _node = new Node<Model>(null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new Node<Model>(_node, null, _cts.Token));
    }
    [TestCase(false), TestCase(true)]
    public void Append_NullLab_Idle(bool postExecute)
    {
        // Arrange
        _node = new Node<Model>(null);

        // Act
        var after = new Node<Model>(_node, _lab2, _cts.Token);
        if (postExecute) after.Execute();

        // Assert
        CheckNode(_node, Node<Model>.NodeStatus.Root, 0, null, new[] { after }, null, null, true);

        if (!postExecute)
        {
            Assert.False(_lab2Executed);
            CheckNode(after, Node<Model>.NodeStatus.Ready, 1, _node, null, _lab2, null, false);
        }
        else
        {
            Assert.True(_lab2Executed);
            CheckNode(after, Node<Model>.NodeStatus.Success, 1, _node, null, _lab2, null, true);
        }
    }
    [Test]
    public async Task Append_NullLab_Idle_Cancelled()
    {
        // Arrange
        _node = new Node<Model>(null);
        _cts.Cancel();

        Node<Model> after = null;
        Exception ex = null;

        // Act
        try
        {
            await Task.Run(() => after = new Node<Model>(_node, _lab2, _cts.Token));
        }
        catch (Exception _ex)
        {
            ex = _ex;
        }

        // Assert
        CheckNode(_node, Node<Model>.NodeStatus.Root, 0, null, null, null, null, true);
        Assert.That(ex, Is.AssignableTo<OperationCanceledException>());
        Assert.That(after, Is.Null);
    }

    
    [Test]
    public void Append_Idle_NullLab()
    {
        // Arrange
        _node = new Node<Model>(_lab1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new Node<Model>(_node, null, _cts.Token));
    }
    [Test]
    public void Append_Idle_Idle()
    {
        // Arrange
        _node = new Node<Model>(_lab1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new Node<Model>(_node, _lab2, _cts.Token));
    }


    [Test]
    public void Append_Executed_Continuable_NullLab()
    {
        // Arrange
        _node = new Node<Model>(_lab1);
        _node.Execute();

        // Act & Assert
        Assert.True(_lab1Executed);
        Assert.Throws<InvalidOperationException>(() => new Node<Model>(_node, null, _cts.Token));
    }
    [TestCase(false), TestCase(true)]
    public void Append_Executed_Continuable_Idle(bool postExecute)
    {
        // Arrange
        _node = new Node<Model>(_lab1);
        _node.Execute();

        // Act
        var after = new Node<Model>(_node, _lab2, _cts.Token);
        if (postExecute) after.Execute();

        // Assert
        Assert.True(_lab1Executed);
        CheckNode(_node, Node<Model>.NodeStatus.Success, 0, null, new[] { after }, _lab1, null, true);

        if (!postExecute)
        {
            Assert.False(_lab2Executed);
            CheckNode(after, Node<Model>.NodeStatus.Ready, 1, _node, null, _lab2, null, false);
        }
        else
        {
            Assert.True(_lab2Executed);
            CheckNode(after, Node<Model>.NodeStatus.Success, 1, _node, null, _lab2, null, true);
        }
    }
    [Test]
    public async Task Append_Executed_Continuable_Idle_Cancelled()
    {
        // Arrange
        var lab_1_executionCount = 0;

        var lab_1 = new Lab<Model>(actor: (_, _) =>
        {
            lab_1_executionCount++;
            Thread.Sleep(Delay);
        });

        Node<Model> after = null;
        Exception ex = null;

        _node = new Node<Model>(lab_1);
        _node.Execute();
        _cts.Cancel();

        // Act
        try
        {
            await Task.Run(() => after = new Node<Model>(_node, _lab2, _cts.Token));
        }
        catch (Exception _ex)
        {
            ex = _ex;
        }

        // Assert
        Assert.That(lab_1_executionCount, Is.EqualTo(1));

        CheckNode(_node, Node<Model>.NodeStatus.Success, 0, null, null, lab_1, null, true);

        Assert.That(ex, Is.AssignableTo<OperationCanceledException>());
        Assert.That(after, Is.Null);
    }
    [Test]
    public async Task Append_Executed_Continuable_Idle_Cancelling()
    {
        // Arrange
        var lab_1_executionCount = 0;

        var lab_1 = new Lab<Model>(actor: (_, _) =>
        {
            lab_1_executionCount++;
            Thread.Sleep(Delay);
        });

        Node<Model> after = null;
        Exception ex = null;

        _node = new Node<Model>(lab_1);
        _node.Execute();

        // Act
        var task = Task.Run(() => after = new Node<Model>(_node, _lab2, _cts.Token));
        await Task.Run(async () =>
        {
            await Task.Delay(Delay / 2);
            _cts.Cancel();
        });

        try
        {
            await task;
        }
        catch (Exception _ex)
        {
            ex = _ex;
        }

        // Assert
        Assert.That(lab_1_executionCount, Is.EqualTo(2));

        CheckNode(_node, Node<Model>.NodeStatus.Success, 0, null, null, lab_1, null, true);

        Assert.That(ex, Is.AssignableTo<OperationCanceledException>());
        Assert.That(after, Is.Null);
    }


    [Test]
    public void Append_Executed_NotContinuable_NullLab()
    {
        // Arrange
        _node = new Node<Model>(_lab1);
        _lab1Success = false;
        _node.Execute();

        // Act & Assert
        Assert.True(_lab1Executed);
        Assert.Throws<InvalidOperationException>(() => new Node<Model>(_node, null, _cts.Token));
    }
    [Test]
    public void Append_Executed_NotContinuable_Idle()
    {
        // Arrange
        _node = new Node<Model>(_lab1);
        _lab1Success = false;
        _node.Execute();

        // Act & Assert
        Assert.True(_lab1Executed);
        Assert.Throws<InvalidOperationException>(() => new Node<Model>(_node, _lab2, _cts.Token));
    }


    [TestCase(false), TestCase(true)]
    public void Append_Multi_Sequential(bool beforeIsRoot)
    {
        // Arrange
        _node = new Node<Model>(beforeIsRoot ? null : _lab1);
        if (!beforeIsRoot) _node.Execute();

        var node_2 = new Node<Model>(_node, _lab2, _cts.Token);
        var node_3 = new Node<Model>(_node, _lab3, _cts.Token);


        // Act
        Assert.DoesNotThrow(() => node_2.Execute());
        Assert.DoesNotThrow(() => node_3.Execute());

        // Assert
        if (beforeIsRoot)
        {
            Assert.That(_lab1Executed, Is.False);
            CheckNode(_node, Node<Model>.NodeStatus.Root, 0, null, new[] { node_2, node_3 }, null, null, true);
        }
        else
        {
            Assert.That(_lab1Executed, Is.True);
            CheckNode(_node, Node<Model>.NodeStatus.Success, 0, null, new[] { node_2, node_3 }, _lab1, null, true);
        }

        Assert.That(_lab2Executed, Is.True);
        Assert.That(_lab3Executed, Is.True);
        CheckNode(node_2, Node<Model>.NodeStatus.Success, 1, _node, null, _lab2, null, true);
        CheckNode(node_3, Node<Model>.NodeStatus.Success, 1, _node, null, _lab3, null, true);
    }
    [TestCase(false), TestCase(true)]
    public void Append_Multi_NotSequential(bool beforeIsRoot)
    {
        // Arrange
        _node = new Node<Model>(beforeIsRoot ? null : _lab1);
        if (!beforeIsRoot) _node.Execute();


        // Act
        var node_2 = new Node<Model>(_node, _lab2, _cts.Token);
        Assert.DoesNotThrow(() => node_2.Execute());

        var node_3 = new Node<Model>(_node, _lab3, _cts.Token);
        Assert.DoesNotThrow(() => node_3.Execute());


        // Assert
        if (beforeIsRoot)
        {
            Assert.That(_lab1Executed, Is.False);
            CheckNode(_node, Node<Model>.NodeStatus.Root, 0, null, new[] { node_2, node_3 }, null, null, true);
        }
        else
        {
            Assert.That(_lab1Executed, Is.True);
            CheckNode(_node, Node<Model>.NodeStatus.Success, 0, null, new[] { node_2, node_3 }, _lab1, null, true);
        }

        Assert.That(_lab2Executed, Is.True);
        Assert.That(_lab3Executed, Is.True);
        CheckNode(node_2, Node<Model>.NodeStatus.Success, 1, _node, null, _lab2, null, true);
        CheckNode(node_3, Node<Model>.NodeStatus.Success, 1, _node, null, _lab3, null, true);
    }
    #endregion



    #region Detach And Restore
    [Test]
    public void DetachAndRestore()
    {
        // Arrange
        _node = new Node<Model>(null);
        _lab1Success = false;

        var after = _node.Append(_lab1, _cts.Token);
        after.Execute();

        var originalReport = after.Report;
        var originalParentReport = after.Report.ParentNode;

        // Act
        var restored = after.DetachAndRestore(_cts.Token);

        // Assert
        Assert.That(after.Report, Is.SameAs(originalReport));
        Assert.That(after.Report.ParentNode, Is.SameAs(originalParentReport));
        Assert.That(after.Report.InnerText, Is.EqualTo(Node<Model>.NodeStatus.Failure.ToString()));

        Assert.That(restored.Report, Is.Not.SameAs(originalReport));
        Assert.That(restored.Before, Is.Null);
        Assert.That(_node.Afters, Is.EqualTo(new[] { after }));
        Assert.That(restored.Afters, Is.Empty);

        var restoredParentReport = restored.Report.ParentNode;
        Assert.That(restoredParentReport, Is.Not.Null);
        Assert.That(restoredParentReport.Name, Is.EqualTo(_node.Report.Name));
        Assert.That(restoredParentReport, Is.Not.SameAs(_node.Report));
        Assert.That(restoredParentReport.ParentNode, Is.InstanceOf<XmlDocument>());
        Assert.That(restored.Report.Name, Is.EqualTo(after.ID));
        Assert.That(restored.Report.InnerText, Is.EqualTo("Waiting for execution"));
    }
    [Test]
    public void DetachAndRestore_PreservesFailure()
    {
        // Arrange
        _node = new Node<Model>(null);
        _lab1Success = false;

        var after = _node.Append(_lab1, _cts.Token);
        after.Execute();

        var restored = after.DetachAndRestore(_cts.Token);
        _lab1Success = true;

        // Act
        restored.Execute();

        // Assert
        Assert.That(after.Status, Is.EqualTo(Node<Model>.NodeStatus.Failure));
        Assert.That(restored.Status, Is.EqualTo(Node<Model>.NodeStatus.Success));
        Assert.That(_node.Afters, Is.EqualTo(new[] { after }));

        var originalFailureReport = _node.Report.ChildNodes.OfType<XmlElement>().Single();
        Assert.That(originalFailureReport, Is.SameAs(after.Report));
        Assert.That(_node.Report.ChildNodes.OfType<XmlElement>().Count(), Is.EqualTo(1));
        Assert.That(restored.Report.ParentNode, Is.Not.SameAs(_node.Report));
        Assert.That(after.Report.InnerText, Is.EqualTo(Node<Model>.NodeStatus.Failure.ToString()));
        Assert.That(restored.Report.InnerText, Is.EqualTo(Node<Model>.NodeStatus.Success.ToString()));

        Assert.That(_node.AllSucceed(out var cancelled), Is.False);
        Assert.That(cancelled, Is.False);

        var failedReport = _node.GetFailedReports();
        var failedReports = failedReport.ChildNodes.OfType<XmlElement>().ToArray();
        Assert.That(failedReports.Length, Is.EqualTo(1));
        Assert.That(failedReports[0].OuterXml, Is.EqualTo(after.Report.OuterXml));
    }
    [Test]
    public void DetachAndRestore_ReplaysPreviousTrace()
    {
        // Arrange
        _node = new Node<Model>(null);
        _lab2Success = false;

        var before = _node.Append(_lab1, _cts.Token);
        before.Execute();

        var after = before.Append(_lab2, _cts.Token);
        after.Execute();

        var sibling = _node.Append(_lab3, _cts.Token);

        _lab1Executed = false;
        _lab2Success = true;

        // Act
        var restored = after.DetachAndRestore(_cts.Token);
        restored.Execute();

        // Assert
        Assert.That(_lab1Executed, Is.True);
        Assert.That(restored.Before, Is.Null);
        Assert.That(restored.Afters, Is.Empty);
        Assert.That(_node.Afters, Is.EqualTo(new[] { before, sibling }));
        Assert.That(before.Afters, Is.EqualTo(new[] { after }));

        var copiedBeforeReport = restored.Report.ParentNode;
        Assert.That(copiedBeforeReport, Is.Not.Null);
        Assert.That(copiedBeforeReport.Name, Is.EqualTo(before.Report.Name));
        Assert.That(copiedBeforeReport, Is.Not.SameAs(before.Report));

        var copiedRootReport = copiedBeforeReport.ParentNode;
        Assert.That(copiedRootReport, Is.Not.Null);
        Assert.That(copiedRootReport.Name, Is.EqualTo(_node.Report.Name));
        Assert.That(copiedRootReport, Is.Not.SameAs(_node.Report));
        var copiedRootChildren = copiedRootReport.ChildNodes.OfType<XmlElement>().ToArray();
        Assert.That(copiedRootChildren.Length, Is.EqualTo(1));
        Assert.That(copiedRootChildren[0], Is.SameAs(copiedBeforeReport));

        Assert.That(restored.Depth, Is.EqualTo(2));
        Assert.That(restored.Model.ExecutionCount, Is.EqualTo(2));
        Assert.That(restored.Status, Is.EqualTo(Node<Model>.NodeStatus.Success));
    }
    [Test]
    public void DetachAndRestore_RestoredNode()
    {
        // Arrange
        _node = new Node<Model>(null);
        _lab1Success = false;

        var after = _node.Append(_lab1, _cts.Token);
        after.Execute();

        var restored = after.DetachAndRestore(_cts.Token);
        _lab1Success = true;
        restored.Execute();

        // Act
        var restoredAgain = restored.DetachAndRestore(_cts.Token);

        // Assert
        Assert.That(restoredAgain.Before, Is.Null);
        Assert.That(_node.Afters, Is.EqualTo(new[] { after }));
        Assert.That(restored.Afters, Is.Empty);
        Assert.That(restoredAgain.Afters, Is.Empty);

        var copiedRootReport = restoredAgain.Report.ParentNode;
        Assert.That(copiedRootReport, Is.Not.Null);
        Assert.That(copiedRootReport.Name, Is.EqualTo(_node.Report.Name));
        Assert.That(copiedRootReport, Is.Not.SameAs(_node.Report));
        Assert.That(copiedRootReport, Is.Not.SameAs(restored.Report.ParentNode));
        Assert.That(restoredAgain.Report.InnerText, Is.EqualTo("Waiting for execution"));
    }
    #endregion
}
