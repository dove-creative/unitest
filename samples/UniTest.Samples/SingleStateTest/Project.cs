using System;
using System.Collections.Generic;
using System.Linq;
using UniTest.Samples.NativeCSharp;
using UniTest;
using UniTest_Test.Subject;

namespace UniTest_Test.SingleState
{
    public class Project : Project<Model>
    {
        const int PostExecutionCount = 2;

        enum KickboardState
        {
            Idle,
            Mounted,
            Disposed
        }
        KickboardState GetState(Model model)
        {
            if (model.Kickboard.IsDisposed)
                return KickboardState.Disposed;

            if (model.Kickboard.Rider != null)
                return KickboardState.Mounted;
            else
                return KickboardState.Idle;
        }

        void Check(Model model)
        {
            SampleAssert.IsNotNull(model, "Kickboard is Null");

            SampleAssert.AreEqual(model.isDisposed, model.Kickboard.IsDisposed, "Dispose state mismatched");
            SampleAssert.AreSame(model.rider, model.Kickboard.Rider, "Rider mismatched");
        }


        public override IEnumerable<ILab<Model>> CreateLabs(Model model)
        {
            var labs = new List<CompactLab<Model>>();

            // Ignite
            if (model.Kickboard == null)
            {
                labs.Add(new("Ignite")
                {
                    Actor = m =>
                    {
                        m.Kickboard = new();

                        m.rider = null;
                        m.isDisposed = false;

                        m.TargetedRider = new(true, "Targeted Rider");
                    },
                    Asserter = Check
                });

                return labs.Select(l => l.Build());
            }


            // Mount
            switch (GetState(model))
            {
                case KickboardState.Idle:
                    labs.Add(new("Mount_Licensed")
                    {
                        Arranger = m => m.rider = new Rider(true),
                        Actor = m => m.Kickboard.Mount(m.rider),
                        Asserter = Check
                    });
                    labs.Add(new("Mount_Targeted")
                    {
                        Arranger = m => m.rider = m.TargetedRider,
                        Actor = m => m.Kickboard.Mount(m.rider),
                        Asserter = Check
                    });
                    labs.Add(new("Mount_NotLicensed")
                    {
                        Actor = m => SampleAssert.Throws<InvalidOperationException>(() => m.Kickboard.Mount(new Rider(false))),
                        ToUncontinuable = true
                    });
                    labs.Add(new("Mount_Null")
                    {
                        Arranger = m => m.rider = null,
                        Actor = modml => modml.Kickboard.Mount(null),
                        Asserter = Check
                    });
                    break;


                case KickboardState.Mounted:
                    labs.Add(new("Mount_Licensed")
                    {
                        Actor = m => SampleAssert.Throws<InvalidOperationException>(() => m.Kickboard.Mount(new Rider(true))),
                        ToUncontinuable = true
                    });
                    if (model.Kickboard.Rider != model.TargetedRider) labs.Add(new()
                    {
                        ID = "Mount_Targeted",
                        Actor = m => SampleAssert.Throws<InvalidOperationException>(() => m.Kickboard.Mount(m.TargetedRider)),
                        ToUncontinuable = true
                    });
                    labs.Add(new("Mount_Same")
                    {
                        Actor = m => SampleAssert.DoesNotThrow(() => m.Kickboard.Mount(m.rider)),
                        Asserter = Check
                    });
                    labs.Add(new("Mount_NotLicensed")
                    {
                        Actor = m => SampleAssert.Throws<InvalidOperationException>(() => m.Kickboard.Mount(new Rider(false))),
                        ToUncontinuable = true
                    });
                    labs.Add(new("Mount_Null")
                    {
                        Arranger = m => m.rider = null,
                        Actor = m => m.Kickboard.Mount(null),
                        Asserter = Check
                    });
                    break;


                case KickboardState.Disposed:
                    labs.Add(new("Mount_Licensed")
                    {
                        Actor = m => SampleAssert.Throws<ObjectDisposedException>(() => m.Kickboard.Mount(new Rider(true))),
                        Asserter = Check
                    });
                    labs.Add(new("Mount_Targeted")
                    {
                        Actor = m => SampleAssert.Throws<ObjectDisposedException>(() => m.Kickboard.Mount(m.TargetedRider)),
                        Asserter = Check
                    });
                    labs.Add(new("Mount_NotLicensed")
                    {
                        Actor = m => SampleAssert.Throws<ObjectDisposedException>(() => m.Kickboard.Mount(new Rider(false))),
                        Asserter = Check
                    });
                    labs.Add(new("Mount_Null")
                    {
                        Actor = m => SampleAssert.DoesNotThrow(() => m.Kickboard.Mount(null)),
                        Asserter = Check
                    });
                    break;
            }


            // Ride
            switch (GetState(model))
            {
                case KickboardState.Idle:
                    labs.Add(new("Ride")
                    {
                        Actor = m => SampleAssert.Throws<InvalidOperationException>(() => m.Kickboard.Ride()),
                        ToUncontinuable = true
                    });
                    break;


                case KickboardState.Mounted:
                    labs.Add(new("Ride")
                    {
                        Arranger = model => model.Kickboard.OnRide += model.OnRide,
                        Actor = model => model.Kickboard.Ride(),
                        Asserter = model =>
                        {
                            SampleAssert.AreEqual(1, model.RideCount);

                            model.Kickboard.OnRide -= model.OnRide;
                            model.RideCount = 0;
                        }
                    });
                    break;


                case KickboardState.Disposed:
                    labs.Add(new("Ride")
                    {
                        Actor = model => SampleAssert.Throws<ObjectDisposedException>(() => model.Kickboard.Ride()),
                        Asserter = Check
                    });
                    break;
            }


            // Dismount
            switch (GetState(model))
            {
                case KickboardState.Idle:
                    labs.Add(new("Dismount")
                    {
                        Actor = model => model.Kickboard.Dismount(),
                        Asserter = Check
                    });
                    break;


                case KickboardState.Mounted:
                    labs.Add(new("Dismount")
                    {
                        Arranger = model => model.rider = null,
                        Actor = model => model.Kickboard.Dismount(),
                        Asserter = Check
                    });
                    break;


                case KickboardState.Disposed:
                    labs.Add(new("Dismount")
                    {
                        Actor = model => SampleAssert.DoesNotThrow(() => model.Kickboard.Dismount()),
                        Asserter = Check
                    });
                    break;
            }


            // Dispose
            switch (GetState(model))
            {
                case KickboardState.Idle:
                    labs.Add(new("Dispose")
                    {
                        Arranger = model => model.isDisposed = true,
                        Actor = model => model.Kickboard.Dispose(),
                        Asserter = Check,
                        RemainingExecutionCount = PostExecutionCount
                    });
                    break;


                case KickboardState.Mounted:
                    labs.Add(new("Dispose")
                    {
                        Arranger = model =>
                        {
                            model.isDisposed = true;
                            model.rider = null;
                        },
                        Actor = model => model.Kickboard.Dispose(),
                        Asserter = Check,
                        RemainingExecutionCount = PostExecutionCount
                    });
                    break;


                case KickboardState.Disposed:
                    labs.Add(new("Dispose")
                    {
                        Actor = model => SampleAssert.DoesNotThrow(() => model.Kickboard.Dispose()),
                        Asserter = Check,
                        RemainingExecutionCount = PostExecutionCount
                    });
                    break;
            }

            return labs.Select(l => l.Build());
        }
    } 
}
