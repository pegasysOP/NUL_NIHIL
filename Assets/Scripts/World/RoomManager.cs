using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [SerializeField] private PlayerMovement player;
    [SerializeField] private CameraRig cameraRig;
    [SerializeField] private RoomTransitionController transitionController;

    private readonly List<Room> rooms = new List<Room>();
    public Room CurrentRoom { get; private set; }

    public event Action<Room, Room> RoomChanging;
    public event Action<Room> RoomChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // catch rooms enabled before this manager (e.g., additive scene load
        // where the rooms' OnEnable already fired)
        foreach (Room r in FindObjectsByType<Room>())
        {
            if (!rooms.Contains(r))
                rooms.Add(r);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Register(Room room)
    {
        if (room != null && !rooms.Contains(room))
            rooms.Add(room);
    }

    public void Unregister(Room room)
    {
        rooms.Remove(room);
    }

    private void Start()
    {
        // find the player if the reference is missing
        if (player == null)
            player = FindAnyObjectByType<PlayerMovement>();

        if (player == null)
        {
            Debug.LogError("RoomManager: no PlayerMovement found in scene.", this);
            return;
        }

        if (cameraRig == null)
        {
            Debug.LogError("RoomManager: CameraRig not assigned.", this);
            return;
        }

        cameraRig.SetFollow(player.transform);

        Room initial = FindRoomAtPoint(player.Center);
        if (initial == null)
        {
            Debug.LogWarning(
                $"RoomManager: player spawned outside any Room (position {player.Center}).", this);
            return;
        }

        CurrentRoom = initial;
        cameraRig.SetBounds(initial.Bounds);
        initial.NotifyPlayerEntered();
        RoomChanged?.Invoke(initial);
    }

    private void FixedUpdate()
    {
        if (player == null || transitionController == null)
            return;

        if (transitionController.IsTransitioning)
            return;

        // body centre, not the feet pivot, so vertical seams trigger evenly
        Room hit = FindRoomAtPoint(player.Center);
        if (hit == null || hit == CurrentRoom)
            return;

        Room prev = CurrentRoom;
        RoomChanging?.Invoke(prev, hit);
        transitionController.BeginTransition(prev, hit, () => CommitRoom(prev, hit));
    }

    private void CommitRoom(Room prev, Room next)
    {
        if (prev != null)
            prev.NotifyPlayerExited();

        CurrentRoom = next;
        next.NotifyPlayerEntered();
        RoomChanged?.Invoke(next);
    }

    private Room FindRoomAtPoint(Vector2 point)
    {
        // smallest area wins so overlapping rooms produce a deterministic result
        Room best = null;
        float bestArea = float.PositiveInfinity;
        for (int i = 0; i < rooms.Count; i++)
        {
            Room room = rooms[i];
            if (room == null || !room.ContainsPoint(point))
                continue;

            Vector3 size = room.Bounds != null ? room.Bounds.bounds.size : Vector3.zero;
            float area = size.x * size.y;
            if (area < bestArea)
            {
                best = room;
                bestArea = area;
            }
        }
        return best;
    }
}
