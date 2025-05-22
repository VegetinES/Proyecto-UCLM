using SQLite;

[Table("Users")]
public class LocalUser
{
    [PrimaryKey]
    public string UID { get; set; }
    public string Email { get; set; }
    public bool IsTutor { get; set; }
    public string CreatedAt { get; set; }
    public string LastLogin { get; set; }
}

[Table("Profiles")]
public class LocalProfile
{
    [PrimaryKey, AutoIncrement]
    public int ProfileID { get; set; }
    public string UserID { get; set; }
    public string Name { get; set; }
    public string Gender { get; set; }
}

[Table("Configurations")]
public class LocalConfiguration
{
    [PrimaryKey, AutoIncrement]
    public int ID { get; set; }
    public string UserID { get; set; }
    public int ProfileID { get; set; } = 0; // 0 significa sin perfil específico
    public int Colors { get; set; } = 3;
    public bool AutoNarrator { get; set; } = false;
    public bool Sound { get; set; } = true;
    public int GeneralSound { get; set; } = 50;
    public int MusicSound { get; set; } = 50;
    public int EffectsSound { get; set; } = 50;
    public int NarratorSound { get; set; } = 50;
    public bool Vibration { get; set; } = false;
}

[Table("ParentalControls")]
public class LocalParentalControl
{
    [PrimaryKey, AutoIncrement]
    public int ID { get; set; }
    public string UserID { get; set; }
    public int ProfileID { get; set; } = 0; // 0 significa sin perfil específico
    public bool Activated { get; set; } = false;
    public string Pin { get; set; } = "";
    public bool SoundConf { get; set; } = true;
    public bool AccessibilityConf { get; set; } = true;
    public bool StatisticsConf { get; set; } = true;
    public bool AboutConf { get; set; } = true;
    public bool ProfileConf { get; set; } = true;
}