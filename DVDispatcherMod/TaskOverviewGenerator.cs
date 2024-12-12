using DV.Logic.Job;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DVDispatcherMod
{
    public class TaskOverviewGenerator
    {
        private static readonly FieldInfo StationControllerStationRangeField = typeof(StationController).GetField("stationRange", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly Dictionary<string, Color> _yardID2Color;

        public TaskOverviewGenerator()
        {
            _yardID2Color = StationController.allStations.ToDictionary(s => s.stationInfo.YardID, s => s.stationInfo.StationColor);
        }

        public void DebugTaskOutput(Task task, ref int i, ref int carsNum, HashSet<string> cars, HashSet<string> warehouseLoadTypeCars)
        {
            //Debug.Log("task index: " + i);
            i++;
            var TaskData = task.GetTaskData();
            //Debug.Log("task type: " + TaskData.type);

            if (TaskData.nestedTasks != null && TaskData.nestedTasks.Any())
            {
                //Debug.Log(TaskData.nestedTasks.Count.ToString() + " nested tasks");

                foreach (Task NestedJobTask in TaskData.nestedTasks)
                {
                    DebugTaskOutput(NestedJobTask, ref i, ref carsNum, cars, warehouseLoadTypeCars);
                }
            }
            else
            {
                //Debug.Log("no nested tasks");
            }

            if (TaskData.type == TaskType.Warehouse)
            {
                if (TaskData.warehouseTaskType == WarehouseTaskType.Loading)
                {
                    if (TaskData.cars != null && TaskData.cars.Any())
                    {
                        foreach (var car in TaskData.cars)
                        {
                            if (!warehouseLoadTypeCars.Contains(car.ID))
                            {
                                warehouseLoadTypeCars.Add(car.ID);
                            }
                        }
                    }
                }
            }

            if (TaskData.cars != null && TaskData.cars.Any())
            {
                //Debug.Log((TaskData.cars.Count).ToString() + " cars");
                if (TaskData.type != TaskType.Warehouse)
                {
                }
                else
                {
                    //Debug.Log("start track: " + TaskData.startTrack.ID);
                }
                //Debug.Log("destination track: " + TaskData.destinationTrack.ID);
                foreach (var car in TaskData.cars)
                {
                    if (!cars.Contains(car.ID))
                    {
                        cars.Add(car.ID);
                        carsNum++;
                    }
                }
            }
            else
            {
                //Debug.Log("no cars");
            }
        }

        public void RecursivelyGetAllTasksData(Task task, ref int i, ref List<TaskData> JobTasks, ref List<int> indentList, int currentLevel = 0)
        {
            var TaskData = task.GetTaskData();
            if (!JobTasks.Contains(TaskData))
            {
                //Debug.Log($"Adding {TaskData.type} task as {i} with indent level {currentLevel}");
                JobTasks.Add(TaskData);
                indentList.Add(currentLevel);
                i++;
            }
            if (TaskData.nestedTasks != null && TaskData.nestedTasks.Any())
            {
                //Debug.Log($"that has {TaskData.nestedTasks.Count} nested tasks");
                foreach (Task NestedJobTask in TaskData.nestedTasks)
                {
                    RecursivelyGetAllTasksData(NestedJobTask, ref i, ref JobTasks, ref indentList, currentLevel + 1);
                }
            }
            else
            {
                //Debug.Log($"that has {0} nested tasks");
            }
        }

        public List<TaskData> MergeTasks(List<TaskData> JobTasks, ref int i)
        {
            List<Car> mergedCars;
            for (int index = 1; index < JobTasks.Count; index++)
            {
                var currentTask = JobTasks[index];
                var previousTask = JobTasks[index - 1];
                //Debug.Log("1st task type: " + previousTask.type);
                //Debug.Log("2nd task type: " + currentTask.type);

                if ((currentTask.type != TaskType.Sequential || currentTask.type != TaskType.Parallel) && (previousTask.type != TaskType.Sequential || previousTask.type != TaskType.Parallel))
                {
                    if (currentTask.type == TaskType.Warehouse && previousTask.type == TaskType.Warehouse)
                    {
                        //Debug.Log("1st task dest. track: " + previousTask.destinationTrack.ID);
                        //Debug.Log("2nd task dest. track: " + currentTask.destinationTrack.ID);

                        if (currentTask.warehouseTaskType == WarehouseTaskType.Loading &&
                            previousTask.warehouseTaskType == WarehouseTaskType.Loading &&
                            currentTask.destinationTrack == previousTask.destinationTrack)
                        {
                            //Debug.Log("Conditions met, tasks will merge");
                            mergedCars = previousTask.cars.Union(currentTask.cars).ToList();
                            TaskData mergedTaskData = new TaskData(
                                currentTask.type,
                                currentTask.state,
                                currentTask.taskStartTime,
                                currentTask.taskFinishTime,
                                mergedCars,
                                currentTask.startTrack,
                                currentTask.destinationTrack,
                                currentTask.warehouseTaskType,
                                currentTask.cargoTypePerCar,
                                currentTask.totalCargoAmount,
                                null,
                                currentTask.couplingRequiredAndNotDone,
                                currentTask.anyHandbrakeRequiredAndNotDone
                            );
                            //Debug.Log($"Merged tasks of {previousTask.cars.Count} and {currentTask.cars.Count} to a new task of {mergedTaskData.cars.Count} cars.");

                            JobTasks.RemoveAt(index - 1);
                            JobTasks[index - 1] = mergedTaskData;
                            i--;
                        }
                        else
                        {
                            //Debug.Log("Not merging, leaving task as are");
                        }
                    }
                }
                else
                {
                    //Debug.Log("Task is Sequential or Parallel, skipping");
                }
            }
            return JobTasks;
        }

        public Tuple<List<TaskData>, List<int>> RebuiltTasksList(Job job)
        {
            int i = 0;
            var JobTasks = new List<TaskData>();
            var indentList = new List<int>();
            //Debug.Log("index i is: " + i);
            foreach (var jobTask in job.tasks)
            {
                RecursivelyGetAllTasksData(jobTask, ref i, ref JobTasks, ref indentList);
                //Debug.Log("Flattened job task list (before merging):");
                for (int i1 = 0; i1 < JobTasks.Count; i1++)
                {
                    TaskData task = JobTasks[i1];
                    //Debug.Log(task.type + $" task, at list index {i1}");
                }
                i -= 1;
                //Debug.Log("index i is: " + i + " list lenght is: " + JobTasks.Count);
                JobTasks = MergeTasks(JobTasks, ref i);
            }
            int remergeCheck = JobTasks.Count();
            while (remergeCheck > 6)
            {
                i = 0;
                JobTasks = MergeTasks(JobTasks, ref i);
                remergeCheck--;
            }
            //Debug.Log("Rebuilt job task list (after merging):");
            foreach (var task in JobTasks)
            {
                //Debug.Log(task.type + "task");
            }        
            Tuple<List<TaskData>, List<int>> output = new Tuple<List<TaskData>, List<int>>(JobTasks, indentList);
            return output;
        }

        public string GetTaskOverview(Job job)
        {
            var nearestYardID = StationController.allStations.OrderBy(s => ((StationJobGenerationRange)StationControllerStationRangeField.GetValue(s)).PlayerSqrDistanceFromStationCenter).FirstOrDefault()?.stationInfo.YardID;
            int i = 0;
            //var carsNum = 0;
            int indent = 0;
            var cars = new HashSet<string>();
            var warehouseLoadTypeCars = new HashSet<string>();
            //Debug.Log(job.ID);

            foreach (var jobTask in job.tasks)
            {
                //DebugTaskOutput(jobTask, ref i, ref carsNum, cars, warehouseLoadTypeCars);
            }
            //Debug.Log("number of encountered cars: " + carsNum);
            //Debug.Log(cars.ToString());
            //Debug.Log("load number of cars" + warehouseLoadTypeCars.Count());
            //return GenerateTaskOverview(0, job.tasks.First(), nearestYardID); - GTO probably not needed since recursive taskgoing is already done before in RecursivelyGetAllTasksData (?)
            var TaskStringBuilder = new StringBuilder();
            var outputStringBuilder = new StringBuilder();
            foreach (var taskTaskData in RebuiltTasksList(job).Item1)
            {
                indent = RebuiltTasksList(job).Item2[i];
                TaskStringBuilder.Clear();

                if (taskTaskData.type == TaskType.Transport)
                {
                    AppendIndented(indent, $"Transport {FormatNumberOfCars(taskTaskData.cars.Count)} from {FormatTrack(taskTaskData.startTrack, nearestYardID)} to {FormatTrack(taskTaskData.destinationTrack, nearestYardID)}", TaskStringBuilder);
                }
                else if (taskTaskData.type == TaskType.Warehouse)
                {
                    if (taskTaskData.warehouseTaskType == WarehouseTaskType.Loading)
                    {
                        AppendIndented(indent, $"Load {FormatNumberOfCars(taskTaskData.cars.Count)} at {FormatTrack(taskTaskData.destinationTrack, nearestYardID)}", TaskStringBuilder);
                    }
                    else if (taskTaskData.warehouseTaskType == WarehouseTaskType.Unloading)
                    {
                        AppendIndented(indent, $"Unload {FormatNumberOfCars(taskTaskData.cars.Count)} at {FormatTrack(taskTaskData.destinationTrack, nearestYardID)}", TaskStringBuilder);
                    }
                    else
                    {
                        AppendIndented(indent, "(unknown WarehouseTaskType)", TaskStringBuilder);
                    }
                }
                else if (taskTaskData.type == TaskType.Parallel)
                {
                    //Debug.Log("GTO-TaskType is parallel");
                    //AppendIndented(indent, "Parallel", TaskStringBuilder);
                }
                else if (taskTaskData.type == TaskType.Sequential)
                {
                    //Debug.Log("GTO-TaskType is sequential");
                    //AppendIndented(indent, "Sequential", TaskStringBuilder);
                }
                else
                {
                    AppendIndented(indent, "(unknown TaskType)", TaskStringBuilder);
                }

                if (taskTaskData.state == TaskState.Done)
                {
                    outputStringBuilder.Append(GetColoredString(Color.green, TaskStringBuilder.ToString()));
                }
                else if (taskTaskData.state == TaskState.Failed)
                {
                    outputStringBuilder.Append(GetColoredString(Color.red, TaskStringBuilder.ToString()));
                }
                else
                {
                    outputStringBuilder.Append(TaskStringBuilder.ToString());
                }
                i++;
            }
            //Debug.Log("final string is: " + Environment.NewLine + outputStringBuilder.ToString());
            return outputStringBuilder.ToString();
        }
        private static string GetColoredString(Color color, string content)
        {
            var colorString = GetHexColorComponent(color.r) + GetHexColorComponent(color.g) + GetHexColorComponent(color.b);
            return $"<color=#{colorString}>{content}</color>";
        }

        private static string GetHexColorComponent(float colorComponent)
        {
            return ((int)(255 * colorComponent)).ToString("X2");
        }

        private string FormatTrack(Track track, string nearestYardID)
        {
            if (track.ID.yardId == nearestYardID)
            {
                return Main.Settings.ShowFullTrackIDs ? track.ID.FullID : track.ID.TrackPartOnly;
            }

            var trackDisplayID = Main.Settings.ShowFullTrackIDs ? track.ID.FullID : track.ID.FullDisplayID;
            if (_yardID2Color.TryGetValue(track.ID.yardId, out var color))
            {
                return GetColoredString(color, trackDisplayID);
            }

            return trackDisplayID;
        }

        private static string FormatNumberOfCars(int count)
        {
            if (count == 1)
            {
                return "1 car";
            }
            else
            {
                return count + " cars";
            }
        }

        private static void AppendIndented(int indent, string value, StringBuilder sb)
        {
            for (var i = 0; i < indent; i += 1)
            {
                sb.Append("  ");
            }
            sb.Append("- ");
            sb.Append(value + Environment.NewLine);
        }
    }
}