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
    Node<Model> node;
    CancellationTokenSource cts;
    const int DELAY = 100;

    Lab<Model> lab_1;
    Lab<Model> lab_2;
    Lab<Model> lab_3;

    bool lab_1_success;
    bool lab_2_success;
    bool lab_3_success;

    bool lab_1_executed;
    bool lab_2_executed;
    bool lab_3_executed;


    [SetUp]
    public void SetUp()
    {
        lab_1 = new Lab<Model>("lab_1")
        {
            Arranger = (_, _) => lab_1_executed = true,
            Actor = (_, _) => { if (!lab_1_success) throw new ProbeException(); }
        };
        lab_2 = new Lab<Model>("lab_1")
        {
            Arranger = (_, _) => lab_2_executed = true,
            Actor = (_, _) => { if (!lab_2_success) throw new ProbeException(); }
        };
        lab_3 = new Lab<Model>("lab_3")
        {
            Arranger = (_, _) => lab_3_executed = true,
            Actor = (_, _) => { if (!lab_3_success) throw new ProbeException(); }
        };

        lab_1_success = true;
        lab_2_success = true;
        lab_3_success = true;

        lab_1_executed = false;
        lab_2_executed = false;
        lab_3_executed = false;

        cts = new();
    }

    [TearDown]
    public void TearDown()
    {
        node = null;

        lab_1 = null;
        lab_2 = null;
        lab_3 = null;

        cts.Cancel();
        cts.Dispose();
    }

    public void CheckNode(Node<Model> node,
        Node<Model>.NodeStatus status, int depth, Node<Model> before, IEnumerable<Node<Model>> afters, Lab<Model> lab, Type exception, bool continuable)
    {
        Assert.AreEqual(status, node.Status);
        Assert.AreEqual(depth, node.Depth);
        Assert.AreSame(before, node.Before);
        CollectionAssert.AreEquivalent(afters ?? Array.Empty<Node<Model>>(), node.Afters);

        Assert.AreSame(lab, node.Lab);
        Assert.AreSame(lab?.ID ?? "root", node.ID);
        Assert.IsNotNull(node.Model);

        if (exception != null)
            Assert.IsAssignableFrom(exception, node.Exception);
        else
            Assert.IsNull(node.Exception);

        Assert.AreEqual(continuable, node.Continuable);
    }



    #region State Transition
    [Test]
    public void Create_NullLab()
    {
        // Act
        node = new Node<Model>(null);

        // Assert
        CheckNode(node, Node<Model>.NodeStatus.Root, 0, null, null, null, null, true);
    }
    [Test]
    public void Create_Lab()
    {
        // Act
        node = new Node<Model>(lab_1);

        // Assert
        CheckNode(node, Node<Model>.NodeStatus.Ready, 0, null, null, lab_1, null, false);
    }


    [Test]
    public void Executed_Continuable()
    {
        // Arrange
        node = new Node<Model>(lab_1);

        // Act
        node.Execute();

        // Assert
        CheckNode(node, Node<Model>.NodeStatus.Success, 0, null, null, lab_1, null, true);
        Assert.True(lab_1_executed);
    }
    [Test]
    public void Executed_NotContinuable()
    {
        // Arrange
        node = new Node<Model>(lab_1);
        lab_1_success = false;

        // Act
        node.Execute();

        // Assert
        CheckNode(node, Node<Model>.NodeStatus.Failure, 0, null, null, lab_1, typeof(ExecutionException), false);
        Assert.True(lab_1_executed);
    }
    #endregion



    #region Execute
    [Test]
    public void Execute_NullLab()
    {
        // Arrange
        node = new Node<Model>(null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => node.Execute());
    }
    [Test]
    public void Execute_Idle_Success()
    {
        // Arrange
        node = new Node<Model>(lab_1);

        // Act
        node.Execute();

        // Assert
        CheckNode(node, Node<Model>.NodeStatus.Success, 0, null, null, lab_1, null, true);
        Assert.True(lab_1_executed);
    }
    [Test]
    public void Execute_Idle_Failure()
    {
        // Arrange
        node = new Node<Model>(lab_1);
        lab_1_success = false;

        // Act
        node.Execute();

        // Assert
        CheckNode(node, Node<Model>.NodeStatus.Failure, 0, null, null, lab_1, typeof(ExecutionException), false);
        Assert.True(lab_1_executed);
    }
    [Test]
    public void Execute_Executed_Continuable()
    {
        // Arrange
        node = new Node<Model>(lab_1);
        node.Execute();

        // Act & Assert
        Assert.True(lab_1_executed);
        Assert.Throws<InvalidOperationException>(() => node.Execute());
    }
    [Test]
    public void Execute_Executed_NotContinuable()
    {
        // Arrange
        node = new Node<Model>(lab_1);
        lab_1_success = false;

        node.Execute();

        // Act & Assert
        Assert.True(lab_1_executed);
        Assert.Throws<InvalidOperationException>(() => node.Execute());
    }
    #endregion



    #region Set External Exception
    [Test]
    public void SetExternalException_Valid_NullLab()
    {
        // Arrange
        node = new Node<Model>(null);

        // Act 
        node.SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Cancelled);

        // Assert
        CheckNode(node, Node<Model>.NodeStatus.Cancelled, 0, null, null, null, typeof(ProbeException), false);
    }
    [Test]
    public void SetExternalException_Valid_Idle()
    {
        // Arrange
        node = new Node<Model>(lab_1);

        // Act 
        node.SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Cancelled);

        // Assert
        CheckNode(node, Node<Model>.NodeStatus.Cancelled, 0, null, null, lab_1, typeof(ProbeException), false);
    }
    [Test]
    public void SetExternalException_Valid_Executed_Continuable()
    {
        // Arrange
        node = new Node<Model>(lab_1);
        node.Execute();

        // Act 
        node.SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Cancelled);

        // Assert
        Assert.True(lab_1_executed);
        CheckNode(node, Node<Model>.NodeStatus.Cancelled, 0, null, null, lab_1, typeof(ProbeException), false);
    }
    [Test]
    public void SetExternalException_Valid_Executed_NotContinuable()
    {
        // Arrange
        node = new Node<Model>(lab_1);
        lab_1_success = false;

        node.Execute();


        // Act 
        Assert.True(lab_1_executed);
        node.SetExternalException(new ProbeException(), Node<Model>.NodeStatus.Cancelled);

        // Assert
        CheckNode(node, Node<Model>.NodeStatus.Cancelled, 0, null, null, lab_1, typeof(ProbeException), false);
    }


    [Test]
    public void SetExternalException_Null_NullLab()
    {
        // Arrange
        node = new Node<Model>(null);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => node.SetExternalException(null, Node<Model>.NodeStatus.Cancelled));
    }
    [Test]
    public void SetExternalException_Null_Idle()
    {
        // Arrange
        node = new Node<Model>(lab_1);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => node.SetExternalException(null, Node<Model>.NodeStatus.Cancelled));
    }
    [Test]
    public void SetExternalException_Null_Executed_Continuable()
    {
        // Arrange
        node = new Node<Model>(lab_1);
        node.Execute();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => node.SetExternalException(null, Node<Model>.NodeStatus.Cancelled));
    }
    [Test]
    public void SetExternalException_Null_Executed_NotContinuable()
    {
        // Arrange
        node = new Node<Model>(lab_1);
        lab_1_success = false;

        node.Execute();


        // Act & Assert
        Assert.True(lab_1_executed);
        Assert.Throws<ArgumentNullException>(() => node.SetExternalException(null, Node<Model>.NodeStatus.Cancelled));
    }
    #endregion



    #region Append
    [Test]
    public void Append_NullLab_NullLab()
    {
        // Arrange
        node = new Node<Model>(null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new Node<Model>(node, null, cts.Token));
    }
    [TestCase(false), TestCase(true)]
    public void Append_NullLab_Idle(bool postExecute)
    {
        // Arrange
        node = new Node<Model>(null);

        // Act
        var after = new Node<Model>(node, lab_2, cts.Token);
        if (postExecute) after.Execute();

        // Assert
        CheckNode(node, Node<Model>.NodeStatus.Root, 0, null, new[] { after }, null, null, true);

        if (!postExecute)
        {
            Assert.False(lab_2_executed);
            CheckNode(after, Node<Model>.NodeStatus.Ready, 1, node, null, lab_2, null, false);
        }
        else
        {
            Assert.True(lab_2_executed);
            CheckNode(after, Node<Model>.NodeStatus.Success, 1, node, null, lab_2, null, true);
        }
    }
    [Test]
    public async Task Append_NullLab_Idle_Cancelled()
    {
        // Arrange
        node = new Node<Model>(null);
        cts.Cancel();

        Node<Model> after = null;
        Exception ex = null;

        // Act
        try
        {
            await Task.Run(() => after = new Node<Model>(node, lab_2, cts.Token));
        }
        catch (Exception _ex)
        {
            ex = _ex;
        }

        // Assert
        CheckNode(node, Node<Model>.NodeStatus.Root, 0, null, null, null, null, true);
        Assert.IsAssignableFrom<OperationCanceledException>(ex);
        Assert.IsNull(after);
    }

    
    [Test]
    public void Append_Idle_NullLab()
    {
        // Arrange
        node = new Node<Model>(lab_1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new Node<Model>(node, null, cts.Token));
    }
    [Test]
    public void Append_Idle_Idle()
    {
        // Arrange
        node = new Node<Model>(lab_1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new Node<Model>(node, lab_2, cts.Token));
    }


    [Test]
    public void Append_Executed_Continuable_NullLab()
    {
        // Arrange
        node = new Node<Model>(lab_1);
        node.Execute();

        // Act & Assert
        Assert.True(lab_1_executed);
        Assert.Throws<InvalidOperationException>(() => new Node<Model>(node, null, cts.Token));
    }
    [TestCase(false), TestCase(true)]
    public void Append_Executed_Continuable_Idle(bool postExecute)
    {
        // Arrange
        node = new Node<Model>(lab_1);
        node.Execute();

        // Act
        var after = new Node<Model>(node, lab_2, cts.Token);
        if (postExecute) after.Execute();

        // Assert
        Assert.True(lab_1_executed);
        CheckNode(node, Node<Model>.NodeStatus.Success, 0, null, new[] { after }, lab_1, null, true);

        if (!postExecute)
        {
            Assert.False(lab_2_executed);
            CheckNode(after, Node<Model>.NodeStatus.Ready, 1, node, null, lab_2, null, false);
        }
        else
        {
            Assert.True(lab_2_executed);
            CheckNode(after, Node<Model>.NodeStatus.Success, 1, node, null, lab_2, null, true);
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
            Thread.Sleep(DELAY);
        });

        Node<Model> after = null;
        Exception ex = null;

        node = new Node<Model>(lab_1);
        node.Execute();
        cts.Cancel();

        // Act
        try
        {
            await Task.Run(() => after = new Node<Model>(node, lab_2, cts.Token));
        }
        catch (Exception _ex)
        {
            ex = _ex;
        }

        // Assert
        Assert.AreEqual(1, lab_1_executionCount);

        CheckNode(node, Node<Model>.NodeStatus.Success, 0, null, null, lab_1, null, true);

        Assert.IsAssignableFrom<OperationCanceledException>(ex);
        Assert.IsNull(after);
    }
    [Test]
    public async Task Append_Executed_Continuable_Idle_Cancelling()
    {
        // Arrange
        var lab_1_executionCount = 0;

        var lab_1 = new Lab<Model>(actor: (_, _) =>
        {
            lab_1_executionCount++;
            Thread.Sleep(DELAY);
        });

        Node<Model> after = null;
        Exception ex = null;

        node = new Node<Model>(lab_1);
        node.Execute();

        // Act
        var task = Task.Run(() => after = new Node<Model>(node, lab_2, cts.Token));
        await Task.Run(async () =>
        {
            await Task.Delay(DELAY / 2);
            cts.Cancel();
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
        Assert.AreEqual(2, lab_1_executionCount);

        CheckNode(node, Node<Model>.NodeStatus.Success, 0, null, null, lab_1, null, true);

        Assert.IsAssignableFrom<OperationCanceledException>(ex);
        Assert.IsNull(after);
    }


    [Test]
    public void Append_Executed_NotContinuable_NullLab()
    {
        // Arrange
        node = new Node<Model>(lab_1);
        lab_1_success = false;
        node.Execute();

        // Act & Assert
        Assert.True(lab_1_executed);
        Assert.Throws<InvalidOperationException>(() => new Node<Model>(node, null, cts.Token));
    }
    [Test]
    public void Append_Executed_NotContinuable_Idle()
    {
        // Arrange
        node = new Node<Model>(lab_1);
        lab_1_success = false;
        node.Execute();

        // Act & Assert
        Assert.True(lab_1_executed);
        Assert.Throws<InvalidOperationException>(() => new Node<Model>(node, lab_2, cts.Token));
    }


    [TestCase(false), TestCase(true)]
    public void Append_Multi_Sequential(bool beforeIsRoot)
    {
        // Arrange
        node = new Node<Model>(beforeIsRoot ? null : lab_1);
        if (!beforeIsRoot) node.Execute();

        var node_2 = new Node<Model>(node, lab_2, cts.Token);
        var node_3 = new Node<Model>(node, lab_3, cts.Token);


        // Act
        Assert.DoesNotThrow(() => node_2.Execute());
        Assert.DoesNotThrow(() => node_3.Execute());

        // Assert
        if (beforeIsRoot)
        {
            Assert.IsFalse(lab_1_executed);
            CheckNode(node, Node<Model>.NodeStatus.Root, 0, null, new[] { node_2, node_3 }, null, null, true);
        }
        else
        {
            Assert.IsTrue(lab_1_executed);
            CheckNode(node, Node<Model>.NodeStatus.Success, 0, null, new[] { node_2, node_3 }, lab_1, null, true);
        }

        Assert.IsTrue(lab_2_executed);
        Assert.IsTrue(lab_3_executed);
        CheckNode(node_2, Node<Model>.NodeStatus.Success, 1, node, null, lab_2, null, true);
        CheckNode(node_3, Node<Model>.NodeStatus.Success, 1, node, null, lab_3, null, true);
    }
    [TestCase(false), TestCase(true)]
    public void Append_Multi_NotSequential(bool beforeIsRoot)
    {
        // Arrange
        node = new Node<Model>(beforeIsRoot ? null : lab_1);
        if (!beforeIsRoot) node.Execute();


        // Act
        var node_2 = new Node<Model>(node, lab_2, cts.Token);
        Assert.DoesNotThrow(() => node_2.Execute());

        var node_3 = new Node<Model>(node, lab_3, cts.Token);
        Assert.DoesNotThrow(() => node_3.Execute());


        // Assert
        if (beforeIsRoot)
        {
            Assert.IsFalse(lab_1_executed);
            CheckNode(node, Node<Model>.NodeStatus.Root, 0, null, new[] { node_2, node_3 }, null, null, true);
        }
        else
        {
            Assert.IsTrue(lab_1_executed);
            CheckNode(node, Node<Model>.NodeStatus.Success, 0, null, new[] { node_2, node_3 }, lab_1, null, true);
        }

        Assert.IsTrue(lab_2_executed);
        Assert.IsTrue(lab_3_executed);
        CheckNode(node_2, Node<Model>.NodeStatus.Success, 1, node, null, lab_2, null, true);
        CheckNode(node_3, Node<Model>.NodeStatus.Success, 1, node, null, lab_3, null, true);
    }
    #endregion



    #region Detach And Restore
    [Test]
    public void DetachAndRestore()
    {
        // Arrange
        node = new Node<Model>(null);
        lab_1_success = false;

        var after = node.Append(lab_1, cts.Token);
        after.Execute();

        var originalReport = after.Report;
        var originalParentReport = after.Report.ParentNode;

        // Act
        var restored = after.DetachAndRestore(cts.Token);

        // Assert
        Assert.AreSame(originalReport, after.Report);
        Assert.AreSame(originalParentReport, after.Report.ParentNode);
        Assert.AreEqual(Node<Model>.NodeStatus.Failure.ToString(), after.Report.InnerText);

        Assert.AreNotSame(originalReport, restored.Report);
        Assert.IsNull(restored.Before);
        CollectionAssert.AreEqual(new[] { after }, node.Afters);
        CollectionAssert.IsEmpty(restored.Afters);

        var restoredParentReport = restored.Report.ParentNode;
        Assert.IsNotNull(restoredParentReport);
        Assert.AreEqual(node.Report.Name, restoredParentReport.Name);
        Assert.AreNotSame(node.Report, restoredParentReport);
        Assert.IsInstanceOf<XmlDocument>(restoredParentReport.ParentNode);
        Assert.AreEqual(after.ID, restored.Report.Name);
        Assert.AreEqual("Waiting for execution", restored.Report.InnerText);
    }
    [Test]
    public void DetachAndRestore_PreservesFailure()
    {
        // Arrange
        node = new Node<Model>(null);
        lab_1_success = false;

        var after = node.Append(lab_1, cts.Token);
        after.Execute();

        var restored = after.DetachAndRestore(cts.Token);
        lab_1_success = true;

        // Act
        restored.Execute();

        // Assert
        Assert.AreEqual(Node<Model>.NodeStatus.Failure, after.Status);
        Assert.AreEqual(Node<Model>.NodeStatus.Success, restored.Status);
        CollectionAssert.AreEqual(new[] { after }, node.Afters);

        var originalFailureReport = node.Report.ChildNodes.OfType<XmlElement>().Single();
        Assert.AreSame(after.Report, originalFailureReport);
        Assert.AreEqual(1, node.Report.ChildNodes.OfType<XmlElement>().Count());
        Assert.AreNotSame(node.Report, restored.Report.ParentNode);
        Assert.AreEqual(Node<Model>.NodeStatus.Failure.ToString(), after.Report.InnerText);
        Assert.AreEqual(Node<Model>.NodeStatus.Success.ToString(), restored.Report.InnerText);

        Assert.IsFalse(node.AllSucceed(out var cancelled));
        Assert.IsFalse(cancelled);

        var failedReport = node.GetFailedReports();
        var failedReports = failedReport.ChildNodes.OfType<XmlElement>().ToArray();
        Assert.AreEqual(1, failedReports.Length);
        Assert.AreEqual(after.Report.OuterXml, failedReports[0].OuterXml);
    }
    [Test]
    public void DetachAndRestore_ReplaysPreviousTrace()
    {
        // Arrange
        node = new Node<Model>(null);
        lab_2_success = false;

        var before = node.Append(lab_1, cts.Token);
        before.Execute();

        var after = before.Append(lab_2, cts.Token);
        after.Execute();

        var sibling = node.Append(lab_3, cts.Token);

        lab_1_executed = false;
        lab_2_success = true;

        // Act
        var restored = after.DetachAndRestore(cts.Token);
        restored.Execute();

        // Assert
        Assert.IsTrue(lab_1_executed);
        Assert.IsNull(restored.Before);
        CollectionAssert.IsEmpty(restored.Afters);
        CollectionAssert.AreEqual(new[] { before, sibling }, node.Afters);
        CollectionAssert.AreEqual(new[] { after }, before.Afters);

        var copiedBeforeReport = restored.Report.ParentNode;
        Assert.IsNotNull(copiedBeforeReport);
        Assert.AreEqual(before.Report.Name, copiedBeforeReport.Name);
        Assert.AreNotSame(before.Report, copiedBeforeReport);

        var copiedRootReport = copiedBeforeReport.ParentNode;
        Assert.IsNotNull(copiedRootReport);
        Assert.AreEqual(node.Report.Name, copiedRootReport.Name);
        Assert.AreNotSame(node.Report, copiedRootReport);
        var copiedRootChildren = copiedRootReport.ChildNodes.OfType<XmlElement>().ToArray();
        Assert.AreEqual(1, copiedRootChildren.Length);
        Assert.AreSame(copiedBeforeReport, copiedRootChildren[0]);

        Assert.AreEqual(2, restored.Depth);
        Assert.AreEqual(2, restored.Model.ExecutionCount);
        Assert.AreEqual(Node<Model>.NodeStatus.Success, restored.Status);
    }
    [Test]
    public void DetachAndRestore_RestoredNode()
    {
        // Arrange
        node = new Node<Model>(null);
        lab_1_success = false;

        var after = node.Append(lab_1, cts.Token);
        after.Execute();

        var restored = after.DetachAndRestore(cts.Token);
        lab_1_success = true;
        restored.Execute();

        // Act
        var restoredAgain = restored.DetachAndRestore(cts.Token);

        // Assert
        Assert.IsNull(restoredAgain.Before);
        CollectionAssert.AreEqual(new[] { after }, node.Afters);
        CollectionAssert.IsEmpty(restored.Afters);
        CollectionAssert.IsEmpty(restoredAgain.Afters);

        var copiedRootReport = restoredAgain.Report.ParentNode;
        Assert.IsNotNull(copiedRootReport);
        Assert.AreEqual(node.Report.Name, copiedRootReport.Name);
        Assert.AreNotSame(node.Report, copiedRootReport);
        Assert.AreNotSame(restored.Report.ParentNode, copiedRootReport);
        Assert.AreEqual("Waiting for execution", restoredAgain.Report.InnerText);
    }
    #endregion
}
