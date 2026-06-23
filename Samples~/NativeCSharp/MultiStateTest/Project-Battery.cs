using System;
using System.Collections.Generic;
using UniTest;

namespace UniTest_Test.MultiState
{
    public partial class Project : Project<Model>
    {
        IEnumerable<ILab<Model>> CreateLabs_Battery(Model model, TestCase testCase)
        {
            if (testCase.Count == 0)
                throw new TestCaseAbsentException(nameof(testCase));
            

            TestCase tc;
            var labs = new List<ILab<Model>>();

            if (testCase.Confineable(0, out var _tc, MainTestCase.Mount))
            {
                if (model.rider == null)
                    _tc = _tc.Exclude(1, "Same");

                if (model.isDisposed)
                {
                    if (_tc.ConfineableExcept(1, out tc, "Null"))
                    {
                        labs.AddRange(new Lab<Model>() 
                        {
                            ID = "disposed",
                            Arranger = (_, md) =>
                            {
                                md.ExpectedExceptionType = typeof(ObjectDisposedException);
                                md.RemainingExecutionCount = postExecutionCount;
                            }
                        }.Merge(GetTemplates(tc)));
                    }

                    if (_tc.Confineable(1, out tc, "Null"))
                    {
                        labs.AddRange(new Lab<Model>("disposed", asserter: Check)
                            .Extend(CreateLabs_Kickboard(model, tc)));
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
                        .Extend(CreateLabs_Kickboard(model, _tc)));
                }
            }

            if (testCase.Confineable(0, out tc, MainTestCase.Ride))
            {
                if (model.isDisposed)
                {
                    labs.AddRange(new Lab<Model>()
                    {
                        ID = "disposed",
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
                        ID = "available",
                        Arranger = (m, _) =>
                        {
                            if (!m.isDisposed)
                                m.battery -= 10;
                        },
                        Asserter = Check
                    }.Extend(CreateLabs_Kickboard(model, tc)));
                }
            }

            if (testCase.ConfineableExcept(0, out tc, MainTestCase.Mount, MainTestCase.Ride))
            {
                labs.AddRange(new Lab<Model>("pass")
                    .Extend(CreateLabs_Kickboard(model, tc)));
            }

            return labs;
        }
    }
}
