using System;
using System.Text;
using System.Collections.Generic;
using UniTest_Test.Subject;

namespace UniTest_Test.MultiState
{
    public class Model : UniTest.Model
    {
        // Subject
        public MultiStatedKickboard Kickboard
        {
            get => (MultiStatedKickboard)Subject;
            set => Subject = value;
        }

        // Mock
        public Rider rider;
        public int battery;
        public bool charging;
        public bool isDisposed;

        // Assets
        public Rider TargetedRider;

        public int RideCount = 0;
        public void OnRide() => RideCount++;

        public class Charger : IObservable<object>
        {
            List<IObserver<object>> _observers = new();

            public IDisposable Subscribe(IObserver<object> observer)
            {
                _observers.Add(observer);
                return new Token(() => _observers.Remove(observer));
            }

            public void DoCharge() => _observers.ForEach(o => o.OnNext(null));

            class Token : IDisposable
            {
                Action _onDispose;

                public Token(Action onDispose)
                    => _onDispose = onDispose;

                public void Dispose()
                {
                    _onDispose?.Invoke();
                    _onDispose = null;
                }
            }
        }
        public Charger TargetCharger;


        // Content
        public override string ToString()
        {
            var sb = new StringBuilder();

            if (Kickboard == null)
            {
                sb.Append($"Kickboard is null | ");
            }
            else
            {
                sb.Append($"Rider : {Kickboard.Rider?.Name ?? "None"}, ");
                sb.Append($"Battery : {Kickboard.Battery}, ");
                sb.Append($"Charging : {Kickboard.Charging}, ");
                sb.Append($"Disposed : {Kickboard.IsDisposed} | ");
            }

            sb.AppendLine(base.ToString());
            return sb.ToString().TrimEnd('\r', '\n');
        }
    }
}
