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
    object _testDesigner;
    int _delay = 100;

    Lab<Model> _lab;
    Node<Model> _node;

    Func<Model, IEnumerable<ILab<Model>>> _createLabs;
    ProjectMock _project;

    ICollection _completedDesigners;
    Queue<Node<Model>> _preparedNodes;
    CancellationTokenSource _cts;


    void SetUp(Func<Model, IEnumerable<ILab<Model>>> createLabs, bool withRootNode, bool secondFailure = false)
    {
        _project = new ProjectMock(createLabs);

        _completedDesigners = (ICollection)typeof(Project<Model>)
            .GetField("_completedDesigners", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(_project);

        _preparedNodes = (Queue<Node<Model>>)typeof(Project<Model>)
            .GetField("_preparedNodes", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(_project);

        _cts = new();
        int executionCount = 0;

        if (withRootNode)
            _lab = null;
        else
            _lab = !secondFailure
                ? new Lab<Model>()
                : new Lab<Model>(actor: (_, _) =>
                {
                    if (executionCount++ >= 1)
                        throw new ProbeException();
                });

        _node = new Node<Model>(_lab);

        if (!withRootNode) _node.Execute();

        var ctorInfo = typeof(Project<Model>)
            .GetNestedType("TestDesigner", BindingFlags.NonPublic)
            .MakeGenericType(typeof(Model))
            .GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(Project<Model>), typeof(Node<Model>) },
                modifiers: null
            );

        _testDesigner = ctorInfo.Invoke(new object[] { _project, _node });
    }

    [TearDown]
    public void TearDown()
    {
        _project = null;
        _testDesigner = null;
        _node = null;
        _lab = null;

        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        _completedDesigners = null;
        _preparedNodes = null;
    }

    public void CheckNode(Node<Model> node, Node<Model>.NodeStatus status, int depth,
        Node<Model> before, IEnumerable<Node<Model>> afters, Type expectedExceptionType, Lab<Model> lab)
    {
        Assert.That(node.Status, Is.EqualTo(status), node.Exception?.Message ?? "No exception");
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
        var method = _testDesigner.GetType()
            .GetMethod(
                "Execute",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(CancellationToken) },
                modifiers: null
            );

        await (Task)method.Invoke(_testDesigner, new object[] { _cts.Token });
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
        Assert.That(_completedDesigners, Is.EqualTo(new[] { _testDesigner }));
        Assert.That(_preparedNodes.Select(n => n.Lab), Is.EqualTo(labs));

        CheckNode(_node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, _preparedNodes, null, _lab);

        int i = 0;
        foreach (var preparedNode in _preparedNodes)
            CheckNode(preparedNode, Node<Model>.NodeStatus.Ready, 1, _node, null, null, labs[i++]);
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
        Assert.That(_completedDesigners, Is.EqualTo(new[] { _testDesigner }));
        Assert.That(_preparedNodes, Is.Empty);

        CheckNode(_node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, _lab);
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
        _cts.Cancel();

        // Assert
        Assert.That(_completedDesigners, Is.Empty);
        Assert.That(_preparedNodes, Is.Empty);

        CheckNode(_node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, _lab);
    }
    [TestCase(true), TestCase(false)]
    public void OnCancel_Idle_ExecptionThrown_CreateLabs(bool withRootNode)
    {
        // Arrange
        SetUp(_ => throw new ProbeException(), withRootNode);

        // Act
        _cts.Cancel();

        // Assert
        Assert.That(_completedDesigners, Is.Empty);
        Assert.That(_preparedNodes, Is.Empty);

        CheckNode(_node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, _lab);
    }
    [Test]
    public void OnCancel_Idle_ExecptionThrown_CreateLabs_Append()
    {
        // Arrange
        var labs = new[] { new Lab<Model>(), new Lab<Model>() };
        SetUp(_ => labs, false, true);

        // Act
        _cts.Cancel();

        // Assert
        Assert.That(_completedDesigners, Is.Empty);
        Assert.That(_preparedNodes, Is.Empty);
        
        CheckNode(_node, Node<Model>.NodeStatus.Success, 0, null, null, null, _lab);
    }
    [TestCase(true), TestCase(false)]
    public void OnCancel_Idle_NoTests(bool withRootNode)
    {
        // Arrange
        SetUp(null, withRootNode);

        // Act
        _cts.Cancel();

        // Assert
        Assert.That(_completedDesigners, Is.Empty);
        Assert.That(_preparedNodes, Is.Empty);

        CheckNode(_node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, _lab);
    }
    #endregion



    #region Cancelled - Execute
    [TestCase(true), TestCase(false)]
    public async Task Execute_Cancelled_Normal(bool withRootNode)
    {
        // Arrange
        var labs = new[] { new Lab<Model>(), new Lab<Model>() };
        SetUp(_ => labs, withRootNode);
        _cts.Cancel();

        // Act
        await Execute();

        // Assert
        Assert.That(_completedDesigners, Is.EqualTo(new[] { _testDesigner }));
        Assert.That(_preparedNodes, Is.Empty);
        CheckNode(_node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, _lab);
    }
    [TestCase(true), TestCase(false)]
    public async Task Execute_Cancelled_ExecptionThrown_CreateLab(bool withRootNode)
    {
        // Arrange
        SetUp(_ => throw new ProbeException(), withRootNode);
        _cts.Cancel();

        // Act
        await Execute();

        // Assert
        Assert.That(_completedDesigners, Is.EqualTo(new[] { _testDesigner }));
        Assert.That(_preparedNodes, Is.Empty);
        CheckNode(_node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, _lab);
    }
    [Test]
    public async Task Execute_Cancelled_ExecptionThrown_Append()
    {
        // Arrange
        var labs = new[] { new Lab<Model>(), new Lab<Model>() };
        SetUp(_ => labs, false, true);
        _cts.Cancel();

        // Act
        await Execute();

        // Assert
        Assert.That(_completedDesigners, Is.EqualTo(new[] { _testDesigner }));
        Assert.That(_preparedNodes, Is.Empty);
        CheckNode(_node, Node<Model>.NodeStatus.Success, 0, null, _preparedNodes, null, _lab);
    }
    [TestCase(true), TestCase(false)]
    public async Task Execute_Cancelled_NoTests(bool withRootNode)
    {
        // Arrange
        SetUp(null, withRootNode);
        _cts.Cancel();

        // Act
        await Execute();

        // Assert
        Assert.That(_completedDesigners, Is.EqualTo(new[] { _testDesigner }));
        Assert.That(_preparedNodes, Is.Empty);
        CheckNode(_node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, _lab);
    }
    #endregion

 

    #region Executing - Execute
    [TestCase(true), TestCase(false)]
    public void Execute_Executing_Normal(bool withRootNode)
    {
        // Arrange
        var labs = new[] { new Lab<Model>(), new Lab<Model>() };
        _createLabs = _ =>
        {
            Thread.Sleep(_delay);
            return labs;
        };
        
        SetUp(_createLabs, withRootNode);


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
        _createLabs = _ =>
        {
            Thread.Sleep(_delay);
            return labs;
        };

        SetUp(_createLabs, withRootNode);
        var task = Execute();


        // Act
        _cts.Cancel();
        await task;


        // Assert
        Assert.That(_completedDesigners, Is.EqualTo(new[] { _testDesigner }));
        Assert.That(_preparedNodes, Is.Empty);
        CheckNode(_node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, _lab);
    }
    [TestCase(true), TestCase(false)]
    public async Task OnCancel_Executing_NoTest(bool withRootNode)
    {
        // Arrange
        _createLabs = _ =>
        {
            Thread.Sleep(_delay);
            return null;
        };

        SetUp(_createLabs, withRootNode);
        var task = Execute();

        // Act
        _cts.Cancel();
        await task;


        // Assert
        Assert.That(_completedDesigners, Is.EqualTo(new[] { _testDesigner }));
        Assert.That(_preparedNodes, Is.Empty);
        CheckNode(_node, withRootNode ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Success,
            0, null, null, null, _lab);
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
        Assert.DoesNotThrow(() => _cts.Cancel());
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
        Assert.DoesNotThrow(() => _cts.Cancel());
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
        Assert.DoesNotThrow(() => _cts.Cancel());
    }
    [TestCase(true), TestCase(false)]
    public async Task OnCancel_Executed_NullTest(bool withRootNode)
    {
        // Arrange
        SetUp(null, withRootNode);
        await Execute();


        // Act & Assert
        Assert.DoesNotThrow(() => _cts.Cancel());
    }
    #endregion
}
