using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UniTest;

public class TestDesignerTest
{
    object testDesigner;
    int DELAY = 100;

    Lab<Model> lab;
    Node<Model> node;

    Func<Model, IEnumerable<ILab<Model>>> createLabs;
    ProjectMock project;

    ICollection completedDesigners;
    Queue<Node<Model>> preparedNodes;
    CancellationTokenSource cts;


    void SetUp(Func<Model, IEnumerable<ILab<Model>>> createLabs, bool withRootNode, bool secondFailure = false)
    {
        project = new ProjectMock(createLabs);

        completedDesigners = (ICollection)typeof(Project<Model>)
            .GetField("completedDesigners", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(project);

        preparedNodes = (Queue<Node<Model>>)typeof(Project<Model>)
            .GetField("preparedNodes", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(project);

        cts = new();
        int executionCount = 0;

        if (withRootNode)
            lab = null;
        else
            lab = !secondFailure
                ? new Lab<Model>()
                : new Lab<Model>(actor: (_, _) =>
                {
                    if (executionCount++ >= 1)
                        throw new ProbeException();
                });

        node = new Node<Model>(lab);

        if (!withRootNode) node.Execute();

        var ctorInfo = typeof(Project<Model>)
            .GetNestedType("TestDesigner", BindingFlags.NonPublic)
            .MakeGenericType(typeof(Model))
            .GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(Project<Model>), typeof(Node<Model>) },
                modifiers: null
            );

        testDesigner = ctorInfo.Invoke(new object[] { project, node });
    }

    [TearDown]
    public void TearDown()
    {
        project = null;
        testDesigner = null;
        node = null;
        lab = null;

        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }

