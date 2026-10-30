using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackThunder.UniTest;
using NUnit.Framework;

namespace BlackThunder.UniTest.Tests.Unit
{
    public class NodeTest
    {
        // Internal
        private Node<Model> _node;
        private CancellationTokenSource _cts;
        private const int Delay = 100;

        private Lab<Model> _lab1;
        private Lab<Model> _lab2;
        private Lab<Model> _lab3;

        private bool _lab1Success;
        private bool _lab2Success;
        private bool _lab3Success;

        private bool _lab1Executed;
        private bool _lab2Executed;
        private bool _lab3Executed;


        // Content --------------------
        // Fixture
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
            Assert.That(node.Id, Is.SameAs(lab?.Id ?? "root"));
            Assert.That(node.Model, Is.Not.Null);

            if (exception != null)
                Assert.That(node.Exception, Is.AssignableTo(exception));
            else
                Assert.That(node.Exception, Is.Null);

            Assert.That(node.Continuable, Is.EqualTo(continuable));
        }

        // State Transition
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

        // Execute
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

        // Set External Exception
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

        // Append
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

        // Detach And Restore
        [Test]
        public void DetachAndRestore()
        {
            // Arrange
            _node = new Node<Model>(null);
            _lab1Success = false;

            var after = _node.Append(_lab1, _cts.Token);
            after.Execute();

            var originalReport = after.GetReport().ToJson();

            // Act
            var restored = after.DetachAndRestore(_cts.Token);

            // Assert
            Assert.That(after.GetReport().ToJson(), Is.EqualTo(originalReport));
            Assert.That(after.Status, Is.EqualTo(Node<Model>.NodeStatus.Failure));

            Assert.That(restored.Before, Is.Null);
            Assert.That(_node.Afters, Is.EqualTo(new[] { after }));
            Assert.That(restored.Afters, Is.Empty);

            var report = restored.GetReport();
            Assert.That(report.Nodes.Count, Is.EqualTo(2));
            Assert.That(report.Nodes[0].LabId, Is.Null);
            Assert.That(report.Nodes[0].ParentId, Is.Null);
            Assert.That(report.Nodes[1].LabId, Is.EqualTo(after.Id));
            Assert.That(report.Nodes[1].ParentId, Is.EqualTo(0));
            Assert.That(report.Nodes[1].Status, Is.EqualTo("Ready"));
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

            Assert.That(_node.GetReport().Nodes.Count, Is.EqualTo(2));
            Assert.That(after.GetReport().Nodes.Last().Status, Is.EqualTo("Failure"));
            Assert.That(restored.GetReport().Nodes.Last().Status, Is.EqualTo("Success"));

            Assert.That(_node.AllSucceed(out var cancelled), Is.False);
            Assert.That(cancelled, Is.False);

            var failedReport = _node.GetFailedReports();
            Assert.That(failedReport.Nodes.Count, Is.EqualTo(2));
            Assert.That(failedReport.ToJson(), Is.EqualTo(after.GetReport().ToJson()));
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

            var report = restored.GetReport();
            Assert.That(report.Nodes.Select(n => n.LabId), Is.EqualTo(new[] { null, before.Id, after.Id }));
            Assert.That(report.Nodes.Select(n => n.ParentId), Is.EqualTo(new int?[] { null, 0, 1 }));

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

            var report = restoredAgain.GetReport();
            Assert.That(report.Nodes.Count, Is.EqualTo(2));
            Assert.That(report.Nodes[0].LabId, Is.Null);
            Assert.That(report.Nodes[1].ParentId, Is.EqualTo(0));
            Assert.That(report.Nodes[1].Status, Is.EqualTo("Ready"));
        }
    }
}
