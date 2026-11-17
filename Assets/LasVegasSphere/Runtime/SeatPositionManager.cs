using System;
using UnityEngine;

public sealed class SeatPositionManager : MonoBehaviour
{
    [Serializable]
    private struct SeatAnchor
    {
        public SeatZone zone;
        public Transform anchor;
    }

    [SerializeField] private Camera participantCamera;
    [SerializeField] private Transform screenCenterViewTarget;
    [SerializeField] private SeatAnchor[] seats;

    public SeatZone CurrentZone { get; private set; }

    public void Configure(Camera camera, Transform[] anchors, Transform sharedViewTarget)
    {
        participantCamera = camera;
        screenCenterViewTarget = sharedViewTarget;
        seats = new SeatAnchor[anchors.Length];
        for (int i = 0; i < anchors.Length; i++)
        {
            seats[i].zone = (SeatZone)i;
            seats[i].anchor = anchors[i];
        }
    }

    public void Apply(SeatZone zone)
    {
        if (participantCamera == null || screenCenterViewTarget == null || seats == null) return;
        for (int i = 0; i < seats.Length; i++)
        {
            if (seats[i].zone != zone || seats[i].anchor == null) continue;
            participantCamera.transform.position = seats[i].anchor.position;
            participantCamera.transform.LookAt(screenCenterViewTarget.position);
            CurrentZone = zone;
            return;
        }
    }
}
