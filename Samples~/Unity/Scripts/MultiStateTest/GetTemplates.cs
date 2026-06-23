using System.Collections.Generic;
using UniTest;
using UniTest_Test.Subject;

namespace UniTest_Test.MultiState
{
    public partial class Project
    {
        IEnumerable<Lab<Model>> GetTemplates(TestCase testCase)
        {
            if (testCase.Confineable(0, MainTestCase.Create))
            {
                if (testCase.Confineable(1, 100))
                    yield return new()
                    {
                        ID = "Ignite_100",
                        SetMetadata = _ => 100,
                        Actor = (m, _) => m.Subject = new MultiStatedKickboard(100)
                    };
                if (testCase.Confineable(1, "Default") || testCase.Confineable(1, 50))
                    yield return new()
                    {
                        ID = "Ignite_Default",
                        SetMetadata = _ => 50,
                        Actor = (m, _) => m.Subject = new MultiStatedKickboard()
                    };
                if (testCase.Confineable(1, 0))
                    yield return new()
                    {
                        ID = "Ignite_0",
                        SetMetadata = _ => 0,
                        Actor = (m, _) => m.Subject = new MultiStatedKickboard(0)
                    };
            }
            if (testCase.Confineable(0, MainTestCase.Mount))
            {
                if (testCase.Confineable(1, "Licensed"))
                    yield return new()
                    {
                        ID = "Mount_Licensed",
                        SetMetadata = _ => new Rider(true),
                        Actor = (m, md) => m.Kickboard.Mount((Rider)md.Metadata)
                    };
                if (testCase.Confineable(1, "Same"))
                    yield return new()
                    {
                        ID = "Mount_Same",
                        SetMetadata = m => m.rider,
                        Actor = (m, _) => m.Kickboard.Mount(m.rider)
                    };
                if (testCase.Confineable(1, "NotLicensed"))
                    yield return new()
                    {
                        ID = "Mount_NotLicensed",
                        SetMetadata = _ => new Rider(false),
                        Actor = (m, md) => m.Kickboard.Mount((Rider)md.Metadata)
                    };
                if (testCase.Confineable(1, "Null"))
                    yield return new()
                    {
                        ID = "Mount_Null",
                        SetMetadata = _ => null,
                        Actor = (m, _) => m.Kickboard.Mount(null)
                    };
                if (testCase.Confineable(1, "Targeted"))
                    yield return new()
                    {
                        ID = "Mount_Targeted",
                        SetMetadata = m => m.TargetedRider,
                        Actor = (m, _) => m.Kickboard.Mount(m.TargetedRider)
                    };
            }
            if (testCase.Confineable(0, MainTestCase.Ride))
            {
                yield return new("Ride", actor: (m, _) => m.Kickboard.Ride());
            }
            if (testCase.Confineable(0, MainTestCase.Dismount))
            {
                yield return new("Dismount", actor: (m, _) => m.Kickboard.Dismount());
            }
            if (testCase.Confineable(0, MainTestCase.Charge))
            {
                yield return new()
                {
                    ID = "Charge",
                    SetMetadata = m => m.TargetCharger,
                    Actor = (m, _) => m.Kickboard.Charge(m.TargetCharger)
                };
            }
            if (testCase.Confineable(0, MainTestCase.DoCharge))
            {
                yield return new("DoCharge", actor: (m, _) => m.TargetCharger.DoCharge());
            }
            if (testCase.Confineable(0, MainTestCase.StopCharging))
            {
                yield return new("StopCharging", actor: (m, _) => m.Kickboard.StopCharging());
            }
            if (testCase.Confineable(0, MainTestCase.Dispose))
            {
                yield return new("Dispose", actor: (m, _) => m.Kickboard.Dispose());
            }
        }
    }
}
