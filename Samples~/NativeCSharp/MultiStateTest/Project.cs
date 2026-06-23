using System;
using System.Collections.Generic;
using UniTest.Samples.NativeCSharp;
using UniTest;

namespace UniTest_Test.MultiState
{
    public partial class Project : Project<Model>
    {
        readonly int postExecutionCount = 2;

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


        void Check(Model model, SubjectMetadata _ = default)
        {
            SampleAssert.IsNotNull(model, "Kickboard is Null");

            SampleAssert.AreEqual(model.isDisposed, model.Kickboard.IsDisposed, "Dispose state mismatched");
            SampleAssert.AreSame(model.rider, model.Kickboard.Rider, "Rider mismatched");
            SampleAssert.AreEqual(model.charging, model.Kickboard.Charging, "Charging State Mismatched");
            SampleAssert.AreEqual(model.battery, model.Kickboard.Battery, "Battery Mismatched");

            if (model.isDisposed) return;

            SampleAssert.AreEqual(model.battery > 10, model.Kickboard.Available, "Available State Mismatched");
        }


        public override IEnumerable<ILab<Model>> CreateLabs(Model model)
        {
            if (model.Subject == null)
            {
                foreach (var lab in CreateLabs_Charge(model, new(MainTestCase.Create)))
                    yield return lab;

                yield break;
            }

            foreach (MainTestCase testCase in Enum.GetValues(typeof(MainTestCase)))
            {
                if (testCase == MainTestCase.Create)
                    continue;

                foreach (var lab in CreateLabs_Charge(model, new(testCase)))
                    yield return lab;
            }
        }
    } 
}
