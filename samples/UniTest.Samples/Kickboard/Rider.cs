namespace UniTest_Test.Subject
{
    public class Rider
    {
        public string Name { get; }
        public bool Licensed { get; set; } = false;

        public Rider(bool licensed, string name = "Rider")
        {
            Name = name;
            Licensed = licensed;
        }

        public override string ToString() => $"Rider '{Name}, {Licensed}' ({GetHashCode()})";
    }
}
