public class Player(string name = "Player")
{
    public string Name = name;
    public bool Ready = false;

    public override string ToString() => $"{Name}";
}
