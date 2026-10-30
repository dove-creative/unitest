using System;
using System.Collections.Generic;
using BlackThunder.UniTest;
using BlackThunder.UniTest.Samples;
using BlackThunder.UniTest.Samples.Subjects;

namespace BlackThunder.UniTest.Samples.Scenarios.MultiState
{
    public partial class Project : Project<Model>
    {
        private enum State
        {
            Ignite,
            Idle,
            Mounted,
            Disposed
        }
        private State GetState(Model model)
        {
            if (model.Kickboard == null)
                return State.Ignite;

            if (model.IsDisposed)
                return State.Disposed;

            if (model.Rider == null)
                return State.Idle;
            else
                return State.Mounted;
        }

        private IEnumerable<Lab<Model>> CreateKickboardLabs(Model model, TestCase testCase)
        {
            if (testCase.Count == 0)
                throw new TestCaseAbsentException(nameof(testCase));


            TestCase tc;
            var state = GetState(model);

            if (testCase.Confineable(0, out tc, MainTestCase.Create))
                return new Lab<Model>()
                {
                    Id = "ignite",
                    Arranger = (m, md) =>
                    {
                        m.TargetedRider = new Rider(true, "Targeted Rider");
                        m.TargetCharger = new Model.Charger();

                        m.IsDisposed = false;
                        m.Battery = (int)md.Metadata;
                    },
                    Asserter = Check
                }.Merge(GetTemplates(tc));


            var labs = new List<Lab<Model>>();

            if (testCase.Confineable(0, out var _tc, MainTestCase.Mount))
            {
                if (_tc.Confineable(1, out tc, "Licensed"))
                    Licensed();

                if (_tc.Confineable(1, out tc, "Same") && model.Rider != null)
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
                        Id = "idle",
                        Arranger = (m, md) => m.Rider = (Rider)md.Metadata,
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
                        Id = "mounted",
                        Arranger = (m, _) => m.Rider = null,
                        Asserter = Check
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
                        Id = "mounted",
                        Arranger = (m, _) =>
                        {
                            m.RideCount = 0;
                            m.Kickboard.OnRide += m.OnRide;
                        },
                        Asserter = (m, _) =>
                        {
                            SampleAssert.AreEqual(1, m.RideCount);
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
                        Id = "mounted",
                        Arranger = (m, _) => m.Rider = null,
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
                        Id = "idle",
                        Arranger = (m, md) =>
                        {
                            m.Rider = null;
                            m.IsDisposed = true;

                            md.RemainingExecutionCount = _postExecutionCount;
                        },
                        Asserter = Check,
                    }.Merge(GetTemplates(tc)));
                }
                else if (state == State.Mounted)
                {
                    labs.AddRange(new Lab<Model>
                    {
                        Id = "mounted",
                        Arranger = (m, md) =>
                        {
                            m.Rider = null;
                            m.IsDisposed = true;

                            md.RemainingExecutionCount = _postExecutionCount;
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
