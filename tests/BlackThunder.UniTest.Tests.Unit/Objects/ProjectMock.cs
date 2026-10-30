using System;
using System.Collections.Generic;
using System.Reflection;
using BlackThunder.UniTest;

namespace BlackThunder.UniTest.Tests.Unit
{
    public class ProjectMock : Project<Model>
    {
        public Func<Model, IEnumerable<ILab<Model>>> CreateLabsHandler;

        public ProjectMock(Func<Model, IEnumerable<ILab<Model>>> createLabs = null) => this.CreateLabsHandler = createLabs;

        public ProjectMock(ExecutionMode executionMode, Func<Model, IEnumerable<ILab<Model>>> createLabs = null)
            : base(executionMode) => this.CreateLabsHandler = createLabs;

        public ProjectMock SetTargetDepth(int depth)
        {
            typeof(Project<Model>)
                .GetField("_targetDepth", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(this, depth);

            return this;
        }

        public override IEnumerable<ILab<Model>> CreateLabs(Model model) => CreateLabsHandler?.Invoke(model);
    }
}
