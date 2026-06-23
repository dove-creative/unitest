using System;
using System.Collections.Generic;
using System.Reflection;
using UniTest;

public class ProjectMock : Project<Model>
{
    public Func<Model, IEnumerable<ILab<Model>>> createLabs;

    public ProjectMock(Func<Model, IEnumerable<ILab<Model>>> createLabs = null) => this.createLabs = createLabs;
    public ProjectMock SetTargetDepth(int depth)
    {
        typeof(Project<Model>)
            .GetField("targetDepth", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(this, depth);

        return this;
    }

    public override IEnumerable<ILab<Model>> CreateLabs(Model model) => createLabs?.Invoke(model);
}
