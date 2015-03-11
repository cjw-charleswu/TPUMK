﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectSerializer;
using System.Collections.Concurrent;
using System.Net;
using System.Diagnostics;
using Microsoft.Kinect;
using System.Runtime.CompilerServices;
using KinectMultiTrack.WorldView;

namespace KinectMultiTrack
{
    public class Tracker
    {
        private int expectedKinectsCount;
        // 4 Seconds
        public const uint MIN_CALIBRATION_FRAMES = 120;
        private readonly object syncFrameLock = new object();
        private bool systemCalibrated;

        private readonly ConcurrentDictionary<IPEndPoint, KinectClient> kinectClients;
        private TrackerResult currentResult;

        public event TrackerEventHandler CalibrationEvent;
        public delegate void TrackerEventHandler();

        public Tracker()
        {
            this.systemCalibrated = false;
            this.kinectClients = new ConcurrentDictionary<IPEndPoint, KinectClient>();
            this.currentResult = TrackerResult.Empty;
        }

        public void Configure(TrackerSetup setup)
        {
            this.expectedKinectsCount = setup.KinectsCount;
        }

        public void RemoveClient(IPEndPoint clientIP)
        {
            KinectClient kinect;
            if (this.kinectClients.TryRemove(clientIP, out kinect))
            {
                kinect.DisposeUI();
            }
        }

        private bool KinectsMeetCalibrationRequirement()
        {
            if (this.kinectClients.Count == this.expectedKinectsCount)
            {
                foreach (KinectClient kinect in this.kinectClients.Values)
                {
                    // TODO: Remove HACK!!! Instead, show progress bar
                    if (kinect.UncalibratedFramesCount < Tracker.MIN_CALIBRATION_FRAMES*3)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        // TODO: take the last frame after calibration and start tracking
        private void TryCalibration()
        {
            if (this.systemCalibrated)
            {
                return;
            }
            if (this.kinectClients.Count == this.expectedKinectsCount)
            {
                this.CalibrationEvent();
            }
            if (this.KinectsMeetCalibrationRequirement())
            {
                foreach (KinectClient kinect in this.kinectClients.Values)
                {
                    kinect.Calibrate();
                }
                this.systemCalibrated = true;
                Debug.WriteLine("Calibration done!!", "Tracker");
            }
        }

        public Tuple<TrackerResult, TrackerResult> SynchronizeTracking(IPEndPoint source, SBodyFrame frame)
        {
            // Put this code elsewhere
            if (!this.kinectClients.ContainsKey(source))
            {
                // Pass in height and tilt angle
                this.kinectClients[source] = new KinectClient((uint)this.kinectClients.Count, source, 0.0, 0.0);
            }
            lock (this.syncFrameLock)
            {
                this.kinectClients[source].StoreFrame(frame);
                if (!this.systemCalibrated)
                {
                    this.TryCalibration();
                    if (this.systemCalibrated)
                    {
                        Debug.WriteLine("Before initial people detection!!", "Tracker");
                        this.currentResult = this.PeopleDetection();
                        Debug.WriteLine("Initial people detection done!!", "Tracker");
                    }
                }
                // Do extra work
                if (this.systemCalibrated)
                {
                    this.PeopleTracking(source, frame);
                }
                return Tuple.Create(TrackerResult.Copy(this.currentResult), TrackerResult.Copy(this.currentResult));
            }
        }

        # region People Detection
        private TrackerResult PeopleDetection()
        {
            List<TrackerResult.KinectFOV> currentFOVsList = new List<TrackerResult.KinectFOV>();
            List<TrackerResult.PotentialSkeleton> currentSkeletonsList = new List<TrackerResult.PotentialSkeleton>();
            foreach (KinectClient kinect in this.kinectClients.Values)
            {
                TrackerResult.KinectFOV fov = KinectClient.ExtractFOVInfo(kinect);
                currentFOVsList.Add(fov);
                foreach (MovingSkeleton movingSkeleton in kinect.MovingSkeletons) {
                    currentSkeletonsList.Add(new TrackerResult.PotentialSkeleton(fov, movingSkeleton));
                }
            }

            Debug.WriteLine("FOV: " + currentFOVsList.Count, "People Detection");
            Debug.WriteLine("Skeletons: " + currentSkeletonsList.Count, "People Detection");

            List<TrackerResult.Person> trackedPeopleList = new List<TrackerResult.Person>();
            while (true) {
                TrackerResult.Person person = this.FindPersonWithMultipleSkeletons(ref currentSkeletonsList);
                if (person != null) {
                    // Update person id!!
                    person.Id = (uint)trackedPeopleList.Count;
                    trackedPeopleList.Add(person);
                }
                else
                {
                    break;
                }
            }

            Debug.WriteLine("Tracked people: " + trackedPeopleList.Count, "People Detection");

            // Add remaining skeletons as single-skeleton person
            foreach (TrackerResult.PotentialSkeleton potentialSkeleton in currentSkeletonsList) {
                // Update skeleton id!!
                potentialSkeleton.Id = 0;
                TrackerResult.Person person = new TrackerResult.Person(potentialSkeleton);
                // Update person id!!
                person.Id = (uint)trackedPeopleList.Count;
                trackedPeopleList.Add(person);
            }

            return new TrackerResult(currentFOVsList, trackedPeopleList);
        }

        // Create person based on different FOVs and proximity in worldview positions
        // TODO: extend for multiple FOVs (now: 2)
        // TODO: use previous tracking results
        private TrackerResult.Person FindPersonWithMultipleSkeletons(ref List<TrackerResult.PotentialSkeleton> skeletonsList)
        {
            // Find the two closest skeletons
            double globalDifference = Double.MaxValue;
            TrackerResult.PotentialSkeleton skeletonMatch1 = null, skeletonMatch2 = null;
            foreach (TrackerResult.PotentialSkeleton thisSkeleton in skeletonsList)
            {
                TrackerResult.KinectFOV fov1 = thisSkeleton.FOV;
                MovingSkeleton skeleton1 = thisSkeleton.Skeleton;
                foreach (TrackerResult.PotentialSkeleton thatSkeleton in skeletonsList)
                {
                    TrackerResult.KinectFOV fov2 = thatSkeleton.FOV;
                    MovingSkeleton skeleton2 = thatSkeleton.Skeleton;
                    if (!fov1.Equals(fov2))
                    {
                        WBody pos1 = skeleton1.CurrentPosition.Worldview;
                        WBody pos2 = skeleton2.CurrentPosition.Worldview;
                        double localDifference = WBody.CalculateDifferences(pos1, pos2);
                        if (localDifference < globalDifference)
                        {
                            globalDifference = localDifference;
                            skeletonMatch1 = thisSkeleton;
                            skeletonMatch2 = thatSkeleton;
                        }
                    }
                }
            }

            if (skeletonMatch1 != null && skeletonMatch2 != null)
            {
                skeletonsList.Remove(skeletonMatch1);
                skeletonsList.Remove(skeletonMatch2);
                skeletonMatch1.Id = 0;
                skeletonMatch2.Id = 1;
                return new TrackerResult.Person(skeletonMatch1, skeletonMatch2);
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region People Tracking
        private void PeopleTracking(IPEndPoint source, SBodyFrame frame)
        {
        }
        #endregion

    }
}