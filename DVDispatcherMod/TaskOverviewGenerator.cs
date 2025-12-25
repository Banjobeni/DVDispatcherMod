using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DV.Logic.Job;
using UnityEngine;

namespace DVDispatcherMod {
    public class TaskOverviewGenerator {
        private static readonly FieldInfo StationControllerStationRangeField = typeof(StationController).GetField("stationRange", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly Dictionary<string, Color> _yardID2Color;

        public TaskOverviewGenerator() {
            _yardID2Color = StationController.allStations.ToDictionary(s => s.stationInfo.YardID, s => s.stationInfo.StationColor);
        }

        public string GetTaskOverview(Job job) {
            var currentYardID = GetCurrentStation(job).stationInfo.YardID;
            return GenerateTaskOverview(0, job.tasks.First(), currentYardID);
        }

        private string GenerateTaskOverview(int indent, Task task, string nearestYardID) 
        {
            if (task.InstanceTaskType == TaskType.Parallel || task.InstanceTaskType == TaskType.Sequential) 
            {
                var taskData = task.GetTaskData();
                var taskStrings = taskData.nestedTasks.Select(t => GenerateTaskOverview(indent + 1, t, nearestYardID)).ToList();
                for (int i = 0; i < taskStrings.Count - 1; i++)
                {
                    var prev = taskStrings[i].TrimStart().Split(" ".ToCharArray());
                    var curr = taskStrings[i + 1].TrimStart().Split(" ".ToCharArray());
                    if (prev.Length >= 6 && curr.Length >= 6 && prev[1] == "Load" && curr[1] == "Load" && prev[5] == curr[5]) 
                    {
                        int newCount = int.Parse(prev[2]) + int.Parse(curr[2]);
                        string indentPrefix = taskStrings[i].Substring(0, taskStrings[i].IndexOf('-'));
                        taskStrings[i] = $"{indentPrefix}- Load {newCount} cars at {prev[5]}";
                        taskStrings.RemoveAt(i + 1);
                        i--; 
                    }
                    else if (prev.Length >= 6 && curr.Length >= 6 && prev[1] == "Unload" && curr[1] == "Unload" && prev[5] == curr[5])
                    {
                        int newCount = int.Parse(prev[2]) + int.Parse(curr[2]);
                        string indentPrefix = taskStrings[i].Substring(0, taskStrings[i].IndexOf('-'));
                        taskStrings[i] = $"{indentPrefix}- Unload {newCount} cars at {prev[5]}";
                        taskStrings.RemoveAt(i + 1);
                        i--;
                    }
                }
                return string.Join(Environment.NewLine, taskStrings);
            } 
            else 
            {
                return GetTaskString(indent, task, nearestYardID);
            }
        }

        private string GetTaskString(int indent, Task task, string nearestYardID) {
            var taskData = task.GetTaskData();

            var stringBuilder = new StringBuilder();

            if (task.InstanceTaskType == TaskType.Parallel) {
                AppendIndented(indent, "Parallel", stringBuilder);
            } else if (task.InstanceTaskType == TaskType.Sequential) {
                AppendIndented(indent, "Sequential", stringBuilder);
            } else if (task.InstanceTaskType == TaskType.Transport) {
                AppendIndented(indent, $"Transport {FormatNumberOfCars(taskData.cars.Count)} from {FormatTrack(taskData.startTrack, nearestYardID)} to {FormatTrack(taskData.destinationTrack, nearestYardID)}", stringBuilder);
            } else if (task.InstanceTaskType == TaskType.Warehouse) {
                if (taskData.warehouseTaskType == WarehouseTaskType.Loading) {
                    AppendIndented(indent, $"Load {FormatNumberOfCars(taskData.cars.Count)} at {FormatTrack(taskData.destinationTrack, nearestYardID)}", stringBuilder);
                } else if (taskData.warehouseTaskType == WarehouseTaskType.Unloading) {
                    AppendIndented(indent, $"Unload {FormatNumberOfCars(taskData.cars.Count)} at {FormatTrack(taskData.destinationTrack, nearestYardID)}", stringBuilder);
                } else {
                    AppendIndented(indent, "(unknown WarehouseTaskType)", stringBuilder);
                }
            } else if (task.InstanceTaskType == (TaskType)42) {
                // Passenger Jobs RuralTaskData
                Color ruralColor = new Color32(151, 121, 210, 255);

                dynamic ruralTask = taskData;
                string stationId = ruralTask.stationId;
                string action = (bool)ruralTask.isLoading ? "Load" : "Unload";

                AppendIndented(indent, $"{action} {FormatNumberOfCars(taskData.cars.Count)} at Station {GetColoredString(ruralColor, stationId)}", stringBuilder);
            } else {
                AppendIndented(indent, "(unknown TaskType)", stringBuilder);
            }

            if (task.state == TaskState.Done) {
                return GetColoredString(Color.green, stringBuilder.ToString());
            } else if (task.state == TaskState.Failed) {
                return GetColoredString(Color.red, stringBuilder.ToString());
            } else {
                return stringBuilder.ToString();
            }
        }

        private static string GetColoredString(Color color, string content) {
            var colorString = GetHexColorComponent(color.r) + GetHexColorComponent(color.g) + GetHexColorComponent(color.b);
            return $"<color=#{colorString}>{content}</color>";
        }

        private static string GetHexColorComponent(float colorComponent) {
            return ((int)(255 * colorComponent)).ToString("X2");
        }

        private string FormatTrack(Track track, string nearestYardID) {
            if (track.ID.yardId == nearestYardID) {
                return Main.Settings.ShowFullTrackIDs ? track.ID.FullID : track.ID.TrackPartOnly;
            }

            var trackDisplayID = Main.Settings.ShowFullTrackIDs ? track.ID.FullID : track.ID.FullDisplayID;
            if (_yardID2Color.TryGetValue(track.ID.yardId, out var color)) {
                return GetColoredString(color, trackDisplayID);
            }

            return trackDisplayID;
        }

        public static StationController StationFromTrack(Track track)
        {
            var searchedTracks = new HashSet<Track>();
            if (track == null) return null;
            return FindStationRecursive(track, 0, searchedTracks);
        }

        private static StationController FindStationRecursive(Track track, int depth, HashSet<Track> searchedTracks)
        {
            if (track == null || searchedTracks.Contains(track)) return null;
            searchedTracks.Add(track);
            var yardId = track.ID?.yardId;
            if (yardId == null) return null;
            var station = StationController.GetStationByYardID(yardId);
            if (station != null) return station;
            if (yardId == "#Y" && depth < 10)
            {
                var connectedTracks = GetConnectedTracks(track);
                foreach (var connected in connectedTracks)
                {
                    var result = FindStationRecursive(connected, depth + 1, searchedTracks);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }

        private static IEnumerable<Track> GetConnectedTracks(Track track)
        {
            var tracks = new List<Track>();
            if (track.InTrack != null) tracks.Add(track.InTrack);
            if (track.OutTrack != null) tracks.Add(track.OutTrack);
            if (track.PossibleInTracks != null) tracks.AddRange(track.PossibleInTracks);
            if (track.PossibleOutTracks != null) tracks.AddRange(track.PossibleOutTracks);
            return tracks;
        }

        public StationController GetCurrentStation(Job job)
        {
            var closestStation = StationController.allStations.OrderBy(s => ((StationJobGenerationRange)StationControllerStationRangeField.GetValue(s)).PlayerSqrDistanceFromStationCenter).FirstOrDefault();
            if (job.State == DV.ThingTypes.JobState.Completed)
            {
                return closestStation;
            }
            var track = FindTrackInTasks(job.tasks);
            if (track != null)
            {
                var station = StationFromTrack(track);
                if (station != null)
                    return station;
            }
            return closestStation;
        }

        public Track FindTrackInTasks(IEnumerable<Task> tasks)
        {
            foreach (var task in tasks)
            {
                var taskData = task.GetTaskData();
                if (task.InstanceTaskType == TaskType.Transport && taskData?.cars?.Count > 0)
                {
                    return taskData.cars[0]?.CurrentTrack;
                }
                if (taskData?.nestedTasks != null)
                {
                    var nestedResult = FindTrackInTasks(taskData.nestedTasks);
                    if (nestedResult != null)
                        return nestedResult;
                }
            }
            return null;
        }


        private static string FormatNumberOfCars(int count) {
            if (count == 1) {
                return "1 car";
            } else {
                return count + " cars";
            }
        }

        private static void AppendIndented(int indent, string value, StringBuilder sb) {
            for (var i = 0; i < indent; i += 1) {
                sb.Append("  ");
            }
            sb.Append("- ");
            sb.Append(value);
        }
    }
}