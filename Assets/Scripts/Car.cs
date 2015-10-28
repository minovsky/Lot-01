﻿using UnityEngine;
using UnityEngine.Assertions;

using System.Collections;
using System.Collections.Generic;

public class Car : Moveable
{
    public static readonly float PATIENCE = 3; 

    private bool parking;
    private Coroutine curRoutine;

    private bool waitingForPedestrian = false;

    HashSet<Pedestrian> obstaclePedestrians = new HashSet<Pedestrian>();

    private IEnumerator Frustation()
    {
        yield return new WaitForSeconds(PATIENCE);

        parking = false;
        OnDestinationReached();
    }

    public override void Update()
    {
        if(!waitingForPedestrian)
        {
            base.Update();
        }
    }

    public void WaitForPedestrian(Pedestrian p)
    {
        obstaclePedestrians.Add(p);
        waitingForPedestrian = true;
    }

    public void StopWaiting(Pedestrian p)
    {
        if(obstaclePedestrians.Contains(p))
        {
            obstaclePedestrians.Remove(p);
            if(obstaclePedestrians.Count == 0)
            {
                waitingForPedestrian = false;
            }
        }
    }

    public virtual void Park(World.WorldCoord dir)
    {
        Assert.IsTrue(World.Instance.ParkingSpotOpen(worldLocation+dir, dir));

        parking = true;
        MoveIfPossible(dir);

        curRoutine = StartCoroutine(Frustation());
    }

    public virtual void UnPark()
    {
    }

    protected sealed override void OnDestinationReached()
    {
        if(!parking)
            OnRoadReached();
        else
            OnParked();
    }

    protected virtual void OnRoadReached()
    {
        MoveIfPossible(direction);
    }

    protected virtual void OnParked()
    {
        if(curRoutine != null)
            StopCoroutine(curRoutine);
    }
}
