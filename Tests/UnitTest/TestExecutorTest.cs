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
    object _testExecutor;
    int _delay = 100;

    Lab<Model> _lab;
    Node<Model> _node;

    Func<Model, IEnumerable<ILab<Model>>> _createLabs;
    ProjectMock _project;

    ICollection _completedExecutors;
    Queue<Node<Model>> _idleNodes;
    CancellationTokenSource _cts;


    void SetUp(Lab<Model> lab, int targetDepth = int.MaxValue)
    {
        _project = new ProjectMock().SetTargetDepth(targetDepth);

        _completedExecutors = (ICollection)typeof(Project<Model>)
            .GetField("_completedExecutors", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(_project);

        _idleNodes = (Queue<Node<Model>>)typeof(Project<Model>)
            .GetField("_idleNodes", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(_project);

        _cts = new();

        this._lab = lab;
        _node = new Node<Model>(lab);

        var ctorInfo = typeof(Project<Model>)
            .GetNestedType("TestExecutor", BindingFlags.NonPublic)
            .MakeGenericType(typeof(Model))
            .GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(Project<Model>), typeof(Node<Model>) },
                modifiers: null
            );

        _testExecutor = ctorInfo.Invoke(new object[] { _project, _node });
    }

    [TearDown]
    public void TearDown()
    {
        _project = null;
        _testExecutor = null;
        _node = null;

        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        _completedExecutors = null;
        _idleNodes = null;
    }

    public void CheckNode(Node<Model> node, Node<Model>.NodeStatus status, int depth,
        Node<Model> before, IEnumerable<Node<Model>> afters, Type expectedExceptionType, Lab<Model> lab)
    {
        Assert.That(node.Status, Is.EqualTo(status));
        Assert.That(node.Depth, Is.EqualTo(depth));
        Assert.That(node.Before, Is.SameAs(before));
        Assert.That(node.Afters, Is.EquivalentTo(afters ?? Array.Empty<Node<Model>>()));

        if (expectedExceptionType != null)
            Assert.That(node.Exception, Is.AssignableTo(expectedExceptionType));
        else
            Assert.That(node.Exception, Is.Null);

        Assert.That(node.Lab, Is.SameAs(lab));
        Assert.That(node.ID, Is.SameAs(lab?.ID ?? "root"));
        Assert.That(node.Model, Is.Not.Null);
    }

    async Task Execute()
    {
        var method = _testExecutor.GetType()
            .GetMethod(
                "Execute",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(CancellationToken) },
                modifiers: null
            );

        await (Task)method.Invoke(_testExecutor, new object[] { _cts.Token });
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
        Assert.That(_completedExecutors, Is.EqualTo(new[] { _testExecutor }));
        Assert.That(_idleNodes, Is.EqualTo(new[] { _node }));
        CheckNode(_node, Node<Model>.NodeStatus.Success, 0, null, null, null, _lab);
    }
    [Test]
    public async Task Execute_Idle_ExecptionThrown()
    {
        // Arrange
        SetUp(new Lab<Model>(actor: (_, _) => throw new ProbeException()));

        // Act
        await Execute();

        // Assert
        Assert.That(_completedExecutors, Is.EqualTo(new[] { _testExecutor }));
        Assert.That(_idleNodes, Is.Empty);
        CheckNode(_node, Node<Model>.NodeStatus.Failure, 0, null, null, typeof(ExecutionException), _lab);
    }
    [Test]
    public async Task Execute_Idle_ReachedDepthLimit()
    {
        // Arrange
        SetUp(new Lab<Model>(), 0);

        // Act
        await Execute();

        // Assert
        Assert.That(_completedExecutors, Is.EqualTo(new[] { _testExecutor }));
        Assert.That(_idleNodes, Is.Empty);
        CheckNode(_node, Node<Model>.NodeStatus.Success, 0, null, null, null, _lab);
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
        _cts.Cancel();

        // Assert
        Assert.That(_completedExecutors, Is.Empty);
        Assert.That(_idleNodes, Is.Empty);
        CheckNode(_node, Node<Model>.NodeStatus.Ready, 0, null, null, null, _lab);
    }
    [Test]
    public void OnCancel_Idle_ExecptionThrown()
    {
        // Arrange
        SetUp(new Lab<Model>(actor: (_, _) => throw new ProbeException()));

        // Act
        _cts.Cancel();

        // Assert
        Assert.That(_completedExecutors, Is.Empty);
        Assert.That(_idleNodes, Is.Empty);
        CheckNode(_node, Node<Model>.NodeStatus.Ready, 0, null, null, null, _lab);
    }
    [Test]
    public void OnCancel_Idle_ReachedDepthLimit()
    {
        // Arrange
        SetUp(new Lab<Model>(), 0);

        // Act
        _cts.Cancel();

        // Assert
        Assert.That(_completedExecutors, Is.Empty);
        Assert.That(_idleNodes, Is.Empty);
        CheckNode(_node, Node<Model>.NodeStatus.Ready, 0, null, null, null, _lab);
    }
    [Test]
    public void OnCancel_Idle_NullLab()
    {
        // Arrange
        SetUp(null);

        // Act
        _cts.Cancel();

        // Assert
        Assert.That(_completedExecutors, Is.Empty);
        Assert.That(_idleNodes, Is.Empty);
        CheckNode(_node, Node<Model>.NodeStatus.Root, 0, null, null, null, _lab);
    }
    #endregion



    #region Cancelled - Execute
    [Test]
    public async Task Execute_Cancelled_Normal()
    {
        // Arrange
        SetUp(new Lab<Model>());
        _cts.Cancel();

        // Act
        await Execute();

        // Assert
        Assert.That(_completedExecutors, Is.EqualTo(new[] { _testExecutor }));
        Assert.That(_idleNodes, Is.Empty);
        CheckNode(_node, Node<Model>.NodeStatus.Cancelled, 0, null, null, typeof(OperationCanceledException), _lab);
    }
    [Test]
    public async Task Execute_Cancelled_ExecptionThrown()
    {
        // Arrange
        SetUp(new Lab<Model>(actor: (_, _) => throw new ProbeException()));
        _cts.Cancel();

        // Act
        await Execute();

        // Assert
        Assert.That(_completedExecutors, Is.EqualTo(new[] { _testExecutor }));
        Assert.That(_idleNodes, Is.Empty);
        CheckNode(_node, Node<Model>.NodeStatus.Cancelled, 0, null, null, typeof(OperationCanceledException), _lab);
    }
    [Test]
    public async Task Execute_Cancelled_ReachedDepthLimit()
    {
        // Arrange
        SetUp(new Lab<Model>(), 0);
        _cts.Cancel();

        // Act
        await Execute();

        // Assert
        Assert.That(_completedExecutors, Is.EqualTo(new[] { _testExecutor }));
        Assert.That(_idleNodes, Is.Empty);
        CheckNode(_node, Node<Model>.NodeStatus.Cancelled, 0, null, null, typeof(OperationCanceledException), _lab);
    }
    [Test]
    public async Task Execute_Cancelled_NullLab()
    {
        // Arrange
        SetUp(null);
        _cts.Cancel();

        // Act
        await Execute();

        // Assert
        Assert.That(_completedExecutors, Is.EqualTo(new[] { _testExecutor }));
        Assert.That(_idleNodes, Is.Empty);
        CheckNode(_node, Node<Model>.NodeStatus.Cancelled, 0, null, null, typeof(OperationCanceledException), _lab);
    }
    #endregion



    #region Executing - Execute
    [Test]
    public void Execute_Executing_Normal()
    {
        // Arrange
        SetUp(new Lab<Model>(actor: (_, _) => Thread.Sleep(_delay)));
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
        SetUp(new Lab<Model>(actor: (_, _) => Thread.Sleep(_delay)));
        var task = Execute();

        // Act
        _cts.Cancel();
        await task;

        // Assert
        Assert.That(_completedExecutors, Is.EqualTo(new[] { _testExecutor }));
        Assert.That(_idleNodes, Is.Empty);
        CheckNode(_node, Node<Model>.NodeStatus.Cancelled, 0, null, null, typeof(OperationCanceledException), _lab);
    }
    [Test]
    public async Task OnCancel_Executing_NullLab()
    {
        // Arrange
        SetUp(null);
        var task = Execute();

        // Act
        _cts.Cancel();
        await task;

        // Assert
        Assert.That(_completedExecutors, Is.EqualTo(new[] { _testExecutor }));
        Assert.That(_idleNodes, Is.Empty);
        CheckNode(_node, Node<Model>.NodeStatus.Cancelled, 0, null, null, typeof(OperationCanceledException), _lab);
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
        Assert.DoesNotThrow(() => _cts.Cancel());
    }
    [Test]
    public async Task OnCancel_Executed_ExecptionThrown()
    {
        // Arrange
        SetUp(new Lab<Model>(actor: (_, _) => throw new ProbeException()));
        await Execute();

        // Act & Assert
        Assert.DoesNotThrow(() => _cts.Cancel());
    }
    [Test]
    public async Task OnCancel_Executed_ReachedDepthLimit()
    {
        // Arrange
        SetUp(new Lab<Model>(), 0);
        await Execute();

        // Act & Assert
        Assert.DoesNotThrow(() => _cts.Cancel());
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
        Assert.DoesNotThrow(() => _cts.Cancel());
    }
    #endregion
}
