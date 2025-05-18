using UnityModManagerNet;

namespace DVDispatcherMod {
    public class Settings : UnityModManager.ModSettings, IDrawable {
        [Draw("Show full TrackIDs")]
        public bool ShowFullTrackIDs = false;

        [Draw("Show targetting line")]
        public bool ShowAttentionLine = true;

        [Draw("Enable debug logging of job structure")]
        public bool EnableDebugLoggingOfJobStructure = false;

        [Draw("NamedTrackSearch: Maximum air line distance from station center")]
        public double MaxStationCenterDistance = 1000;

        [Draw("NamedTrackSearch: Maximum track distance")]
        public double MaxTrackDistance = 800;

        [Draw("NamedTrackSearch: Maximum military track distance")]
        public double MaxMilitaryTrackDistance = 400;

        public override void Save(UnityModManager.ModEntry entry) {
            Save(this, entry);
        }

        public void OnChange() { }
    }
}