using System;
using System.Collections.Generic;
using System.Linq;
using DV.Logic.Job;
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

        private string GetLocoLocationHint(TrainCar loco) {
            if (loco.derailed) {
                return "derailed";
            }

            var track = loco.FrontBogie.track;
            var position = loco.FrontBogie.traveller.Span;
            var length = loco.FrontBogie.track.GetKinkedPointSet().span;

            var basicInfo = $"{track.LogicTrack().ID.FullDisplayID} {position:F2} / {length:F2}";

            var foundTrackWithDistanceOrNull = Search(track.LogicTrack(), position);
            if (foundTrackWithDistanceOrNull != null) {
                return $"{basicInfo}{Environment.NewLine}{foundTrackWithDistanceOrNull.Value.Track.ID.FullDisplayID} {foundTrackWithDistanceOrNull.Value.Distance}";
            }

            return basicInfo;
        }

        private (Track Track, double Distance)? Search(Track track, double position) {
            if (!track.ID.IsGeneric()) {
                return (track, 0);
            }

            var fakeMinHeap = new List<(TrackSide TrackSide, double Distance)>();
            var visited = new HashSet<TrackSide>();

            fakeMinHeap.Add((new TrackSide { Track = track, IsStart = true }, position));
            fakeMinHeap.Add((new TrackSide { Track = track, IsStart = false }, track.length - position));
            fakeMinHeap.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            while (fakeMinHeap.Any()) {
                var (currentTrackSide, currentDistance) = fakeMinHeap.First();
                fakeMinHeap.RemoveAt(0);
                if (!visited.Contains(currentTrackSide)) {
                    visited.Add(currentTrackSide);

                    if (!currentTrackSide.Track.ID.IsGeneric()) {
                        return (currentTrackSide.Track, currentDistance);
                    }

                    var connectedTrackSides = GetConnectedTrackSides(currentTrackSide);
                    foreach (var connectedTrackSide in connectedTrackSides) {
                        fakeMinHeap.Add((connectedTrackSide, currentDistance));
                    }
                    fakeMinHeap.Add((currentTrackSide with { IsStart = !currentTrackSide.IsStart }, currentDistance + currentTrackSide.Track.length));
                    fakeMinHeap.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                }
            }

            return null;
        }

        private List<TrackSide> GetConnectedTrackSides(TrackSide currentTrackSide) {
            var railTrack = currentTrackSide.Track.RailTrack();
            var branches = currentTrackSide.IsStart ? railTrack.GetAllInBranches() : railTrack.GetAllOutBranches();
            if (branches == null) {
                return new List<TrackSide>();
            }
            return branches.Select(b => new TrackSide { Track = b.track.LogicTrack(), IsStart = b.first }).ToList();
        }


        public record TrackSide {
            public Track Track { get; set; }
            public bool IsStart { get; set; }
        }
    }
}