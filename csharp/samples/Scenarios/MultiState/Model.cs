using System;
using System.Collections.Generic;
using System.Text;
using BlackThunder.UniTest.Samples.Subjects;

namespace BlackThunder.UniTest.Samples.Scenarios.MultiState
{
    public class Model : BlackThunder.UniTest.Model
    {
        // Front --------------------
        // Subject
        public MultiStatedKickboard Kickboard
        {
            get => (MultiStatedKickboard)Subject;
            set => Subject = value;
        }

        // Mock
        public Rider Rider;
        public int Battery;
        public bool Charging;
        public bool IsDisposed;

        // Assets
        public Rider TargetedRider;

        public int RideCount = 0;

        public class Charger : IObservable<object>
        {
            private List<IObserver<object>> _observers = new();

            private class Token : IDisposable
            {
                private Action _onDispose;

                public Token(Action onDispose) => _onDispose = onDispose;

                public void Dispose()
                {
                    _onDispose?.Invoke();
                    _onDispose = null;
                }
            }

            public IDisposable Subscribe(IObserver<object> observer)
            {
                _observers.Add(observer);
                return new Token(() => _observers.Remove(observer));
            }

            public void DoCharge() => _observers.ForEach(o => o.OnNext(null));
        }
        public Charger TargetCharger;
        public void OnRide() => RideCount++;


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
