using System.Text;
using UniTest_Test.Subject;

namespace UniTest_Test.SingleState
{
    public class Model : UniTest.Model
    {
        // Subject
        public SingleStatedKickboard Kickboard
        {
            get => (SingleStatedKickboard)Subject;
            set => Subject = value;
        }

        // Mock
        public Rider rider;
        public bool isDisposed;

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
