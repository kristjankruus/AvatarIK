using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameDevWare.Serialization;

public class ProfileController
{
    private List<Profile> _profiles = new List<Profile>();
    private string fileName = "profiles.json";

    public void SaveProfile(Profile profile)
    {
        _profiles = _profiles.Distinct().ToList();

        var exisitingProfile = _profiles.Where(x => x.ProfileName == profile.ProfileName).FirstOrDefault();
        if (exisitingProfile != null)
        {
            _profiles.Replace(exisitingProfile, profile);
        }
        else
        {
            _profiles.Add(profile);
        }

        var jsonString = Json.SerializeToString(_profiles);
        using (StreamWriter newTask = new StreamWriter(fileName, false))
        {
            newTask.WriteLine(jsonString);
        }
    }

    public Profile LoadProfileByName(string profileName)
    {
        try
        {
            byte[] baites = File.ReadAllBytes(fileName);
            _profiles = Json.Deserialize<List<Profile>>(new MemoryStream(baites));
        }
        catch (System.Exception)
        {
        }
        return _profiles.Where(x => x.ProfileName == profileName).FirstOrDefault();
    }
}
