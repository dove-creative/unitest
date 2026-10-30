using System.Text;
using BlackThunder.UniTest.Samples.Subjects;

namespace BlackThunder.UniTest.Samples.Scenarios.SingleState
{
    public class Model : BlackThunder.UniTest.Model
    {
        // Front --------------------
        // Subject
        public SingleStatedKickboard Kickboard
        {
            get => (SingleStatedKickboard)Subject;
            set => Subject = value;
        }

        // Mock
        public Rider Rider;
        public bool IsDisposed;

        // Assets
        public Rider TargetedRider;

        public int RideCount = 0;
        public void OnRide() => RideCount++;


        // Content
        public override string ToString()
        {
            var sb = new StringBuilder();

            if (Kickboard == null)
            {
                sb.Append($"Kickboard is null");
            }
            else
            {
                sb.Append($"Rider : {Kickboard.Rider?.Name ?? "null"}, ");
                sb.Append($"Disposed : {Kickboard.IsDisposed} | ");
            }

            sb.AppendLine(base.ToString());
            return sb.ToString();
        }
    }
}
