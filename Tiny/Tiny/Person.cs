﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;

namespace Tiny
{
    class Person
    {
        private double initAngle;
        private WorldCoordinate initCentrePosition;
        private bool calibrationCompleted = false;

        public Person()
        {

        }

        public void CalibrateKinect()
        {
            // TODO: scale to multiple bodies in one frame
            if (this.ReadyToCalibrate)
            {
                SerializableBodyFrame[] calibrationFrames = new SerializableBodyFrame[Tracker.CALIBRATION_FRAMES];
                int frameCount = 0;
                while (frameCount < Tracker.CALIBRATION_FRAMES)
                {
                    SerializableBodyFrame calibrationFrame;
                    this.calibrationBodyFrames.TryPop(out calibrationFrame);
                    calibrationFrames[frameCount++] = calibrationFrame;
                }
                // Get rid of rest of calibration frames
                //this.calibrationBodyFrames.Clear();

                SerializableBodyFrame firstCalibrationFrame = calibrationFrames[0];
                SerializableBody[] calibrationBodies = new SerializableBody[calibrationFrames.Length];
                for (int i = 0; i < calibrationBodies.Length; i++)
                {
                    calibrationBodies[i] = calibrationFrames[i].Bodies[0];
                }
                this.initAngle = WorldView.GetInitialAngle(firstCalibrationFrame.Bodies[0]);
                Debug.WriteLine("angle: " + this.initAngle);
                this.initCentrePosition = WorldView.GetInitialCentrePosition(calibrationBodies);
                this.depthFrameWidth = firstCalibrationFrame.DepthFrameWidth;
                this.depthFrameHeight = firstCalibrationFrame.DepthFrameHeight;
                this.calibrationCompleted = true;
            }
        }

        // TODO should have one enqueueing thread and one dequeueing thread
        public void ProcessBodyFrame()
        {
            SerializableBodyFrame nextKinectFrame;
            if (!this.incomingBodyFrames.TryDequeue(out nextKinectFrame))
            {
                return;
            }

            Debug.WriteLine("Processing bodyframe @ timestamp: " + nextKinectFrame.TimeStamp);

            if (this.CalibrationCompleted)
            {
                this.processedBodyFrames.Push(Tuple.Create(nextKinectFrame, new WorldView(WorldView.GetBodyWorldCoordinates(nextKinectFrame.Bodies[0], this.initAngle, this.initCentrePosition), this.initAngle, this.initCentrePosition, this.depthFrameWidth, this.depthFrameHeight)));
            }
            else
            {
                if (nextKinectFrame.Bodies.Count > 0)
                {
                    this.calibrationBodyFrames.Push(nextKinectFrame);
                }
            }

            if (this.DisplayKinectBodyFrame != null)
            {
                this.DisplayKinectBodyFrame(nextKinectFrame);
            }
        }
        
        public void CloseBodyViewer()
        {
            this.CloseKinectBodyViewer();
        }

        public bool ReadyToCalibrate
        {
            get
            {
                return !this.calibrationCompleted && this.calibrationBodyFrames.Count >= Tracker.CALIBRATION_FRAMES;
            }
        }

        public bool CalibrationCompleted
        {
            get
            {
                return this.calibrationCompleted;
            }
        }

        public ConcurrentQueue<SerializableBodyFrame> IncomingBodyFrames
        {
            get
            {
                return this.incomingBodyFrames;
            }
        }

        public ConcurrentStack<Tuple<SerializableBodyFrame, WorldView>> ProcessedBodyFrames
        {
            get
            {
                return this.processedBodyFrames;
            }
        }

        public SerializableBodyFrame LastKinectFrame
        {
            get
            {
                if (this.processedBodyFrames.Count > 0)
                {
                    Tuple<SerializableBodyFrame, WorldView> lastKinectFrameTuple;
                    this.processedBodyFrames.TryPeek(out lastKinectFrameTuple);
                    return lastKinectFrameTuple.Item1;
                }
                else if (this.calibrationBodyFrames.Count > 0)
                {
                    SerializableBodyFrame lastKinectFrame;
                    this.calibrationBodyFrames.TryPeek(out lastKinectFrame);
                    return lastKinectFrame;
                }
                else if (this.incomingBodyFrames.Count > 0)
                {
                    SerializableBodyFrame lastKinectFrame;
                    this.incomingBodyFrames.TryPeek(out lastKinectFrame);
                    return lastKinectFrame;
                }
                else
                {
                    return null;
                }
            }
        }

        public WorldView LastWorldView
        {
            get
            {
                if (this.processedBodyFrames.Count > 0)
                {

                    Tuple<SerializableBodyFrame, WorldView> lastKinectFrameTuple;
                    this.processedBodyFrames.TryPeek(out lastKinectFrameTuple);
                    return lastKinectFrameTuple.Item2;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}