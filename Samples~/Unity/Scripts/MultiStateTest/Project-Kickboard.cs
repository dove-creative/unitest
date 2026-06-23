using System;
using System.Collections.Generic;
using NUnit.Framework;
using UniTest;
using UniTest_Test.Subject;

namespace UniTest_Test.MultiState
{
    public partial class Project : Project<Model>
    {
        enum State
        {
            Ignite,
            Idle,
            Mounted,
            Disposed
        }
        State GetState(Model model)
        {
            if (model.Kickboard == null)
                return State.Ignite;

            if (model.isDisposed)
                return State.Disposed;

            if (model.rider == null)
                return State.Idle;
            else
                return State.Mounted;
        }

        IEnumerable<Lab<Model>> CreateLabs_Kickboard(Model model, TestCase testCase)
        {
            if (testCase.Count == 0)
                throw new TestCaseAbsentException(nameof(testCase));


            TestCase tc;
            var state = GetState(model);

            if (testCase.Confineable(0, out tc, MainTestCase.Create))
                return new Lab<Model>()
                {
                    ID = "ignite",
                    Arranger = (m, md) =>
                    {
                        m.TargetedRider = new Rider(true, "Targeted Rider");
                        m.TargetCharger = new Model.Charger();

                        m.isDisposed = false;
                        m.battery = (int)md.Metadata;
                    },
                    Asserter = Check
                }.Merge(GetTemplates(tc));


            var labs = new List<Lab<Model>>();

            if (testCase.Confineable(0, out var _tc, MainTestCase.Mount))
            {
                if (_tc.Confineable(1, out tc, "Licensed"))
                    Licensed();

                if (_tc.Confineable(1, out tc, "Same") && model.rider != null)
                    Same();

                if (_tc.Confineable(1, out tc, "NotLicensed"))
                    NotLicensed();

                if (_tc.Confineable(1, out tc, "Null"))
                    Null();
            }


            if (testCase.Confineable(0, out tc, MainTestCase.Ride))
                Ride();

            if (testCase.Confineable(0, out tc, MainTestCase.Dismount))
                Dismount();

            if (testCase.Confineable(0, out tc, MainTestCase.Dispose))
                Dispose();


            return labs;



            void Licensed()
            {
                if (state == State.Idle)
                {
                    labs.AddRange(new Lab<Model>
                    {
                        ID = "idle",
                        Arranger = (m, md) => m.rider = (Rider)md.Metadata,
                        Asserter = Check
                    }.Merge(GetTemplates(tc)));
                }
                else if (state == State.Mounted)
                {
                    labs.AddRange(new Lab<Model>("mounted",
                        expectedExceptionType: typeof(InvalidOperationException),
                        toUncontinuable: true)
                        .Merge(GetTemplates(tc)));
                }
                else if (state == State.Disposed)
                {
                    labs.AddRange(new Lab<Model>("disposed",
                        asserter: Check,
                        expectedExceptionType: typeof(ObjectDisposedException),
                        toUncontinuable: true)
                        .Merge(GetTemplates(tc)));
                }
                else
                    throw new InvalidTestException(state, model);
            }
            void Same()
            {
                if (state == State.Mounted)
                {
                    labs.AddRange(new Lab<Model>("mounted", asserter: Check)
                        .Merge(GetTemplates(tc)));
                }
                else
                    throw new InvalidTestException(state, model);
            }
            void NotLicensed()
            {
                if (state == State.Idle)
                {
                    labs.AddRange(new Lab<Model>("idle", expectedExceptionType: typeof(InvalidOperationException), toUncontinuable: true)
                        .Merge(GetTemplates(tc)));
                }
                else if (state == State.Mounted)
                {
                    labs.AddRange(new Lab<Model>("mounted", expectedExceptionType: typeof(InvalidOperationException), toUncontinuable: true)
                        .Merge(GetTemplates(tc)));
                }
                else if (state == State.Disposed)
                {
                    labs.AddRange(new Lab<Model>("disposed", expectedExceptionType: typeof(ObjectDisposedException), toUncontinuable: true)
                        .Merge(GetTemplates(tc)));
                }
                else
                    throw new InvalidTestException(state, model);
            }
            void Null()
            {
                if (state == State.Idle)
                {
                    labs.AddRange(new Lab<Model>("idle", asserter: Check)
                        .Merge(GetTemplates(tc)));
                }
                else if (state == State.Mounted)
                {
                    labs.AddRange(new Lab<Model>
                    {
                        ID = "mounted",
                        Arranger = (m, _) => m.rider = null,
                        Asserter = Check
                    }.Merge(GetTemplates(tc)));
                }
                else if(state == State.Disposed)
                { 
                    labs.AddRange(new Lab<Model>("disposed", asserter: Check)
                        .Merge(GetTemplates(tc)));
                }
                else
                    throw new InvalidTestException(state, model);
            }

            void Ride()
            {
                if (state == State.Idle)
                {
                    labs.AddRange(new Lab<Model>("idle", expectedExceptionType: typeof(InvalidOperationException), toUncontinuable: true)
                            .Merge(GetTemplates(tc)));
                }
                else if (state == State.Mounted)
                {
                    labs.AddRange(new Lab<Model>
                    {
                        ID = "mounted",
                        Arranger = (m, _) =>
                        {
                            m.RideCount = 0;
                            m.Kickboard.OnRide += m.OnRide;
                        },
                        Asserter = (m, _) =>
                        {
                            Assert.AreEqual(1, m.RideCount);
                            Check(m);

                            m.Kickboard.OnRide -= m.OnRide;
                        }
                    }.Merge(GetTemplates(tc)));
                }
                else if (state == State.Disposed)
                { 
                    labs.AddRange(new Lab<Model>("disposed", asserter: Check, expectedExceptionType: typeof(ObjectDisposedException), toUncontinuable: true)
                        .Merge(GetTemplates(tc)));
                }
                else
                    throw new InvalidTestException(state, model);
            }
            void Dismount()
            {
                if (state == State.Idle)
                {
                    labs.AddRange(new Lab<Model>("idle", asserter: Check)
                        .Merge(GetTemplates(tc)));
                }
                else if (state == State.Mounted)
                {
                    labs.AddRange(new Lab<Model>
                    {
                        ID = "mounted",
                        Arranger = (m, _) => m.rider = null,
                        Asserter = Check
                    }.Merge(GetTemplates(testCase)));
                }
                else if (state == State.Disposed)
                { 
                    labs.AddRange(new Lab<Model>("disposed", asserter: Check)
                        .Merge(GetTemplates(tc)));
                }
                else
                    throw new InvalidTestException(state, model);
            }
            void Dispose()
            {
                if (state == State.Idle)
                {
                    labs.AddRange(new Lab<Model>()
                    {
                        ID = "idle",
                        Arranger = (m, md) =>
                        {
                            m.rider = null;
                            m.isDisposed = true;

                            md.RemainingExecutionCount = postExecutionCount;
                        },
                        Asserter = Check,
                    }.Merge(GetTemplates(tc)));
                }
                else if (state == State.Mounted)
                {
                    labs.AddRange(new Lab<Model>
                    {
                        ID = "mounted",
                        Arranger = (m, md) =>
                        {
                            m.rider = null;
                            m.isDisposed = true;

                            md.RemainingExecutionCount = postExecutionCount;
                        },
                        Asserter = Check,
                    }.Merge(GetTemplates(tc)));
                }
                else if (state == State.Disposed)
                { 
                    labs.AddRange(new Lab<Model>("disposed", asserter: Check)
                        .Merge(GetTemplates(tc)));
                }
                else
                    throw new InvalidTestException(state, model);
            }
        }
    }
}
