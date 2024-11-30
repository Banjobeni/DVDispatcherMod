using UnityModManagerNet;

namespace DVDispatcherMod
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Show full TrackIDs")]
        public bool fullTrackIDs = false;

        [Draw("Enable logging")]
        public bool enableLogging = false;

        public override void Save(UnityModManager.ModEntry entry)
        {
            Save(this, entry);
        }

        public void OnChange()
        { }
    }
}