using Realms;

public class ParentalControl : RealmObject
{
    [PrimaryKey]
    public string ID { get; set; }
    
    public User User { get; set; }
    
    public Profile Profile { get; set; }
    
    public bool Activated { get; set; } = true; 
    public string Pin { get; set; } = "";
    
    public bool SoundConf { get; set; } = true;
    public bool AccessibilityConf { get; set; } = true;
    public bool StatisticsConf { get; set; } = true;
    public bool AboutConf { get; set; } = true;
    public bool ProfileConf { get; set; } = true;
}