using UnityModManagerNet;

namespace DVDispatcherMod {
    public class Settings : UnityModManager.ModSettings, IDrawable {
        [Draw("Show full TrackIDs")]
        public bool ShowFullTrackIDs = false;

        [Draw("Show targetting line")]
        public bool ShowAttentionLine = true;

        [Draw("Enable debug logging of job structure")]
        public bool EnableDebugLoggingOfJobStructure = false;

        [Draw("Clear existing messageboxes when displaying new ones - WARNING: Interfears with other mods that use messageboxes!")]
        public bool BoxesClear = false;

        public override void Save(UnityModManager.ModEntry entry) {
            Save(this, entry);
        }

        public void OnChange() { }
    }
}