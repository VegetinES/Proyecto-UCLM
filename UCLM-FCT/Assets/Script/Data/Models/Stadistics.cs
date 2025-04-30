using System.Linq;
using Realms;

public class Statistics : RealmObject
{
    [PrimaryKey]
    public int StatID { get; set; }
    
    public User User { get; set; }
    
    public Profile Profile { get; set; }

    public int Level { get; set; }
    public bool Completed { get; set; }
    public int Failed { get; set; }
    
    [Backlink(nameof(LevelAttempt.Statistics))]
    public IQueryable<LevelAttempt> Attempts { get; }
}