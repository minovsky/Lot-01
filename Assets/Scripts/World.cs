﻿#define DEBUG

using System;
using System.Collections;

using UnityEngine;
using UnityEngine.Assertions;

public class World : MonoBehaviour
{
    public struct WorldCoord
    {
        public int x;
        public int y;

        public WorldCoord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static WorldCoord operator+(WorldCoord c1, WorldCoord c2)
        {
            return new WorldCoord(c1.x + c2.x, c1.y + c2.y);
        }
        public static WorldCoord operator-(WorldCoord c1, WorldCoord c2)
        {
            return new WorldCoord(c1.x - c2.x, c1.y - c2.y);
        }

        public override string ToString()
        {
            return String.Format("({0}, {1})", x, y);
        }
    }

    private static World m_instance = null;

    public static World Instance
    {
        get
        {
            if(m_instance == null)
            {
                m_instance = FindObjectOfType<World>();
            }

            return m_instance;
        }
    }


    public enum LANE_SIDE {UP_LEFT=0, DOWN_RIGHT};
    private struct Lane
    {
        GameObject left;
        GameObject right;

        public enum ORIENTATION {VERTICAL, HORIZONTAL, INTERSECTION}
        public ORIENTATION orientation;
        public ORIENTATION currentFlow;

        public GameObject GetLaneSide(LANE_SIDE ls)
        {
            switch(ls)
            {
                case LANE_SIDE.UP_LEFT:
                    return left;
                default:
                case LANE_SIDE.DOWN_RIGHT:
                    return right;
            }
        }

        public void SetLaneSide(LANE_SIDE ls, GameObject go)
        {
            switch(ls)
            {
                case LANE_SIDE.UP_LEFT:
                    left = go;
                    break;
                default:
                case LANE_SIDE.DOWN_RIGHT:
                    right = go;
                    break;
            }
        }
    }

    public enum DIRECTION {UP, RIGHT, DOWN, LEFT};

    public float gridToWorldSize = .5f;

    public static readonly int WORLD_WIDTH = 11;
    public static readonly int WORLD_HEIGHT = 9;

#if DEBUG
    public GameObject gridSpot;
    GameObject[,,] debugLanePosition = new GameObject[WORLD_WIDTH,WORLD_HEIGHT,4];
#endif

    private static readonly int[,] template =
    {
        {3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3}, 

        {0, 4, 0, 4, 0, 4, 0, 4, 0, 4, 0}, 
        {0, 4, 0, 4, 0, 4, 0, 4, 0, 4, 0}, 
        {0, 4, 0, 4, 0, 4, 0, 4, 0, 4, 0}, 

        {3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3}, 

        {0, 4, 0, 4, 0, 4, 0, 4, 0, 4, 0}, 
        {0, 4, 0, 4, 0, 4, 0, 4, 0, 4, 0}, 
        {0, 4, 0, 4, 0, 4, 0, 4, 0, 4, 0}, 

        {3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3}, 
    };

    private bool[,] m_parkingSpot = GenerateParking();
    private Lane[,] m_lot = GenerateLot();

    private static bool[,] GenerateParking()
    {
        bool[,] parking = new bool[WORLD_WIDTH, WORLD_HEIGHT];

        for(int i = 0; i < WORLD_WIDTH; i++)
        {
            for(int j = 0; j < WORLD_HEIGHT; j++)
            {
                parking[i,j] = template[j,i] == 4;
            }
        }

        return parking;
    }

    private static Lane[,] GenerateLot()
    {
        Lane[,] lot = new Lane[WORLD_WIDTH, WORLD_HEIGHT];

        for(int i = 0; i < WORLD_WIDTH; i++)
        {
            for(int j = 0; j < WORLD_HEIGHT; j++)
            {
                switch(template[j,i])
                {
                    default:
                    case 4:
                    case 0:
                        lot[i,j].orientation = Lane.ORIENTATION.VERTICAL;
                        break;
                    case 1:
                        lot[i,j].orientation = Lane.ORIENTATION.HORIZONTAL;
                        break;
                    case 3:
                        lot[i,j].orientation = Lane.ORIENTATION.INTERSECTION;
                        lot[i,j].currentFlow = Lane.ORIENTATION.INTERSECTION;
                        break;
                }
            }
        }

        return lot;
    }

