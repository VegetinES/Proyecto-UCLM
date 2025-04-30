using Realms;

public class Profile : RealmObject
{
    [PrimaryKey]
    public int ProfileID { get; set; } 
    
    public User User { get; set; }

    public string Name { get; set; }
    
    public string Gender { get; set; }
    
    public Configuration Configuration { get; set; }
    
    public ParentalControl ParentalControl { get; set; }
}