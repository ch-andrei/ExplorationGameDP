using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Voronoi2;
using System.Linq;
using Meshes;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class WaterOverlay : MonoBehaviour {
    
    [Range(5,10000)]
    public int waterPolygonCount;

    [Range(0, 100)]
    public int minDist;

    [Range(0, 400)]
    public int noiseResolution;

    public float noiseAmplitude;

    public float noisePersistance;

    [Range(0, 50)]
    public int maxWaterLevelDisplacement;

    public int seed;
    public bool useRandomSeed;

    private ViewableRegion region;

    private int water_elevation;

    private int size;

    private Voronoi voronoi;

    double[] X, Y;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    Mesh mesh;
    MeshCollider collider;

    bool generated = false;

    void Awake() {
    }

    // Use this for initialization
    void Start () {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;
        collider = GetComponent<MeshCollider>();

        mesh.Clear();
        //meshRenderer.material.SetTexture("_BumpMap", Resources.Load("Textures/PolygonNormal", typeof(Texture)) as Texture);
        collider.sharedMesh = mesh;
    }
	
	// Update is called once per frame
	void Update () {
        if ((GameControl.gameSession.mapGenerator.getRegion()) != null && !generated) {
            if (regenerateVoronoi(true))
                generated = true;
        } 
    }

    public bool regenerateVoronoi(bool drawmesh) {
        if (GameControl.gameSession == null || (region = GameControl.gameSession.mapGenerator.getRegion()) == null) {
            //Debug.Log("Cannot regenerate Voronoi for Water - gameSession is not ready.");
            return false;
        }

        // prepare noise
        int noise_seed = (useRandomSeed) ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : this.seed;
        Noise noise = new FastPerlinNoise(noiseResolution, noise_seed, noiseAmplitude, noisePersistance);
        noise.setNoiseValues(NoiseMap.adjustNoise(noise.getNoiseValues(), 5));

        // get region parameters
        this.water_elevation = region.getWaterLevelElevation();
        this.size = region.getViewableSize();
        int halfSize = this.size / 2;

        // generate random coordinates for polygons
        generateXY(out this.X, out this.Y);

        // use voronoi generator
        voronoi = new Voronoi(minDist);
        voronoi.generateVoronoi(X, Y, 0, size, 0, size);

        VoronoiGraph vG = voronoi.getVoronoiGraph();
        foreach (GraphEdge gE in vG.edges) {
            gE.x1 -= halfSize;
            gE.x2 -= halfSize;
            gE.y1 -= halfSize;
            gE.y2 -= halfSize;
        }

        if (drawmesh) {
            Meshes.WaterMesh.InitializeMesh(region, voronoi.getVoronoiGraph(), mesh, noise, maxWaterLevelDisplacement);
        }
        return true;
    }

    private void generateXY(out double[] X, out double[] Y) {
        List<Point> points = new List<Point>();
        for (int i = 0; i < waterPolygonCount; i++) {
            // must ensure uniqueness of the points else voronoi is messy
            Point p = new Point();
            int x = UnityEngine.Random.Range(0, size),
                y = UnityEngine.Random.Range(0, size);
            p.setPoint(x, y);
            points.Add(p);
        }
        removeDuplicates(points);
        X = new double[points.Count];
        Y = new double[points.Count];
        for (int i = 0; i < points.Count; i++) {
            Point p = points.ElementAt(i);
            X[i] = p.x;
            Y[i] = p.y;
        }
        Debug.Log("Asked for " + waterPolygonCount + " points, made " + points.Count + " points.");
    }

    private void removeDuplicates(List<Point> points) {
        points.Sort(delegate (Point a, Point b)
        {
            int xdiff = a.x.CompareTo(b.x);
            if (xdiff != 0) return xdiff;
            else return a.y.CompareTo(b.y);
        });
        for (int i = 0; i < points.Count; i++) {
            Point p = points.ElementAt(i);
            // remove duplicate points
            for (int j = i + 1; j < points.Count; j++) {
                Point p2 = points.ElementAt(j);
                if (p.x == p2.x && p.y == p2.y) {
                    points.RemoveAt(j);
                    j--;
                }
            }
        }
    }

    private void OnDrawGizmos() {
        if (region == null || voronoi == null) {
            regenerateVoronoi(false);
            return;
        }
        foreach (GraphEdge gE in voronoi.getVoronoiGraph().edges) {
            Gizmos.DrawLine(new Vector3((float)gE.x1, this.water_elevation, (float)gE.y1), new Vector3((float)gE.x2, this.water_elevation, (float)gE.y2));
        }
    }
}
