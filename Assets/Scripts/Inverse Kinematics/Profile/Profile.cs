using System;
using System.Collections.Generic;

[Serializable]
public class Profile
{
    public string ProfileName;
    public float Height;
    public List<CalibrationPoint> CalibrationPoints;
    public DateTime LastSavedTime;
}
