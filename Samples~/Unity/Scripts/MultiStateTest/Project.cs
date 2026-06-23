using System;
using System.Collections.Generic;
using NUnit.Framework;
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
            Assert.IsNotNull(model, "Kickboard is Null");

            Assert.AreEqual(model.isDisposed, model.Kickboard.IsDisposed, "Dispose state mismatched");
            Assert.AreSame(model.rider, model.Kickboard.Rider, "Rider mismatched");
            Assert.AreEqual(model.charging, model.Kickboard.Charging, "Charging State Mismatched");
            Assert.AreEqual(model.battery, model.Kickboard.Battery, "Battery Mismatched");

            if (model.isDisposed) return;

            Assert.AreEqual(model.battery > 10, model.Kickboard.Available, "Available State Mismatched");
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
