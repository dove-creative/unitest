using System;

namespace UniTest_Test.Subject
{
    public class SingleStatedKickboard : IDisposable
    {
        // Front
        public Rider Rider { get; private set; }

        public event Action OnRide;
        public event Action OnDisposed;
        public bool IsDisposed { get; private set; } = false;


        // Content
        public virtual void Mount(Rider rider)
        {
            if (rider == null)
            {
                Dismount();
                return;
            }

            ThrowIfDisposed();

            if (Rider != null && rider != Rider)
                throw new InvalidOperationException("Kickboard already mounted by another rider.");

            if (!rider.Licensed)
                throw new InvalidOperationException("Rider must be licensed to mount.");

            Rider = rider;
        }
        public void Dismount()
        {
            Rider = null;
        }

        public virtual void Ride()
        {
            ThrowIfDisposed();
            ThrowIfDismounted();

            OnRide?.Invoke();
        }

        protected void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
        protected void ThrowIfDismounted()
        {
            if (Rider == null)
                throw new InvalidOperationException("Cannot operate: kickboard is dismounted.");
        }


        public virtual void Dispose()
        {
            if (IsDisposed)
                return;

            if (Rider != null)
                Dismount();

            IsDisposed = true;
            OnDisposed?.Invoke();
        }
    }
}
