using System;
using System.Collections.Generic;
using BlackThunder.UniTest;

namespace BlackThunder.UniTest.Samples.Scenarios.MultiState
{
    public partial class Project : Project<Model>
    {
        private IEnumerable<ILab<Model>> CreateBatteryLabs(Model model, TestCase testCase)
        {
            if (testCase.Count == 0)
                throw new TestCaseAbsentException(nameof(testCase));


            TestCase tc;
            var labs = new List<ILab<Model>>();

            if (testCase.Confineable(0, out var _tc, MainTestCase.Mount))
            {
                if (model.Rider == null)
                    _tc = _tc.Exclude(1, "Same");

                if (model.IsDisposed)
                {
                    if (_tc.ConfineableExcept(1, out tc, "Null"))
                    {
                        labs.AddRange(new Lab<Model>()
                        {
                            Id = "disposed",
                            Arranger = (_, md) =>
                            {
                                md.ExpectedExceptionType = typeof(ObjectDisposedException);
                                md.RemainingExecutionCount = _postExecutionCount;
                            }
                        }.Merge(GetTemplates(tc)));
                    }

                    if (_tc.Confineable(1, out tc, "Null"))
                    {
                        labs.AddRange(new Lab<Model>("disposed", asserter: Check)
                            .Extend(CreateKickboardLabs(model, tc)));
                    }
                }
                else if (!model.Kickboard.Available)
                {
                    labs.AddRange(new Lab<Model>("discharged", asserter: Check)
                        .Merge(GetTemplates(_tc)));
                }
                else
                {
                    labs.AddRange(new Lab<Model>("available", asserter: Check)
                        .Extend(CreateKickboardLabs(model, _tc)));
                }
            }

            if (testCase.Confineable(0, out tc, MainTestCase.Ride))
            {
                if (model.IsDisposed)
                {
                    labs.AddRange(new Lab<Model>()
                    {
                        Id = "disposed",
                        Arranger = (_, md) => md.ExpectedExceptionType = typeof(ObjectDisposedException)
                    }.Merge(GetTemplates(tc)));
                }
                else if (!model.Kickboard.Available)
                {
                    labs.AddRange(new Lab<Model>("discharged", asserter: Check)
                        .Merge(GetTemplates(tc)));
                }
                else
                {
                    labs.AddRange(new Lab<Model>
                    {
                        Id = "available",
                        Arranger = (m, _) =>
                        {
                            if (!m.IsDisposed)
                                m.Battery -= 10;
                        },
                        Asserter = Check
                    }.Extend(CreateKickboardLabs(model, tc)));
                }
            }

            if (testCase.ConfineableExcept(0, out tc, MainTestCase.Mount, MainTestCase.Ride))
            {
                labs.AddRange(new Lab<Model>("pass")
                    .Extend(CreateKickboardLabs(model, tc)));
            }

            return labs;
        }
    }
}
