using DV.UI;
using DV.Utils;
using DVDispatcherMod.DispatcherHintManagers;
using DVDispatcherMod.DispatcherHintShowers;
using DVDispatcherMod.PlayerInteractionManagers;
using System;
using UnityModManagerNet;

namespace DVDispatcherMod {
    internal static class Main {
        private const float SETUP_INTERVAL = 1;
        private const float POINTER_INTERVAL = 1; // Time between forced dispatcher updates.

        private static float _timer;
        private static int _counter;
        private static bool _isEnabled;

        private static DispatcherHintManager _dispatcherHintManager;

        public static UnityModManager.ModEntry ModEntry { get; private set; }
        public static Settings Settings { get; private set; }

        private static bool Load(UnityModManager.ModEntry modEntry) {
            ModEntry = modEntry;
            ModEntry.OnToggle = OnToggle;
            ModEntry.OnUpdate = OnUpdate;
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry) {
            Settings.Draw(modEntry);
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry) {
            Settings.Save(modEntry);
        }

        private static bool OnToggle(UnityModManager.ModEntry _, bool isEnabled) {
            _isEnabled = isEnabled;

            ModEntry.Logger.Log($"isEnabled toggled to {isEnabled}.");

            return true;
        }

        private static bool IsUIReady()
        {
            var canvas = SingletonBehaviour<ACanvasController<CanvasController.ElementType>>.Instance;
            return canvas && canvas.NotificationManager != null;
        }

        private static void OnUpdate(UnityModManager.ModEntry mod, float delta) {
            try {
                if (IsModEnabledAndWorldReadyForInteraction()) {
                    _timer += delta;

                    if (_dispatcherHintManager == null) {
                        if (_timer > SETUP_INTERVAL && IsUIReady()) {
                            _timer %= SETUP_INTERVAL;

                            _dispatcherHintManager = TryCreateDispatcherHintManager();
                            if (_dispatcherHintManager != null) {
                                mod.Logger.Log("Dispatcher hint manager created.");
                            }
                        }
                    }

                    if (_dispatcherHintManager != null)
                    {
                        // If UI disappeared (eg. back to menu), dispose immediately
                        if (!IsUIReady())
                        {
                            _dispatcherHintManager.Dispose();
                            _dispatcherHintManager = null;
                            ModEntry.Logger.Log("Disposed dispatcher hint manager (UI not present).");
                        }
                        else if (_timer > POINTER_INTERVAL)
                        {
                            _counter++;
                            _timer %= POINTER_INTERVAL;

                            _dispatcherHintManager.SetCounter(_counter);
                        }
                    }
                } else 
                {
                    if (_dispatcherHintManager != null) {
                        _dispatcherHintManager.Dispose();
                        _dispatcherHintManager = null;
                        ModEntry.Logger.Log("Disposed dispatcher hint manager.");
                    }
                }
            } catch (Exception e) {
                ModEntry.Logger.Log(e.ToString());
            }
        }

        private static bool IsModEnabledAndWorldReadyForInteraction() {
            if (!_isEnabled) {
                return false;
            }
            if (LoadingScreenManager.IsLoading) {
                return false;
            }
            if (!WorldStreamingInit.IsLoaded) {
                return false;
            }
            return true;
        }

        private static DispatcherHintManager TryCreateDispatcherHintManager() {
            if (!IsUIReady()) return null;
            if (VRManager.IsVREnabled()) {
                var playerInteractionManager = VRPlayerInteractionManagerFactory.TryCreate();
                if (playerInteractionManager == null) {
                    return null;
                }

                var dispatcherHintShower = new DispatcherHintShower();
                return new DispatcherHintManager(playerInteractionManager, dispatcherHintShower, new TaskOverviewGenerator());
            } else {
                var playerInteractionManager = NonVRPlayerInteractionManagerFactory.TryCreate();
                if (playerInteractionManager == null) {
                    return null;
                }

                var dispatcherHintShower = new DispatcherHintShower();
                return new DispatcherHintManager(playerInteractionManager, dispatcherHintShower, new TaskOverviewGenerator());
            }
        }
    }
}
