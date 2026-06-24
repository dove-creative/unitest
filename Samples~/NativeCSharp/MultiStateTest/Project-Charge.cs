using System;
using System.Collections.Generic;
using UniTest;

namespace UniTest_Test.MultiState
{
    public partial class Project : Project<Model>
    {
        IEnumerable<ILab<Model>> CreateLabs_Charge(Model model, TestCase testCase)
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
                labs.AddRange(new Lab<Model>("pass").Extend(CreateLabs_Battery(model, tc)));
            
            return labs;



            void Mount()
            {
                if (model.rider == null)
                    tc = tc.Exclude(1, "Same");

                TestCase _tc;

                if (model.isDisposed)
                {
                    if (tc.ConfineableExcept(1, out _tc, "Null"))
                    {
                        labs.AddRange(new Lab<Model>()
                        {
                            ID = "disposed_notNull",
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
                            .Extend(CreateLabs_Battery(model, _tc)));
                    }
                }
                else if (model.charging)
                {
                    if (!model.Kickboard.Available)
                    {
                        labs.AddRange(new Lab<Model>
                        {
                            ID = "charging_discharged",
                            Asserter = Check
                        }.Merge(GetTemplates(tc)));
                        return;
                    }

                    labs.AddRange(new Lab<Model>
                    {
                        ID = "charging_available",
                        Arranger = (m, md) => m.charging = m.rider == null,
                        Asserter = Check
                    }.Extend(CreateLabs_Battery(model, tc)));
                }
                else
                {
                    labs.AddRange(new Lab<Model>("notCharging")
                        .Extend(CreateLabs_Battery(model, tc)));
                }
            }
            void Ride()
            {
                if (model.isDisposed)
                {
                    ThrowIfDisposed(tc);
                }
                else if (model.charging)
                {
                    labs.AddRange(new Lab<Model>("charging", expectedExceptionType: typeof(InvalidOperationException), toUncontinuable: true)
                        .Merge(GetTemplates(tc)));
                }
                else
                {
                    labs.AddRange(new Lab<Model>("notCharging")
                        .Extend(CreateLabs_Battery(model, tc)));
                }
            }

            void Charge()
            {
                if (model.isDisposed)
                {
                    ThrowIfDisposed(tc);
                }
                else if (model.charging)
                {
                    labs.AddRange(new Lab<Model>("charging", arranger: Check)
                        .Merge(GetTemplates(tc)));
                }
                else
                {
                    labs.AddRange(new Lab<Model>()
                    {
                        ID = "notCharging",
                        Arranger = (m, _) => m.charging = true,
                        Asserter = Check
                    }.Merge(GetTemplates(tc)));
                }
            }
            void DoCharge()
            {
                if (model.charging)
                {
                    labs.AddRange(new Lab<Model>()
                    {
                        ID = "charging",
                        Arranger = (m, _) => m.battery = Math.Min(model.battery + 10, 100),
                        Asserter = Check
                    }.Merge(GetTemplates(tc)));
                }
            }
            void StopCharging()
            {
                if (model.charging)
                {
                    labs.AddRange(new Lab<Model>()
                    {
                        ID = "charging",
                        Arranger = (m, _) => m.charging = false,
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
                if (model.charging)
                {
                    labs.AddRange(new Lab<Model>()
                    {
                        ID = "charging",
                        Arranger = (m, _) => m.charging = false,
                        Asserter = Check
                    }.Extend(CreateLabs_Battery(model, tc)));
                }
                else
                {
                    labs.AddRange(new Lab<Model>("notCharging")
                        .Extend(CreateLabs_Battery(model, tc)));
                }
            }

            void ThrowIfDisposed(TestCase tc)
            {
                labs.AddRange(new Lab<Model>(expectedExceptionType: typeof(ObjectDisposedException))
                {
                    ID = "disposed",
                }.Merge(GetTemplates(tc)));
            }
        }
    }
}
