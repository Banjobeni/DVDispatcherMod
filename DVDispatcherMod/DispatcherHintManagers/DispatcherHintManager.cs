using System;
using System.Collections.Generic;
using System.Linq;
using DV.Logic.Job;
using DVDispatcherMod.DispatcherHints;
using DVDispatcherMod.DispatcherHintShowers;
using DVDispatcherMod.PlayerInteractionManagers;
using JetBrains.Annotations;
using UnityEngine;

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

        private static string GetLocoLocationHint(TrainCar loco) {
            if (loco.derailed) {
                return "derailed";
            }

            var track = loco.FrontBogie.track;
            var position = loco.FrontBogie.traveller.Span;
            var length = loco.FrontBogie.track.GetKinkedPointSet().span;

            var basicInfo = $"{track.LogicTrack().ID.FullDisplayID} {position:F2} / {length:F2}";

            var result = Search(track.LogicTrack(), position, Main.Settings.MaxTrackDistance, (foundTrack, distance) => DoesTrackMatch(foundTrack, distance, loco.transform.position));
            //var resultNonGeneric = Search(track.LogicTrack(), position, t => !t.Track.ID.IsGeneric());
            //var resultNone = Search(track.LogicTrack(), position, t => false);

            return $"{basicInfo}{Environment.NewLine}{FormatSearchResult(result, loco.transform.position)}";
        }

        private static bool DoesTrackMatch(Track track, double distance, Vector3 carPosition) {
            if (track.ID.IsGeneric()) {
                return false;
            }

            if(IsSubstationMilitaryTrack(track) && distance > Main.Settings.MaxMilitaryTrackDistance) {
                return false;
            }

            var stationControllerDistance = (StationController.GetStationByYardID(track.ID.yardId).transform.position - carPosition).magnitude;
            if(stationControllerDistance > Main.Settings.MaxStationCenterDistance) {
                return false;
            }

            return true;
        }

        private static bool IsSubstationMilitaryTrack(Track track) {
            var yardID = track.ID.yardId;
            if (yardID == "MB") {
                return false;
            }

            if (yardID.EndsWith("MB")) {
                return true;
            }

            return false;
        }

        private static string FormatSearchResult((Track TrackOrNull, double? Distance, int? steps, int totalIterations) result, Vector3 carPosition) {
            var (trackOrNull, distance, steps, totalIterations) = result;

            var stationControlDistance = (trackOrNull != null && !trackOrNull.ID.IsGeneric()) ? (StationController.GetStationByYardID(trackOrNull.ID.yardId).transform.position - carPosition).magnitude : (float?)null;

            return $"{trackOrNull?.ID.FullDisplayID ?? "-"} dist:{stationControlDistance?.ToString("F2") ?? "-"} path:{distance:F2} steps:{steps?.ToString() ?? "-"} iters:{totalIterations}";
        }

        private static (Track TrackOrNull, double? Distance, int? steps, int totalIterations) Search(Track track, double trackPosition, double maxTrackDistance, Func<Track, double, bool> doesTrackMatch) {
            if (doesTrackMatch(track, 0)) {
                return (track, 0, 0, 0);
            }

            var fakeMinHeap = new List<(TrackSide TrackSide, double Distance, int Steps)>();
            var visited = new HashSet<TrackSide>();

            if (trackPosition < maxTrackDistance) {
                fakeMinHeap.Add((new TrackSide { Track = track, IsStart = true }, trackPosition, 1));
            }
            if (track.length - trackPosition < maxTrackDistance) {
                fakeMinHeap.Add((new TrackSide { Track = track, IsStart = false }, track.length - trackPosition, 1));
            }
            fakeMinHeap.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            int iterations = 0;
            while (fakeMinHeap.Any()) {
                iterations++;
                var (currentTrackSide, currentDistance, currentSteps) = fakeMinHeap.First();
                fakeMinHeap.RemoveAt(0);
                if (!visited.Contains(currentTrackSide)) {
                    visited.Add(currentTrackSide);

                    if (doesTrackMatch(currentTrackSide.Track, currentDistance)) {
                        return (currentTrackSide.Track, currentDistance, currentSteps, iterations);
                    }

                    var connectedTrackSides = GetConnectedTrackSides(currentTrackSide);
                    foreach (var connectedTrackSide in connectedTrackSides) {
                        fakeMinHeap.Add((connectedTrackSide, currentDistance, currentSteps + 1));
                    }
                    if (currentDistance + currentTrackSide.Track.length < maxTrackDistance) {
                        fakeMinHeap.Add((currentTrackSide with { IsStart = !currentTrackSide.IsStart }, currentDistance + currentTrackSide.Track.length, currentSteps + 1));
                    }
                    fakeMinHeap.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                }
            }

            return (null, null, null, iterations);
        }

        private static List<TrackSide> GetConnectedTrackSides(TrackSide currentTrackSide) {
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