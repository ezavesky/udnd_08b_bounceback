﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallSpawner : MonoBehaviour {

    [System.Serializable]
    public class BallProperties {
        public GameObject obj;
        public float timeSpawn;
    }

    protected static System.Object thisLock = new System.Object (); //lock for matrix update
    protected static BallSpawner current;
    protected GameManager gameManager;

    public GameObject pooledBall; //the prefab of the object in the object pool
    protected List<BallProperties> pooledBalls; //the object pool
    public static int ballPoolNum = 0; //a number used to cycle through the pooled objects
    protected static int ballPoolLast = 0;     //index for last loop
    
    // NOTE: replaced the user-specified "ballsAmount" with the formulaic number to create (see "CreatePooledBalls")

    private float cooldownLength = 0.5f;
    public float ballTimeout = 10.0f;      // default time before a ball is reused
    public float ballMaxDistance = 10.0f;   // default distance away from user to reuse a bal

    void Awake()
    {
        current = this; //makes it so the functions in ObjectPool can be accessed easily anywhere
    }

    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        //Create Bullet Pool
        pooledBalls = new List<BallProperties>();
        ballPoolLast = 0;
        CreatePooledBalls();
        InvokeRepeating("SpawnBall", cooldownLength*4, cooldownLength);     //call cooldown after a brief start-up delay
    }

    //create new pooled/cached balls, ideally this should only happen once!
    protected void CreatePooledBalls() {
        lock (thisLock) {
            ballPoolNum = pooledBalls.Count;
            int ballsAmount = (int)Mathf.Floor(ballTimeout/cooldownLength);
            //Debug.Log(string.Format("[GreatePooledBalls]: Executing to generate {0} additional balls for existing {1}, time:{2}.",
            //                        ballsAmount, pooledBalls.Count, Time.fixedTime));
            for (int i = 0; i < ballsAmount; i++)
            {
                BallProperties propNew = new BallProperties();
                propNew.obj = Instantiate(pooledBall);
                propNew.obj.SetActive(false);
                propNew.timeSpawn = -2 * ballTimeout;
                pooledBalls.Add(propNew);
            }
        }
    }

    public GameObject GetPooledBall(bool bApplyDistanceRequirement=true)
    {
        lock (thisLock) {
            do {
                if (ballPoolNum >= pooledBalls.Count) {      //looped past the known index?
                    ballPoolNum = 0;
                }
                //this ball is "old enough" to reuse?
                if ((pooledBalls[ballPoolNum].timeSpawn + ballTimeout) < Time.fixedTime) {  
                    //now compute distance away from the user/camera
                    float fDistBall = Vector3.Distance(Camera.main.transform.position, 
                                                       pooledBalls[ballPoolNum].obj.transform.position);
                    if (!bApplyDistanceRequirement || (fDistBall > ballMaxDistance || !pooledBalls[ballPoolNum].obj.activeInHierarchy)) {
                                      // Debug.Log(string.Format("[GetPooledBall]: Idx:{0}, BallDist: {1}, MaxDist: {2}, BallTime: {3}, TimeNow: {4}", 
          //          ballPoolNum, fDistBall, ballMaxDistance, pooledBalls[ballPoolNum].timeSpawn, Time.fixedTime));
                        break;
                    }
                }
                //not old enough or too close
                ballPoolNum++;
            }
            while (ballPoolNum != ballPoolLast);
            
            //if we’ve run out of objects in the pool too quickly, create a new one
            if (ballPoolNum==ballPoolLast && pooledBalls[ballPoolNum].obj.activeInHierarchy)
            {
                CreatePooledBalls();
            }
            ballPoolLast = ballPoolNum;
            pooledBalls[ballPoolNum].timeSpawn = Time.fixedTime;
            return pooledBalls[ballPoolNum].obj;
        }
    }

    protected void SpawnBall()
    {
        GameObject selectedBall = BallSpawner.current.GetPooledBall(gameManager.GameRunning());
        selectedBall.transform.position = transform.position;
        Rigidbody selectedRigidbody = selectedBall.GetComponent<Rigidbody>();
        selectedRigidbody.velocity = Vector3.zero;
        selectedRigidbody.angularVelocity = Vector3.zero;
        selectedBall.SetActive(gameManager.GameRunning());
    }
}
