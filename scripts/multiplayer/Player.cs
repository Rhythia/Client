public class Player
{
    public string Name;
    public bool Ready;

    public Player(string name = "Player")
    {
        Name = name;
        Ready = false;
    }

    public override string ToString() => $"{Name}";
}
