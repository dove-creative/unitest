using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UniTest;

public class TestExecutorTest
{
    object testExecutor;
    int DELAY = 100;

    Lab<Model> lab;
    Node<Model> node;

    Func<Model, IEnumerable<ILab<Model>>> createLabs;
    ProjectMock project;

    ICollection completedExecutors;
    Queue<Node<Model>> idleNodes;
    CancellationTokenSource cts;


    void SetUp(Lab<Model> lab, int targetDepth = int.MaxValue)
    {
        project = new ProjectMock().SetTargetDepth(targetDepth);

        completedExecutors = (ICollection)typeof(Project<Model>)
            .GetField("completedExecutors", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(project);

        idleNodes = (Queue<Node<Model>>)typeof(Project<Model>)
            .GetField("idleNodes", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(project);

        cts = new();

        this.lab = lab;
        node = new Node<Model>(lab);

        var ctorInfo = typeof(Project<Model>)
            .GetNestedType("TestExecutor", BindingFlags.NonPublic)
            .MakeGenericType(typeof(Model))
            .GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(Project<Model>), typeof(Node<Model>) },
                modifiers: null
            );

        testExecutor = ctorInfo.Invoke(new object[] { project, node });
    }

    [TearDown]
    public void TearDown()
    {
        project = null;
        testExecutor = null;
        node = null;

        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }

