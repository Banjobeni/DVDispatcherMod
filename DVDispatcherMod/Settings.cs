using UnityModManagerNet;

namespace DVDispatcherMod {
    public class Settings : UnityModManager.ModSettings, IDrawable {
        [Draw("Show full TrackIDs")]
        public bool ShowFullTrackIDs = false;

        [Draw("Enable debug logging of job structure")]
        public bool EnableDebugLoggingOfJobStructure = false;

        public override void Save(UnityModManager.ModEntry entry) {
            Save(this, entry);
        }

        public void OnChange() { }
    }
}