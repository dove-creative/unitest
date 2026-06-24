using System;

namespace UniTest_Test.Subject
{
    public class MultiStatedKickboard : SingleStatedKickboard
    {
        // Front
        public int Battery { get; private set; }
        public bool Charging { get; private set; } = false;

        // Control
        public bool Available => Battery > 10;

        // Inner
        IDisposable _handle;


        // Content
        public MultiStatedKickboard(int battery = 50) => Battery = battery;

        public override void Mount(Rider rider)
        {
            if (rider == null)
            {
                Dismount();
                return;
            }

            ThrowIfDisposed();

            if (!Available)
            {
                return;
            }

            if (Charging)
                StopCharging();

            base.Mount(rider);
        }

        public override void Ride()
        {
            ThrowIfDisposed();

            if (Charging)
                throw new InvalidOperationException("Unable to ride the Kickboard during Charging.");

            if (!Available)
            {
                return;
            }

            Battery = Math.Max(Battery - 10, 0);
            base.Ride();
        }

        public void Charge(IObservable<object> charger)
        {
            ThrowIfDisposed();

            if (Charging) return;
            Charging = true;

            _handle = charger.Subscribe(new Charger(this));
        }
        public void StopCharging()
        {
            if (!Charging) return;
            Charging = false;

            _handle.Dispose();
            _handle = null;
        }

        class Charger : IObserver<object>
        {
            MultiStatedKickboard _parent;

            public Charger(MultiStatedKickboard parent) => _parent = parent;

            public void OnCompleted() { }
            public void OnError(Exception _) { }
            public void OnNext(object _) => _parent.DoCharge();
        }
        void DoCharge()
        {
            if (!Charging)
                throw new InvalidOperationException("The kickboard that is not in Charging state cannot be charged.");

            Battery = Math.Min(Battery + 10, 100);
        }

        public override void Dispose()
        {
            if (IsDisposed)
                return;

            StopCharging();
            base.Dispose();
        }
    }
}