        completedExecutors = null;
        idleNodes = null;
    }

    public void CheckNode(Node<Model> node, Node<Model>.NodeStatus status, int depth,
        Node<Model> before, IEnumerable<Node<Model>> afters, Type expectedExceptionType, Lab<Model> lab)
    {
        Assert.AreEqual(status, node.Status);
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
        var method = testExecutor.GetType()
            .GetMethod(
                "Execute",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(CancellationToken) },
                modifiers: null
            );

        await (Task)method.Invoke(testExecutor, new object[] { cts.Token });
    }



    #region Idle - Execute
    [Test]
    public async Task Execute_Idle_Normal()
    {
        // Arrange
        SetUp(new Lab<Model>());

        // Act
        await Execute();
        
        // Assert
        CollectionAssert.AreEqual(new[] { testExecutor }, completedExecutors);
        CollectionAssert.AreEqual(new[] { node }, idleNodes);
        CheckNode(node, Node<Model>.NodeStatus.Success, 0, null, null, null, lab);
    }
    [Test]
    public async Task Execute_Idle_ExecptionThrown()
    {
        // Arrange
        SetUp(new Lab<Model>(actor: (_, _) => throw new ProbeException()));

        // Act
        await Execute();

        // Assert
        CollectionAssert.AreEqual(new[] { testExecutor }, completedExecutors);
        CollectionAssert.IsEmpty(idleNodes);
        CheckNode(node, Node<Model>.NodeStatus.Failure, 0, null, null, typeof(ExecutionException), lab);
    }
    [Test]
    public async Task Execute_Idle_ReachedDepthLimit()
    {
        // Arrange
        SetUp(new Lab<Model>(), 0);

        // Act
        await Execute();

        // Assert
        CollectionAssert.AreEqual(new[] { testExecutor }, completedExecutors);
        CollectionAssert.IsEmpty(idleNodes);
        CheckNode(node, Node<Model>.NodeStatus.Success, 0, null, null, null, lab);
    }
    [Test]
    public void Execute_Idle_NullLab()
    {
        // Arrange
        SetUp(null);

        // Act & Assert
        Assert.ThrowsAsync<ExecutionException>(() => Execute());
    }
    #endregion

    #region Idle - On Cancel
    [Test]
    public void OnCancel_Idle_Normal()
    {
        // Arrange
        SetUp(new Lab<Model>());

        // Act
        cts.Cancel();

        // Assert
        CollectionAssert.IsEmpty(completedExecutors);
        CollectionAssert.IsEmpty(idleNodes);
        CheckNode(node, Node<Model>.NodeStatus.Ready, 0, null, null, null, lab);
    }
    [Test]
    public void OnCancel_Idle_ExecptionThrown()
    {
        // Arrange
        SetUp(new Lab<Model>(actor: (_, _) => throw new ProbeException()));

        // Act
        cts.Cancel();

        // Assert
        CollectionAssert.IsEmpty(completedExecutors);
        CollectionAssert.IsEmpty(idleNodes);
        CheckNode(node, Node<Model>.NodeStatus.Ready, 0, null, null, null, lab);
    }
    [Test]
    public void OnCancel_Idle_ReachedDepthLimit()
    {
        // Arrange
        SetUp(new Lab<Model>(), 0);

        // Act
        cts.Cancel();

        // Assert
        CollectionAssert.IsEmpty(completedExecutors);
        CollectionAssert.IsEmpty(idleNodes);
        CheckNode(node, Node<Model>.NodeStatus.Ready, 0, null, null, null, lab);
    }
    [Test]
    public void OnCancel_Idle_NullLab()
    {
        // Arrange
        SetUp(null);

        // Act
        cts.Cancel();

        // Assert
        CollectionAssert.IsEmpty(completedExecutors);
        CollectionAssert.IsEmpty(idleNodes);
        CheckNode(node, Node<Model>.NodeStatus.Root, 0, null, null, null, lab);
    }
    #endregion



    #region Cancelled - Execute
    [Test]
    public async Task Execute_Cancelled_Normal()
    {
        // Arrange
        SetUp(new Lab<Model>());
        cts.Cancel();

        // Act
        await Execute();

        // Assert
        CollectionAssert.AreEqual(new[] { testExecutor }, completedExecutors);
        CollectionAssert.IsEmpty(idleNodes);
        CheckNode(node, Node<Model>.NodeStatus.Cancelled, 0, null, null, typeof(OperationCanceledException), lab);
    }
    [Test]
    public async Task Execute_Cancelled_ExecptionThrown()
    {
        // Arrange
        SetUp(new Lab<Model>(actor: (_, _) => throw new ProbeException()));
        cts.Cancel();

        // Act
        await Execute();

        // Assert
        CollectionAssert.AreEqual(new[] { testExecutor }, completedExecutors);
        CollectionAssert.IsEmpty(idleNodes);
        CheckNode(node, Node<Model>.NodeStatus.Cancelled, 0, null, null, typeof(OperationCanceledException), lab);
    }
    [Test]
    public async Task Execute_Cancelled_ReachedDepthLimit()
    {
        // Arrange
        SetUp(new Lab<Model>(), 0);
        cts.Cancel();

        // Act
        await Execute();

        // Assert
        CollectionAssert.AreEqual(new[] { testExecutor }, completedExecutors);
        CollectionAssert.IsEmpty(idleNodes);
        CheckNode(node, Node<Model>.NodeStatus.Cancelled, 0, null, null, typeof(OperationCanceledException), lab);
    }
    [Test]
    public async Task Execute_Cancelled_NullLab()
    {
        // Arrange
        SetUp(null);
        cts.Cancel();

        // Act
        await Execute();

        // Assert
        CollectionAssert.AreEqual(new[] { testExecutor }, completedExecutors);
        CollectionAssert.IsEmpty(idleNodes);
        CheckNode(node, Node<Model>.NodeStatus.Cancelled, 0, null, null, typeof(OperationCanceledException), lab);
    }
    #endregion



    #region Executing - Execute
    [Test]
    public void Execute_Executing_Normal()
    {
        // Arrange
        SetUp(new Lab<Model>(actor: (_, _) => Thread.Sleep(DELAY)));
        _ = Execute();

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => Execute());
    }
    [Test]
    public void Executed_Executing_NullLab()
    {
        // Arrange
        SetUp(null, 0);
        _ = Execute();

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => Execute());
    }
    #endregion

    #region Executing - On Cancel
    [Test]
    public async Task OnCancel_Executing_Normal()
    {
        // Arrange
        SetUp(new Lab<Model>(actor: (_, _) => Thread.Sleep(DELAY)));
        var task = Execute();

        // Act
        cts.Cancel();
        await task;

        // Assert
        CollectionAssert.AreEqual(new[] { testExecutor }, completedExecutors);
        CollectionAssert.IsEmpty(idleNodes);
        CheckNode(node, Node<Model>.NodeStatus.Cancelled, 0, null, null, typeof(OperationCanceledException), lab);
    }
    [Test]
    public async Task OnCancel_Executing_NullLab()
    {
        // Arrange
        SetUp(null);
        var task = Execute();

        // Act
        cts.Cancel();
        await task;

        // Assert
        CollectionAssert.AreEqual(new[] { testExecutor }, completedExecutors);
        CollectionAssert.IsEmpty(idleNodes);
        CheckNode(node, Node<Model>.NodeStatus.Cancelled, 0, null, null, typeof(OperationCanceledException), lab);
    }
    #endregion



    #region Executed - Execute
    [Test]
    public async Task Execute_Executed_Normal()
    {
        // Arrange
        SetUp(new Lab<Model>());
        await Execute();

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => Execute());
    }
    [Test]
    public async Task Execute_Executed_ExecptionThrown()
    {
        // Arrange
        SetUp(new Lab<Model>(actor: (_, _) => throw new ProbeException()));
        await Execute();

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => Execute());
    }
    [Test]
    public async Task Execute_Executed_ReachedDepthLimit()
    {
        // Arrange
        SetUp(new Lab<Model>(), 0);
        await Execute();

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => Execute());
    }
    [Test]
    public async Task Execute_Executed_NullLab()
    {
        // Arrange
        SetUp(null, 0);

        try
        {
            await Execute();
        }
        catch { }


        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => Execute());
    }
    #endregion

    #region Executed - On Cancel
    [Test]
    public async Task OnCancel_Executed_Normal()
    {
        // Arrange
        SetUp(new Lab<Model>());
        await Execute();

        // Act & Assert
        Assert.DoesNotThrow(() => cts.Cancel());
    }
    [Test]
    public async Task OnCancel_Executed_ExecptionThrown()
    {
        // Arrange
        SetUp(new Lab<Model>(actor: (_, _) => throw new ProbeException()));
        await Execute();

        // Act & Assert
        Assert.DoesNotThrow(() => cts.Cancel());
    }
    [Test]
    public async Task OnCancel_Executed_ReachedDepthLimit()
    {
        // Arrange
        SetUp(new Lab<Model>(), 0);
        await Execute();

        // Act & Assert
        Assert.DoesNotThrow(() => cts.Cancel());
    }
    [Test]
    public async Task OnCancel_Executed_NullLab()
    {
        // Arrange
        SetUp(null, 0);

        try
        {
            await Execute();
        }
        catch { }


        // Act & Assert
        Assert.DoesNotThrow(() => cts.Cancel());
    }
    #endregion
}
