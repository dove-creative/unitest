using System;
using System.Collections.Generic;
using BlackThunder.UniTest;
using BlackThunder.UniTest.Samples;

namespace BlackThunder.UniTest.Samples.Scenarios.MultiState
{
    public partial class Project : Project<Model>
    {
        private readonly int _postExecutionCount = 2;

        public enum MainTestCase
        {
            Create,
            Mount,
            Ride,
            Dismount,
            Charge,
            DoCharge,
            StopCharging,
            Dispose,
        }


        private void Check(Model model, SubjectMetadata _ = default)
        {
            SampleAssert.IsNotNull(model, "Kickboard is Null");

            SampleAssert.AreEqual(model.IsDisposed, model.Kickboard.IsDisposed, "Dispose state mismatched");
            SampleAssert.AreSame(model.Rider, model.Kickboard.Rider, "Rider mismatched");
            SampleAssert.AreEqual(model.Charging, model.Kickboard.Charging, "Charging State Mismatched");
            SampleAssert.AreEqual(model.Battery, model.Kickboard.Battery, "Battery Mismatched");

            if (model.IsDisposed) return;

            SampleAssert.AreEqual(model.Battery > 10, model.Kickboard.Available, "Available State Mismatched");
        }


        public override IEnumerable<ILab<Model>> CreateLabs(Model model)
        {
            if (model.Subject == null)
            {
                foreach (var lab in CreateChargeLabs(model, new(MainTestCase.Create)))
                    yield return lab;

                yield break;
            }

            foreach (MainTestCase testCase in Enum.GetValues(typeof(MainTestCase)))
            {
                if (testCase == MainTestCase.Create)
                    continue;

                foreach (var lab in CreateChargeLabs(model, new(testCase)))
                    yield return lab;
            }
        }
    }
}