    public static bool WithinBounds(WorldCoord c)
    {
        return (0 <= c.x && c.x < WORLD_WIDTH) && (0 <= c.y && c.y < WORLD_HEIGHT);
    }
    private static bool IsDirection(WorldCoord dir)
    {
        if(dir.x == 0)
            return dir.y == 1 || dir.y == -1;
        else if(dir.y == 0)
            return dir.x == 1 || dir.x == -1;
        return false;
    }
    public static DIRECTION directionFromCoord(WorldCoord dir)
    {
        Assert.IsTrue(IsDirection(dir));
        if(dir.y == 1)
        {
            return DIRECTION.UP;
        }
        else if(dir.x == 1)
        {
            return DIRECTION.RIGHT;
        }
        else if(dir.y == -1)
        {
            return DIRECTION.DOWN;
        }
        else
        {
            return DIRECTION.LEFT;
        }
    }

    public static LANE_SIDE laneSideFromDirection(DIRECTION dir)
    {
        switch(dir)
        {
            case DIRECTION.UP:
            case DIRECTION.LEFT:
                return LANE_SIDE.UP_LEFT;
            default:
            case DIRECTION.DOWN:
            case DIRECTION.RIGHT:
                return LANE_SIDE.DOWN_RIGHT;
        }
    }

    public bool CanMoveInto(WorldCoord c, WorldCoord direction)
    {
        Assert.IsTrue(WithinBounds(c));
        Assert.IsTrue(IsDirection(direction));

        Lane lane = m_lot[c.x,c.y];

        bool isCorrectFlow = false;
        DIRECTION enumDirection = directionFromCoord(direction);
        Lane.ORIENTATION compareToOrientation = lane.orientation != Lane.ORIENTATION.INTERSECTION ? lane.orientation : lane.currentFlow;

        switch(enumDirection)
        {
            case DIRECTION.UP:
            case DIRECTION.DOWN:
                isCorrectFlow = compareToOrientation == Lane.ORIENTATION.VERTICAL || compareToOrientation == Lane.ORIENTATION.INTERSECTION;
                break;
            default:
            case DIRECTION.RIGHT:
            case DIRECTION.LEFT:
                isCorrectFlow = compareToOrientation == Lane.ORIENTATION.HORIZONTAL || compareToOrientation == Lane.ORIENTATION.INTERSECTION;
                break;
        }

        if(isCorrectFlow)
        {
            switch(enumDirection)
            {
                case DIRECTION.UP:
                case DIRECTION.LEFT:
                    return m_lot[c.x, c.y].GetLaneSide(LANE_SIDE.UP_LEFT) == null;
                default:
                case DIRECTION.DOWN:
                case DIRECTION.RIGHT:
                    return m_lot[c.x, c.y].GetLaneSide(LANE_SIDE.DOWN_RIGHT) == null;
            }
        }
        else
        {
            return false;
        }
    }

    public void MoveInto(WorldCoord c, WorldCoord direction, GameObject go)
    {
        Assert.IsTrue(CanMoveInto(c, direction));
        Assert.IsTrue(WithinBounds(c));
        Assert.IsTrue(IsDirection(direction));

        m_lot[c.x,c.y].SetLaneSide(laneSideFromDirection(directionFromCoord(direction)), go);

        if(m_lot[c.x,c.y].orientation == Lane.ORIENTATION.INTERSECTION)
        {
            switch(directionFromCoord(direction))
            {
                case DIRECTION.UP:
                case DIRECTION.DOWN:
                    m_lot[c.x,c.y].currentFlow = Lane.ORIENTATION.VERTICAL;
                    break;
                default:
                case DIRECTION.RIGHT:
                case DIRECTION.LEFT:
                    m_lot[c.x,c.y].currentFlow = Lane.ORIENTATION.HORIZONTAL;
                    break;
            }
        }
    }

