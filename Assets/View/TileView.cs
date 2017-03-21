using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TileAttributes;
using Tiles;

namespace TileViews {

    [System.Serializable]
    public class TileViewInitParams {
        public float maxDistance;
        public float clusterSpace;
        public int clusters;
        public int minTreesPerCluster;
        public int maxTreesPerCluster;

        public TileViewInitParams() {
        }
    }

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class TileView : MonoBehaviour {
        // reference to tile the TileView will display
        public Tile tile { get; set; }

        List<GameObject> tileDecorations;

        void Start() {
        }

        void Update() {
            // TODO
        }

        // init mesh and instantiate
        public void InitializeTileViewObject(Tile tile, TileViewInitParams tvip) {
            this.tile = tile;
            tileDecorations = new List<GameObject>();

            float elevation = tile.elevationToWater;

            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            MeshCollider collider = gameObject.GetComponent<MeshCollider>();

            Material[] materials = meshRenderer.materials;

            Color c = Utilities.hexToColor("#87d0ff");

            if (tile.getTileType().GetType() == typeof(WaterTileType)) {
                //(Resources.Load("Materials/WaterTile", typeof(Material)) as Material).CopyPropertiesFromMaterial(material);
                // tiling
                //material.SetTextureScale("_MainTex", new Vector2(1f, 1f));
                if (elevation > -5) {
                    c = Utilities.hexToColor("#004176");
                } else if (elevation > -10) {
                    c = Utilities.hexToColor("#004176");
                } else if (elevation > -25) {
                    c = Utilities.hexToColor("#004176");
                } else if (elevation > -50) {
                    c = Utilities.hexToColor("#004176");
                } else
                    c = Utilities.hexToColor("#004176");
                materials[0].CopyPropertiesFromMaterial(Resources.Load("Materials/WaterTile", typeof(Material)) as Material);
                materials[1].CopyPropertiesFromMaterial(Resources.Load("Materials/WaterTile", typeof(Material)) as Material);
                materials[0].SetColor("_Color", c);
                materials[1].SetColor("_Color", c);
            } else if (tile.getTileType().GetType() == typeof(LandTileType)) {
                // tiling
                //material.SetTextureScale("_MainTex", new Vector2(1f, 1f));

                materials[0].CopyPropertiesFromMaterial(Resources.Load("Materials/NonShiny", typeof(Material)) as Material);
                materials[1].CopyPropertiesFromMaterial(Resources.Load("Materials/NonShiny", typeof(Material)) as Material);


                if (tile.temperature < 0)
                    c = Utilities.hexToColor("#eeeeee");
                else if (tile.temperature < 5)
                    c = Utilities.hexToColor("#dae2ef");
                else if (tile.temperature < 10)
                    c = Utilities.hexToColor("#a5916d");
                else if (tile.temperature < 15)
                    c = Utilities.hexToColor("#61a339");
                else if (tile.temperature < 20)
                    c = Utilities.hexToColor("#66d345");
                else if (tile.temperature < 30)
                    c = Utilities.hexToColor("#64ed3b");
                else
                    c = Utilities.hexToColor("#edef7a");

                //material.SetTexture("_MainTex", Resources.Load("Textures/TileTexture", typeof(Texture)) as Texture);
                materials[0].SetColor("_Color", c);

                materials[1].SetTextureScale("_MainTex", new Vector2(1f, 5f));
                materials[1].SetTextureScale("_BumpMap", new Vector2(1f, 5f));
                materials[1].SetFloat("_BumpScale", 0.5f);
                materials[1].SetColor("_MainTex", Utilities.hexToColor("#c1bfae"));
            } else {
                materials[0] = Resources.Load("Materials/Grass", typeof(Material)) as Material;
                materials[1] = Resources.Load("Materials/Grass", typeof(Material)) as Material;
            }

            foreach (TileAttribute tileAttribute in tile.getTileAttributes()) {

                // each TileAttribute subtype can be displayed differently

                // FORESTRY TileAttribute
                if (tileAttribute.GetType() == typeof(Forestry)) {
                    // params for deploying trees
                    int clusterCount = (int)(tvip.clusters * ((Forestry)tileAttribute).forestryDensity);
                    for (int i = 0; i < clusterCount; i++) {
                        // compute
                        int countTrees = (int)Random.Range(tvip.minTreesPerCluster, tvip.maxTreesPerCluster);

                        float temperatureFactor = 1f - tile.temperature / (RegionParams.worldAmbientTemperature);
                        int coldTrees = (int)(temperatureFactor * countTrees);

                        // randomize prefab trees
                        string[] prefabNames = new string[countTrees];
                        for (int j = 0; j < countTrees; j++) {
                            // index for probability

                            // NOTE: not using broadleaf because of misplaced models (offset from anchor point)

                            // broadleaf
                            //if (index < elevationFactor / 2f) { // first half of under elevation
                            //    prefabNames[i] = MapView.treesBroadleaf[Random.Range(0, MapView.treesBroadleaf.Length)];
                            //}

                            // randompoly
                            if (j < coldTrees) { // second half of under elevation
                                if (temperatureFactor > 0.9f)
                                    prefabNames[j] = MapView.treesConifersSnowy[Random.Range(0, MapView.treesConifersSnowy.Length)];
                                else
                                    prefabNames[j] = MapView.treesConifers[Random.Range(0, MapView.treesConifers.Length)];
                            }
                            // conifers
                            else {
                                //if (temperatureFactor > 0.9f)
                                //    prefabNames[j] = MapView.treesConifers[Random.Range(0, MapView.treesConifers.Length)];
                                //else
                                prefabNames[j] = MapView.treesRandompoly[Random.Range(0, MapView.treesRandompoly.Length)];
                            }

                        }

                        // randomize tree cluster position
                        float clusterX, clusterY;
                        clusterX = Random.Range(-tvip.maxDistance, tvip.maxDistance);
                        clusterY = Random.Range(-tvip.maxDistance, tvip.maxDistance);
                        Vector3 clusterV = new Vector3(clusterX, 0, clusterY);

                        // instantiate prefabs
                        foreach (string prefabName in prefabNames) {
                            if (prefabName.Equals(""))
                                continue;

                            GameObject tree = Instantiate(Resources.Load("Prefabs/Trees/" + prefabName), this.transform.parent) as GameObject;

                            // backup tree transform to preserve pos and scale offsets
                            Transform t = tree.transform;

                            if (tree == null)
                                continue;

                            // compute tree offset
                            float rX, rZ;
                            rX = Random.Range(-tvip.clusterSpace, tvip.clusterSpace);
                            rZ = Random.Range(-tvip.clusterSpace, tvip.clusterSpace);
                            Vector3 rV = new Vector3(rX, 0, rZ);

                            tree.transform.position = tile.getPos() + rV + clusterV + t.position;

                            tree.transform.parent = this.transform;

                            // preserve prefab scale
                            tree.transform.localScale = t.localScale;

                            float randRotation = Random.Range(0f, 360f);
                            tree.transform.Rotate(0, randRotation, 0);

                            tileDecorations.Add(tree);
                        }
                    }
                }


                // LocalTribe TileAttribute
                if (tileAttribute.GetType() == typeof(LocalTribe)) {
                    string prefabName = "Fancy_Tavern";
                    GameObject village = Instantiate(Resources.Load("Prefabs/Buildings/" + prefabName), this.transform.parent) as GameObject;

                    // backup tree transform to preserve pos and scale offsets
                    Transform t = village.transform;

                    if (village == null)
                        continue;

                    // compute offset
                    float rX, rZ;
                    rX = Random.Range(-tvip.maxDistance, tvip.maxDistance);
                    rZ = Random.Range(-tvip.maxDistance, tvip.maxDistance);
                    Vector3 rV = new Vector3(rX, 0, rZ);

                    village.transform.position = tile.getPos() + rV + t.position;

                    village.transform.parent = this.transform;

                    // preserve prefab scale
                    village.transform.localScale = t.localScale;

                    float randRotation = Random.Range(0f, 360f);
                    village.transform.Rotate(0, randRotation, 0);

                    tileDecorations.Add(village);
                }
            }

            meshRenderer.materials = materials;

            InitializeTileMesh(tile, meshFilter, collider);
        }

        public void InitializeTileMesh(Tile tile, MeshFilter meshFilter, MeshCollider collider) {
            //Meshes.HexTileMesh.InitializeMesh(tile, meshFilter);
            collider.sharedMesh = meshFilter.sharedMesh;
        }
    }
}