using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SharedModels : MonoBehaviour
{
    public class Configuration
    {
        public int Colors { get; set; } = 3;
        public bool AutoNarrator { get; set; } = false;
        public bool Sound { get; set; } = true;
        public int GeneralSound { get; set; } = 50;
        public int MusicSound { get; set; } = 50;
        public int EffectsSound { get; set; } = 50;
        public int NarratorSound { get; set; } = 50;
        public bool Vibration { get; set; } = false;
    }

    public class ParentalControl
    {
        public bool Activated { get; set; } = false;
        public string Pin { get; set; } = "";
        public bool SoundConf { get; set; } = true;
        public bool AccessibilityConf { get; set; } = true;
        public bool StatisticsConf { get; set; } = true;
        public bool AboutConf { get; set; } = true;
        public bool ProfileConf { get; set; } = true;
    }

    public class User
    {
        public string UID { get; set; }
        public string Email { get; set; }
        public bool IsTutor { get; set; }
    }

    public class Statistics
    {
        public int Level { get; set; }
        public bool Completed { get; set; }
        public int TimeSpent { get; set; }
    }
}
