namespace RimworldCloudSave;

public class RimCloudSaveSettings : ModSettings
{
    public string CloudServiceName = "Steam";
    
    public override void ExposeData()
    {
        base.ExposeData();
        
        Scribe_Values.Look(ref CloudServiceName, "CloudServiceName", "Steam", true);
    }
}