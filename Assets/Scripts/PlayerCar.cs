using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerCar : Car
{

    private enum MOVE_ACTION {FORWARD=0, RIGHT, LEFT};
    private World.WorldCoord nextDir;

    private static readonly float INPUT_DELAY= 1;
    private Coroutine inputResetRoutine = null;

    private IEnumerator inputDelayReset()
    {
        yield return new WaitForSeconds(INPUT_DELAY);

        nextDir = direction;
    }

    // Use this for initialization
    public override void Start ()
    {
        nextDir = direction;
    }

    public override void Update()
    {
        bool inputSensed = false;
        if(Input.GetAxis("Vertical") > 0)
        {
            inputSensed = true;
                nextDir = new World.WorldCoord(0, 1);
        }
        else if(Input.GetAxis("Vertical") < 0)
        {
            inputSensed = true;
                nextDir = new World.WorldCoord(0, -1);
        }
        else if(Input.GetAxis("Horizontal") < 0)
        {
            inputSensed = true;
                nextDir = new World.WorldCoord(-1, 0);
        }
        else if(Input.GetAxis("Horizontal") > 0)
        {
            inputSensed = true;
                nextDir = new World.WorldCoord(1, 0);
        }

        if(inputSensed)
        {
            if(inputResetRoutine != null)
                StopCoroutine(inputResetRoutine);
            StartCoroutine(inputDelayReset());
        }
        base.Update();
    }

    private void MoveBasedOnInput()
    {
        uint wrapIndex = (uint)Array.IndexOf(World.POSSIBLE_DIRECTIONS, direction);

        if((nextDir == World.POSSIBLE_DIRECTIONS[(wrapIndex-1) % World.POSSIBLE_DIRECTIONS.Length]
                || nextDir == World.POSSIBLE_DIRECTIONS[(wrapIndex+1) % World.POSSIBLE_DIRECTIONS.Length])
                && World.Instance.CanMoveInto(worldLocation+nextDir, nextDir))
        {
            MoveIfPossible(nextDir);
        }
        else
        {
            //Move forward
            MoveIfPossible(direction);
        }

    }

    protected override void OnRoadReached()
    {
        World.WorldCoord parkingOffset;
        if(World.Instance.NextToOpenParking(worldLocation, direction, out parkingOffset))
        {
            Park(parkingOffset);
        }
        else
        {
            MoveBasedOnInput();
        }
    }

    protected override void OnParked()
    {
    }
}
