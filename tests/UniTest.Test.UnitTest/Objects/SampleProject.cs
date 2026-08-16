using System.Collections.Generic;
using UniTest;

public class SampleProject : Project<Model>
{
    public int FailureAt = -1;

    public override IEnumerable<ILab<Model>> CreateLabs(Model model)
    {
        if (FailureAt >= 0)
        {
            yield return new Lab<Model>("PF")
            {
                Actor = (m, _) =>
                {
                    if (m.ExecutionCount >= FailureAt)
                        throw new ProbeException($"Failure by FailureAt ({FailureAt})");
                }
            };

            yield break;
        }

        yield return new Lab<Model>("pass");
        yield break;
    }
}
