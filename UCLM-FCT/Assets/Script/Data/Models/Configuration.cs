using Realms;

public class Configuration : RealmObject
{
    [PrimaryKey]
    public string ID { get; set; }
    
    public User User { get; set; }
    
    public Profile Profile { get; set; }
    
    public int Colors { get; set; } = 3; 
    public bool AutoNarrator { get; set; } = false;

    public bool Sound { get; set; } = true;

    public int GeneralSound { get; set; } = 50;
    
    public int MusicSound { get; set; } = 50;
    
    public int EffectsSound { get; set; } = 50;
    
    public int NarratorSound { get; set; } = 50;
    
    public bool Vibration { get; set; } = false;
}