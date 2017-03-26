using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudsView : MonoBehaviour {

    // cloud spawn parameter constants
    public static int numberOfCloudsToSpawn = 50; // maximum number of clouds to spawn
    public static int framesBetweenCloudSpawns = 2; // dont spawn clouds every frame to lower strain on the system
    public static float cloudToCameraDistanceToDespawn = MapView.GlobalDrawDistance; // despawn clouds outside this view distance from camera
    public static float cloudSpawnMaxDistanceFromViewPoint = Mathf.Sqrt(Mathf.Pow(cloudToCameraDistanceToDespawn, 2)); // spawn within this width x width area
    public static float cloudSpawnMaxDistanceFromElevationLevel = 25f;
    public static float cloudSpawnElevationToMaxRegionElevationRatio = 0.75f;

    // cloud movement parameter constants
    public static float cloudMoveSpeed = 0.15f;
    public static Vector3 cloudMoveDirectionNormalized = new Vector3(1,0,1).normalized;

    // private variables
    private int numberOfCloudsSpawned;
    private float cloudElevationLevel;
    List<Cloud> clouds;
    Vector3 cameraViewCenterPoint;

    private int framesSinceLastSpawn;

    public class Cloud{
        // cloud dimension and offsets constants
        public static float cloudMaxCenterOffsetHorizontal = 15f;
        public static float cloudMinCenterOffsetHorizontal = 5f;

        public static float cloudMaxCenterOffsetVertical = 7f;
        public static float cloudMinCenterOffsetVertical = 2f;

        public static float cloudMaxObjectSizeVertical = 5f;
        public static float cloudMinObjectSizeVertical = 2f;

        public static float cloudMaxObjectSizeHorizontal = 15f;
        public static float cloudMinObjectSizeHorizontal = 3f;
        // cloud object counts
        public static int maxObjectsPerCloud = 20;
        public static int minObjectsPerCloud = 5;

        // private variables //

        public int objectsPerCloud { get; }

        public GameObject cloudCenter;
        public Vector3 cloudCenterPos { get { return this.cloudCenter.transform.position; } }

        public Cloud(Transform parent, Vector3 spawnPos) {
            objectsPerCloud = (int)UnityEngine.Random.Range(minObjectsPerCloud, maxObjectsPerCloud);
            cloudCenter = new GameObject("Cloud");
            cloudCenter.transform.parent = parent;
            cloudCenter.transform.position = spawnPos;
            generateCloud(parent);
        }

        private void generateCloud(Transform parent) {
            for (int i = 0; i < this.objectsPerCloud; i++) {
                GameObject cloudObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cloudObject.transform.parent = cloudCenter.transform;
                cloudObject.transform.localPosition = 
                                        new Vector3(
                                            Random.Range(cloudMinCenterOffsetHorizontal, cloudMaxCenterOffsetHorizontal),
                                            Random.Range(cloudMinCenterOffsetVertical, cloudMaxCenterOffsetVertical),
                                            Random.Range(cloudMinCenterOffsetHorizontal, cloudMaxCenterOffsetHorizontal)
                                                    );
                cloudObject.transform.localScale = new Vector3(Random.Range(cloudMinObjectSizeHorizontal, cloudMaxObjectSizeHorizontal),
                                                Random.Range(cloudMinObjectSizeVertical, cloudMaxObjectSizeVertical),
                                                Random.Range(cloudMinObjectSizeHorizontal, cloudMaxObjectSizeHorizontal));
            }
        }

        public void tick(float moveSpeed, Vector3 moveDirection) {
            move(moveSpeed, moveDirection);
        }

        public void despawn() {
            Destroy(cloudCenter);
        }

        private void move(float moveSpeed, Vector3 moveDirection) {
            cloudCenter.transform.position += moveDirection * moveSpeed;
        }
    }

	// Use this for initialization
	void Start () {
        cloudElevationLevel = GameControl.gameSession.mapGenerator.getRegion().getMaximumElevation() * cloudSpawnElevationToMaxRegionElevationRatio;
        clouds = new List<Cloud>();
        cameraViewCenterPoint = new Vector3();
        numberOfCloudsSpawned = 0;
        framesSinceLastSpawn = 0;
    }
	
	// Update is called once per frame
	void Update () {

        // update camera position
        cameraViewCenterPoint = CameraControl.viewCenterPoint;
        cameraViewCenterPoint.y = 0;

        if (numberOfCloudsSpawned < numberOfCloudsToSpawn && framesSinceLastSpawn > framesBetweenCloudSpawns) {
            // spawn cloud
            spawnCloud();
            numberOfCloudsSpawned++;
            framesSinceLastSpawn = 0;
        }

        framesSinceLastSpawn++;

        // update clouds
        foreach (Cloud cloud in clouds) {
            // update cloud tick
            cloud.tick(cloudMoveSpeed, cloudMoveDirectionNormalized);
        }

        // despawn old clouds
        for (int i = 0; i < clouds.Count; i++) {
            Cloud cloud = clouds[i];
            Vector3 posNoY = cloud.cloudCenterPos;
            posNoY.y = 0;
            if ((cameraViewCenterPoint - posNoY).magnitude > cloudToCameraDistanceToDespawn) {
                despawnCloud(cloud);
                clouds.RemoveAt(i);
                i--;
            }
        }
    }

    private void spawnCloud() {
        // compute new cloud position
        Vector3 cloudPos = new Vector3(
            Random.Range(-cloudSpawnMaxDistanceFromViewPoint, cloudSpawnMaxDistanceFromViewPoint), 
            cloudElevationLevel + Random.Range(-cloudSpawnMaxDistanceFromElevationLevel, cloudSpawnMaxDistanceFromElevationLevel), 
            Random.Range(-cloudSpawnMaxDistanceFromViewPoint, cloudSpawnMaxDistanceFromViewPoint)
            );
        // create a cloud and add to the list of clouds
        Cloud cloud = new Cloud(transform, cameraViewCenterPoint + cloudPos);
        clouds.Add(cloud);
    }

    private void despawnCloud(Cloud cloud) {
        cloud.despawn();
        numberOfCloudsSpawned--;
    }
}
