using System;
using System.Collections.Generic;
using NUnit.Framework;
using UniTest;

namespace UniTest_Test.MultiState
{
    public partial class Project : Project<Model>
    {
        readonly int _postExecutionCount = 2;

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
            Assert.That(model, Is.Not.Null, "Kickboard is Null");

            Assert.That(model.Kickboard.IsDisposed, Is.EqualTo(model.isDisposed), "Dispose state mismatched");
            Assert.That(model.Kickboard.Rider, Is.SameAs(model.rider), "Rider mismatched");
            Assert.That(model.Kickboard.Charging, Is.EqualTo(model.charging), "Charging State Mismatched");
            Assert.That(model.Kickboard.Battery, Is.EqualTo(model.battery), "Battery Mismatched");

            if (model.isDisposed) return;

            Assert.That(model.Kickboard.Available, Is.EqualTo(model.battery > 10), "Available State Mismatched");
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
