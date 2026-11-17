using System;
using UnityEngine;

public enum ExperimentSceneType
{
    Clap,
    Explosion,
    Aircraft
}

public enum SeatZone
{
    FrontLeft,
    FrontCenter,
    FrontRight,
    MiddleLeft,
    MiddleCenter,
    MiddleRight,
    RearLeft,
    RearCenter,
    RearRight
}

public enum ExperimentRunMode
{
    Debug,
    Participant
}

[Serializable]
public struct TrialCondition
{
    public ExperimentSceneType scene;
    public SeatZone seat;
    [Range(0, 4)] public int offsetIndex;

    public TrialCondition(ExperimentSceneType scene, SeatZone seat, int offsetIndex)
    {
        this.scene = scene;
        this.seat = seat;
        this.offsetIndex = offsetIndex;
    }
}
