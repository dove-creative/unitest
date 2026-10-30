using System;
using System.Collections.Generic;
using System.Linq;
using BlackThunder.UniTest;
using BlackThunder.UniTest.Samples.Subjects;

namespace BlackThunder.UniTest.Samples.Scenarios.MultiState
{
    public partial class Project : Project<Model>
    {
        private IEnumerable<ILab<Model>> CreateChargeLabs(Model model, TestCase testCase)
        {
            if (testCase.Count == 0)
                throw new TestCaseAbsentException(nameof(testCase));


            var labs = new List<ILab<Model>>();
            TestCase tc;

            if (testCase.Confineable(0, out tc, MainTestCase.Mount))
                Mount();

            if (testCase.Confineable(0, out tc, MainTestCase.Ride))
                Ride();

            if (testCase.Confineable(0, out tc, MainTestCase.Charge))
                Charge();

            if (testCase.Confineable(0, out tc, MainTestCase.DoCharge))
                DoCharge();

            if (testCase.Confineable(0, out tc, MainTestCase.StopCharging))
                StopCharging();

            if (testCase.Confineable(0, out tc, MainTestCase.Dispose))
                Dispose();

            if (testCase.ConfineableExcept(0, out tc, MainTestCase.Mount, MainTestCase.Ride, MainTestCase.Charge, MainTestCase.DoCharge, MainTestCase.StopCharging, MainTestCase.Dispose))
                labs.AddRange(new Lab<Model>("pass").Extend(CreateBatteryLabs(model, tc)));

            return labs;


            void Mount()
            {
                if (model.Rider == null)
                    tc = tc.Exclude(1, "Same");

                TestCase _tc;

                if (model.IsDisposed)
                {
                    if (tc.ConfineableExcept(1, out _tc, "Null"))
                    {
                        labs.AddRange(new Lab<Model>()
                        {
                            Id = "disposed_notNull",
                            Arranger = (_, md) =>
                            {
                                md.ExpectedExceptionType = typeof(ObjectDisposedException);
                                md.RemainingExecutionCount = _postExecutionCount;
                            }
                        }.Merge(GetTemplates(_tc)));
                    }

                    if (tc.Confineable(1, out _tc, "Null"))
                    {
                        labs.AddRange(new Lab<Model>("disposed", asserter: Check)
                            .Extend(CreateBatteryLabs(model, _tc)));
                    }
                }
                else if (model.Charging)
                {
                    if (!model.Kickboard.Available)
                    {
                        labs.AddRange(new Lab<Model>
                        {
                            Id = "charging_discharged",
                            Asserter = Check
                        }.Merge(GetTemplates(tc)));
                        return;
                    }

                    labs.AddRange(new Lab<Model>
                    {
                        Id = "charging_available",
                        // Any non-null mount request stops charging before rider validation,
                        // including requests that are expected to throw for an unlicensed rider.
                        Arranger = (m, _) => m.Charging = !m.MetadataGroup.Values.Any(metadata => metadata.Metadata is Rider),
                        Asserter = Check
                    }.Extend(CreateBatteryLabs(model, tc)));
                }
                else
                {
                    labs.AddRange(new Lab<Model>("notCharging")
                        .Extend(CreateBatteryLabs(model, tc)));
                }
            }
            void Ride()
            {
                if (model.IsDisposed)
                {
                    ThrowIfDisposed(tc);
                }
                else if (model.Charging)
                {
                    labs.AddRange(new Lab<Model>("charging", expectedExceptionType: typeof(InvalidOperationException), toUncontinuable: true)
                        .Merge(GetTemplates(tc)));
                }
                else
                {
                    labs.AddRange(new Lab<Model>("notCharging")
                        .Extend(CreateBatteryLabs(model, tc)));
                }
            }

            void Charge()
            {
                if (model.IsDisposed)
                {
                    ThrowIfDisposed(tc);
                }
                else if (model.Charging)
                {
                    labs.AddRange(new Lab<Model>("charging", arranger: Check)
                        .Merge(GetTemplates(tc)));
                }
                else
                {
                    labs.AddRange(new Lab<Model>()
                    {
                        Id = "notCharging",
                        Arranger = (m, _) => m.Charging = true,
                        Asserter = Check
                    }.Merge(GetTemplates(tc)));
                }
            }
            void DoCharge()
            {
                if (model.Charging)
                {
                    labs.AddRange(new Lab<Model>()
                    {
                        Id = "charging",
                        Arranger = (m, _) => m.Battery = Math.Min(model.Battery + 10, 100),
                        Asserter = Check
                    }.Merge(GetTemplates(tc)));
                }
            }
            void StopCharging()
            {
                if (model.Charging)
                {
                    labs.AddRange(new Lab<Model>()
                    {
                        Id = "charging",
                        Arranger = (m, _) => m.Charging = false,
                        Asserter = Check
                    }.Merge(GetTemplates(tc)));
                }
                else
                {
                    labs.AddRange(new Lab<Model>("notCharging")
                        .Merge(GetTemplates(tc)));
                }
            }

            void Dispose()
            {
                if (model.Charging)
                {
                    labs.AddRange(new Lab<Model>()
                    {
                        Id = "charging",
                        Arranger = (m, _) => m.Charging = false,
                        Asserter = Check
                    }.Extend(CreateBatteryLabs(model, tc)));
                }
                else
                {
                    labs.AddRange(new Lab<Model>("notCharging")
                        .Extend(CreateBatteryLabs(model, tc)));
                }
            }

            void ThrowIfDisposed(TestCase tc)
            {
                labs.AddRange(new Lab<Model>(expectedExceptionType: typeof(ObjectDisposedException))
                {
                    Id = "disposed",
                }.Merge(GetTemplates(tc)));
            }
        }
    }
}