        completedDesigners = null;
        preparedNodes = null;
    }

    public void CheckNode(Node<Model> node, Node<Model>.NodeStatus status, int depth,
        Node<Model> before, IEnumerable<Node<Model>> afters, Type expectedExceptionType, Lab<Model> lab)
    {
        Assert.AreEqual(status, node.Status, node.Exception?.Message ?? "No exception");
        Assert.AreEqual(depth, node.Depth);
        Assert.AreSame(before, node.Before);
        CollectionAssert.AreEquivalent(afters ?? Array.Empty<Node<Model>>(), node.Afters);

        if (expectedExceptionType != null)
            Assert.IsAssignableFrom(expectedExceptionType, node.Exception);
        else
            Assert.IsNull(node.Exception);

        Assert.AreSame(lab, node.Lab);
        Assert.AreSame(lab?.ID ?? "root", node.ID);
        Assert.IsNotNull(node.Model);
    }

    async Task Execute()
    {
        var method = testDesigner.GetType()
            .GetMethod(
                "Execute",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(CancellationToken) },
                modifiers: null
            );

        await (Task)method.Invoke(testDesigner, new object[] { cts.Token });
    }



    #region Idle - Execute
    [TestCase(true), TestCase(false)]
    public async Task Execute_Idle_Normal(bool withRootNode)
    {
        // Arrange
        var labs = new[] { new Lab<Model>(), new Lab<Model>() };
        SetUp(_ => labs, withRootNode);

        // Act
        await Execute();

        // Assert
        CollectionAssert.AreEqual(new[] { testDesigner }, completedDesigners);
        CollectionAssert.AreEqual(labs, preparedNodes.Select(n => n.Lab));

        CheckNode(node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, preparedNodes, null, lab);

        int i = 0;
        foreach (var _node in preparedNodes)
            CheckNode(_node, Node<Model>.NodeStatus.Ready, 1, node, null, null, labs[i++]);
    }
    [TestCase(true), TestCase(false)]
    public void Execute_Idle_ExecptionThrown_CreateLab(bool withRootNode)
    {
        // Arrange
        SetUp(_ => throw new ProbeException(), withRootNode);

        // Act & Assert
        Assert.ThrowsAsync<ExecutionException>(() => Execute());
    }
    [Test]
    public void Execute_Idle_ExecptionThrown_Append()
    {
        // Arrange
        var labs = new[] { new Lab<Model>(), new Lab<Model>() };
        SetUp(_ => labs, false, true);

        // Act & Assert
        Assert.ThrowsAsync<ExecutionException>(() => Execute());
    }
    [TestCase(true), TestCase(false)]
    public async Task Execute_Idle_NoTests(bool withRootNode)
    {
        // Arrange
        SetUp(null, withRootNode);

        // Act
        await Execute();

        // Assert
        CollectionAssert.AreEqual(new[] { testDesigner }, completedDesigners);
        CollectionAssert.IsEmpty(preparedNodes);

        CheckNode(node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, lab);
    }
    #endregion

    #region Idle - On Cancel
    [TestCase(true), TestCase(false)]
    public void OnCancel_Idle_Normal(bool withRootNode)
    {
        // Arrange
        var labs = new[] { new Lab<Model>(), new Lab<Model>() };
        SetUp(_ => labs, withRootNode);

        // Act
        cts.Cancel();

        // Assert
        CollectionAssert.IsEmpty(completedDesigners);
        CollectionAssert.IsEmpty(preparedNodes);

        CheckNode(node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, lab);
    }
    [TestCase(true), TestCase(false)]
    public void OnCancel_Idle_ExecptionThrown_CreateLabs(bool withRootNode)
    {
        // Arrange
        SetUp(_ => throw new ProbeException(), withRootNode);

        // Act
        cts.Cancel();

        // Assert
        CollectionAssert.IsEmpty(completedDesigners);
        CollectionAssert.IsEmpty(preparedNodes);

        CheckNode(node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, lab);
    }
    [Test]
    public void OnCancel_Idle_ExecptionThrown_CreateLabs_Append()
    {
        // Arrange
        var labs = new[] { new Lab<Model>(), new Lab<Model>() };
        SetUp(_ => labs, false, true);

        // Act
        cts.Cancel();

        // Assert
        CollectionAssert.IsEmpty(completedDesigners);
        CollectionAssert.IsEmpty(preparedNodes);
        
        CheckNode(node, Node<Model>.NodeStatus.Success, 0, null, null, null, lab);
    }
    [TestCase(true), TestCase(false)]
    public void OnCancel_Idle_NoTests(bool withRootNode)
    {
        // Arrange
        SetUp(null, withRootNode);

        // Act
        cts.Cancel();

        // Assert
        CollectionAssert.IsEmpty(completedDesigners);
        CollectionAssert.IsEmpty(preparedNodes);

        CheckNode(node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, lab);
    }
    #endregion



    #region Cancelled - Execute
    [TestCase(true), TestCase(false)]
    public async Task Execute_Cancelled_Normal(bool withRootNode)
    {
        // Arrange
        var labs = new[] { new Lab<Model>(), new Lab<Model>() };
        SetUp(_ => labs, withRootNode);
        cts.Cancel();

        // Act
        await Execute();

        // Assert
        CollectionAssert.AreEqual(new[] { testDesigner }, completedDesigners);
        CollectionAssert.IsEmpty(preparedNodes);
        CheckNode(node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, lab);
    }
    [TestCase(true), TestCase(false)]
    public async Task Execute_Cancelled_ExecptionThrown_CreateLab(bool withRootNode)
    {
        // Arrange
        SetUp(_ => throw new ProbeException(), withRootNode);
        cts.Cancel();

        // Act
        await Execute();

        // Assert
        CollectionAssert.AreEqual(new[] { testDesigner }, completedDesigners);
        CollectionAssert.IsEmpty(preparedNodes);
        CheckNode(node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, lab);
    }
    [Test]
    public async Task Execute_Cancelled_ExecptionThrown_Append()
    {
        // Arrange
        var labs = new[] { new Lab<Model>(), new Lab<Model>() };
        SetUp(_ => labs, false, true);
        cts.Cancel();

        // Act
        await Execute();

        // Assert
        CollectionAssert.AreEqual(new[] { testDesigner }, completedDesigners);
        CollectionAssert.IsEmpty(preparedNodes);
        CheckNode(node, Node<Model>.NodeStatus.Success, 0, null, preparedNodes, null, lab);
    }
    [TestCase(true), TestCase(false)]
    public async Task Execute_Cancelled_NoTests(bool withRootNode)
    {
        // Arrange
        SetUp(null, withRootNode);
        cts.Cancel();

        // Act
        await Execute();

        // Assert
        CollectionAssert.AreEqual(new[] { testDesigner }, completedDesigners);
        CollectionAssert.IsEmpty(preparedNodes);
        CheckNode(node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, lab);
    }
    #endregion

 

    #region Executing - Execute
    [TestCase(true), TestCase(false)]
    public void Execute_Executing_Normal(bool withRootNode)
    {
        // Arrange
        var labs = new[] { new Lab<Model>(), new Lab<Model>() };
        createLabs = _ =>
        {
            Thread.Sleep(DELAY);
            return labs;
        };
        
        SetUp(createLabs, withRootNode);


        // Act & Assert
        _ = Execute();
        Assert.ThrowsAsync<InvalidOperationException>(() => Execute());
    }
    [TestCase(true), TestCase(false)]
    public void Execute_Executing_NoTests(bool withRootNode)
    {
        // Arrange
        SetUp(null, withRootNode);

        // Act & Assert
        _ = Execute();
        Assert.ThrowsAsync<InvalidOperationException>(() => Execute());
    }
    #endregion

    #region Executing - On Cancel
    [TestCase(true), TestCase(false)]
    public async Task OnCancel_Executing_Normal(bool withRootNode)
    {
        // Arrange
        var labs = new[] { new Lab<Model>(), new Lab<Model>() };
        createLabs = _ =>
        {
            Thread.Sleep(DELAY);
            return labs;
        };

        SetUp(createLabs, withRootNode);
        var task = Execute();


        // Act
        cts.Cancel();
        await task;


        // Assert
        CollectionAssert.AreEqual(new[] { testDesigner }, completedDesigners);
        CollectionAssert.IsEmpty(preparedNodes);
        CheckNode(node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, lab);
    }
    [TestCase(true), TestCase(false)]
    public async Task OnCancel_Executing_NoTest(bool withRootNode)
    {
        // Arrange
        createLabs = _ =>
        {
            Thread.Sleep(DELAY);
            return null;
        };

        SetUp(createLabs, withRootNode);
        var task = Execute();

        // Act
        cts.Cancel();
        await task;


        // Assert
        CollectionAssert.AreEqual(new[] { testDesigner }, completedDesigners);
        CollectionAssert.IsEmpty(preparedNodes);
        CheckNode(node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, lab);
    }
    #endregion



    #region Executed - Execute
    [TestCase(true), TestCase(false)]
    public async Task Execute_Executed_Normal(bool withRootNode)
    {
        // Arrange
        var labs = new[] { new Lab<Model>(), new Lab<Model>() };
        SetUp(_ => labs, withRootNode);

        await Execute();


        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => Execute());
    }
    [TestCase(true), TestCase(false)]
    public async Task Execute_Executed_ExecptionThrown_CreateLab(bool withRootNode)
    {
        // Arrange
        SetUp(_ => throw new ProbeException(), withRootNode);

        try
        {
            await Execute();
        }
        catch { }


        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => Execute());
    }
    [Test]
    public async Task Execute_Executed_ExecptionThrown_Append()
    {
        // Arrange
        var labs = new[] { new Lab<Model>(), new Lab<Model>() };
        SetUp(_ => labs, false, true);

        try
        {
            await Execute();
        }
        catch { }


        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => Execute());
    }
    [TestCase(true), TestCase(false)]
    public async Task Execute_Executed_NoTests(bool withRootNode)
    {
        // Arrange
        SetUp(null, withRootNode);
        await Execute();

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => Execute());
    }
    #endregion

    #region Executed - On Cancel
    [TestCase(true), TestCase(false)]
    public async Task OnCancel_Executed_Normal(bool withRootNode)
    {
        // Arrange
        var labs = new[] { new Lab<Model>(), new Lab<Model>() };
        SetUp(_ => labs, withRootNode);

        await Execute();


        // Act & Assert
        Assert.DoesNotThrow(() => cts.Cancel());
    }
    [TestCase(true), TestCase(false)]
    public async Task OnCancel_Executed_ExecptionThrown_CreateLabs(bool withRootNode)
    {
        // Arrange
        SetUp(_ => throw new ProbeException(), withRootNode);

        try
        {
            await Execute();
        }
        catch { }


        // Act & Assert
        Assert.DoesNotThrow(() => cts.Cancel());
    }
    [Test]
    public async Task OnCancel_Executed_ExecptionThrown_Append()
    {
        // Arrange
        var labs = new[] { new Lab<Model>(), new Lab<Model>() };
        SetUp(_ => labs, false, true);

        try
        {
            await Execute();
        }
        catch { }


        // Act & Assert
        Assert.DoesNotThrow(() => cts.Cancel());
    }
    [TestCase(true), TestCase(false)]
    public async Task OnCancel_Executed_NullTest(bool withRootNode)
    {
        // Arrange
        SetUp(null, withRootNode);
        await Execute();


        // Act & Assert
        Assert.DoesNotThrow(() => cts.Cancel());
    }
    #endregion
}
