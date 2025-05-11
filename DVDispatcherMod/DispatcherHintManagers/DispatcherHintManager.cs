using System;
using DVDispatcherMod.DispatcherHints;
using DVDispatcherMod.DispatcherHintShowers;
using DVDispatcherMod.PlayerInteractionManagers;
using JetBrains.Annotations;

namespace DVDispatcherMod.DispatcherHintManagers {
    public sealed class DispatcherHintManager : IDisposable {
        private readonly IPlayerInteractionManager _playerInteractionManager;
        private readonly IDispatcherHintShower _dispatcherHintShower;
        private readonly TaskOverviewGenerator _taskOverviewGenerator;

        private int _counterValue;

        public DispatcherHintManager([NotNull] IPlayerInteractionManager playerInteractionManager, [NotNull] IDispatcherHintShower dispatcherHintShower, TaskOverviewGenerator taskOverviewGenerator) {
            _playerInteractionManager = playerInteractionManager;
            _dispatcherHintShower = dispatcherHintShower;
            _taskOverviewGenerator = taskOverviewGenerator;

            _playerInteractionManager.JobOfInterestChanged += HandleJobObInterestChanged;
        }

        public void SetCounter(int counterValue) {
            _counterValue = counterValue;
            UpdateDispatcherHint();
        }

        private void HandleJobObInterestChanged() {
            UpdateDispatcherHint();

            if (Main.Settings.EnableDebugLoggingOfJobStructure) {
                var job = _playerInteractionManager.JobOfInterest;
                if (job != null) {
                    DebugOutputJobWriter.DebugOutputJob(job);
                }
            }
        }

        private void UpdateDispatcherHint() {
            var currentHint = GetCurrentDispatcherHint();
            _dispatcherHintShower.SetDispatcherHint(currentHint);

            var loco = PlayerManager.LastLoco;
            if (loco != null) {
                var locoLocationHint = GetLocoLocationHint(loco);
                _dispatcherHintShower.SetLocoLocationHint(locoLocationHint);
            } else {
                _dispatcherHintShower.SetLocoLocationHint(null);
            }
        }

        private string GetLocoLocationHint(TrainCar loco) {
            if (loco.derailed) {
                return "derailed";
            }

            var track = loco.FrontBogie.track;
            var position = loco.FrontBogie.traveller.Span;

            return track.LogicTrack().ID.FullDisplayID + " " + position.ToString("F2");
        }

        private DispatcherHint GetCurrentDispatcherHint() {
            var job = _playerInteractionManager.JobOfInterest;
            if (job != null) {
                return new JobDispatch(job, _taskOverviewGenerator).GetDispatcherHint(_counterValue);
            } else {
                return null;
            }
        }

        public void Dispose() {
            _dispatcherHintShower.Dispose();

            _playerInteractionManager.JobOfInterestChanged -= HandleJobObInterestChanged;
            _playerInteractionManager.Dispose();
        }
    }
}