    public bool IsParkingSpot(WorldCoord c)
    {
        return m_parkingSpot[c.x,c.y];
    }

    public void LeaveFrom(WorldCoord c, WorldCoord direction)
    {
        Assert.IsTrue(!CanMoveInto(c, direction));
        Assert.IsTrue(WithinBounds(c));
        Assert.IsTrue(IsDirection(direction));

        LANE_SIDE ls = laneSideFromDirection(directionFromCoord(direction));

        m_lot[c.x,c.y].SetLaneSide(ls, null);
        if(m_lot[c.x,c.y].orientation == Lane.ORIENTATION.INTERSECTION)
        {
            switch(ls)
            {
                case LANE_SIDE.UP_LEFT:
                    if(m_lot[c.x,c.y].GetLaneSide(LANE_SIDE.DOWN_RIGHT) == null)
                        m_lot[c.x,c.y].currentFlow = Lane.ORIENTATION.INTERSECTION;
                    break;
                case LANE_SIDE.DOWN_RIGHT:
                    if(m_lot[c.x,c.y].GetLaneSide(LANE_SIDE.UP_LEFT) == null)
                        m_lot[c.x,c.y].currentFlow = Lane.ORIENTATION.INTERSECTION;
                    break;
            }
        }
    }

    public Vector2 GetWorldLocation(WorldCoord c, WorldCoord direction)
    {
        //center
        Vector2 location = new Vector2((float)((2*c.x)+1)*gridToWorldSize, (float)((2*c.y)+1)*gridToWorldSize);

        switch(directionFromCoord(direction))
        {
            case DIRECTION.UP:
                location.x += gridToWorldSize/2;
                break;
            case DIRECTION.DOWN:
                location.x -= gridToWorldSize/2;
                break;
            case DIRECTION.LEFT:
                location.y += gridToWorldSize/2;
                break;
            case DIRECTION.RIGHT:
                location.y -= gridToWorldSize/2;
                break;
        }
        
        return transform.TransformPoint(location);
    }

    public void Start()
    {
#if DEBUG
        for(int i = 0; i < WORLD_WIDTH; i++)
        {
            for(int j = 0; j < WORLD_HEIGHT; j++)
            {
                for(int k = 0; k < 4; k++)
                {
                    debugLanePosition[i,j,k] = ((GameObject)Instantiate(gridSpot, Vector3.zero, Quaternion.identity));
                    debugLanePosition[i,j,k].transform.parent = this.transform;
                }
            }
        }
#endif
    }

    public void Update()
    {
#if DEBUG
        World.WorldCoord[] directions = {
            new World.WorldCoord(0, 1),
            new World.WorldCoord(1, 0),
            new World.WorldCoord(0, -1),
            new World.WorldCoord(-1, 0),
            };
        for(int i = 0; i < World.WORLD_WIDTH; i++)
        {
            for(int j = 0; j < World.WORLD_HEIGHT; j++)
            {
                int laneIndex = 0;
                for(int k = 0; k < directions.Length; k++)
                {
                    World.WorldCoord coord = new World.WorldCoord(i, j);
                    if(World.Instance.CanMoveInto(coord, directions[k]))
                    {
                        Vector2 location = World.Instance.GetWorldLocation(coord, directions[k]);
                        debugLanePosition[i,j,laneIndex].transform.position = location;
                        laneIndex++;
                    }
                }
                for(;laneIndex < 4; laneIndex++)
                {
                    Vector2 location = new Vector2(1000, 1000);
                    debugLanePosition[i,j,laneIndex].transform.position = location;
                }
            }
        }
#endif
    }
}
