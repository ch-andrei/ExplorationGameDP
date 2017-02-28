using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Voronoi2;

using Tiles;

namespace Meshes {

    // DEPRECATED
    public class HexRegionMesh {
        public static void InitializeMesh(ViewableRegion region, Mesh mesh) {
            List<Tile> tiles = region.getViewableTiles();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            Vector3[] verticesLocal;
            for (int i = 0; i < tiles.Count; i++) {
                verticesLocal = tiles.ElementAt(i).getGeometry();
                // copy vertice vectors
                foreach (Vector3 triangle in Hexagon.topTriangles) {
                    MeshGenerator.addTriangle(triangle, verticesLocal, vertices, triangles);
                }
                foreach (Vector3 triangle in Hexagon.sidesTriangles) {
                    MeshGenerator.addTriangle(triangle, verticesLocal, vertices, triangles);
                }
            }
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
        }
    }

    // DEPRECATED
    public class HexTileMesh {
        public static void InitializeMesh(Tile tile, MeshFilter meshFilter) {
            Mesh mesh = meshFilter.sharedMesh;
            // compute mesh parameters
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();

            List<Vector2> uvs = new List<Vector2>();

            List<int> topTris = new List<int>();
            List<int> sidesTris = new List<int>();

            Vector3[] verticesLocal = tile.getGeometry();

            // copy vertice vectors
            foreach (Vector3 triangle in Hexagon.topTriangles) {
                MeshGenerator.addTriangle(triangle, verticesLocal, vertices, topTris, normals, Hexagon.uvTop.ToList(), uvs);
            }
            foreach (Vector3 triangle in Hexagon.sidesTriangles) {
                MeshGenerator.addTriangle(triangle, verticesLocal, vertices, sidesTris, normals, Hexagon.uvSides.ToList(), uvs, topTris.Count);
            }

            // set up mesh
            mesh = new Mesh();

            mesh.Clear();
            mesh.subMeshCount = 2;

            mesh.SetVertices(vertices);

            mesh.SetTriangles(topTris.ToArray(), 0);
            mesh.SetTriangles(sidesTris.ToArray(), 1);

            mesh.SetNormals(normals);

            mesh.SetUVs(0, uvs);

            mesh.RecalculateBounds();

            // assign back to meshFilter
            meshFilter.mesh = mesh;
        }
    }

    // DEPRECATED 
    public class WaterMesh {
        public static void InitializeMesh(ViewableRegion region, VoronoiGraph vG, Mesh mesh, Noise noise, int maxWaterLevelDisplacement) {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            float water_elevation = region.getWaterLevelElevation();

            Vector3[] verticesLocal;
            Vector3[] trianglesLocal = new Vector3[] { new Vector3(0,1,2), new Vector3(0,1,3) };

            SiteSorterSiteNBR sorter = new SiteSorterSiteNBR();
            int count = 0;
            foreach (GraphEdge edge in vG.edges) {
                // locate voronoi sites for the edge
                Site site1, site2;
                site1 = vG.sites[sorter.searchSites(vG.sites, edge.site1)];
                site2 = vG.sites[sorter.searchSites(vG.sites, edge.site2)];
                // compute vertex coordinates
                verticesLocal = new Vector3[] {
                    new Vector3((float)edge.x1, water_elevation, (float)edge.y1),
                    new Vector3((float)edge.x2, water_elevation, (float)edge.y2),
                    new Vector3((float)site1.coord.x - region.getViewableSize()/2, water_elevation, (float)site1.coord.y - region.getViewableSize()/2),
                    new Vector3((float)site2.coord.x - region.getViewableSize()/2, water_elevation, (float)site2.coord.y - region.getViewableSize()/2)
                };
                // add noisy elevation to each vertex
                for (int i = 0; i < verticesLocal.Length; i++) {
                    Vector3 v = verticesLocal[i];
                    float val = noise.getNoiseValueAt((int)v.x + region.getViewableSize() / 2, (int)v.z + region.getViewableSize() / 2, (int)(region.getViewableSize()*1.5f));
                    v.y += (-0.5f + val) * maxWaterLevelDisplacement;
                    verticesLocal[i] = v;
                }
                // add triangles to the mesh 
                foreach (Vector3 triangle in trianglesLocal) {
                    MeshGenerator.addTriangle(triangle, verticesLocal, vertices, triangles);
                    count++;
                }
            }
            // set mesh
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            //Debug.Log("Initialized water mesh with " + count + " triangles.");
        }
    }

    public class MeshGenerator {
        // adds a triangle to the mesh
        public static void addTriangle(Vector3 triangle, Vector3[] verticesLocal, List<Vector3> vertices, List<int> triangles, bool reordered=true) {
            // adds vertices clockwise along x, z plane 
            List<Vector3> temp = new List<Vector3>();
            temp.Add(verticesLocal[(int)triangle.x]);
            temp.Add(verticesLocal[(int)triangle.y]);
            temp.Add(verticesLocal[(int)triangle.z]);

            if (reordered) {
                // compute central location
                Vector3 center = (temp[0] + temp[1] + temp[2]) / 3;
                // sort vectors by angle, clockwise so that the normal point outside
                temp = temp.OrderBy(o => (-Mathf.Atan((o.x - center.x) / (o.z - center.z)))).ToList();
            }

            // add triangle
            foreach (Vector3 v in temp) {
                vertices.Add(v);
                triangles.Add(triangles.Count);
            }
        }

        private class VertexData {
            public Vector3 v { get; set; }
            public Vector2 uv { get; set; }
            public VertexData(Vector3 v, Vector2 uv) {
                this.v = v;
                this.uv = uv;
            }
        }

        // adds a triangle to the mesh
        public static void addTriangle(Vector3 triangle, Vector3[] verticesLocal, List<Vector3> vertices, List<int> triangles,
                                            List<Vector3> normals, List<Vector2> uvsLocal, List<Vector2> uvs, int triangleOffset = 0) {

            Vector3 hexCenter = verticesLocal[0];

            // adds vertices clockwise along x, z plane 
            List<VertexData> verts = new List<VertexData>();

            verts.Add(new VertexData(verticesLocal[(int)triangle.x], uvsLocal[(int)triangle.x]) );
            verts.Add(new VertexData(verticesLocal[(int)triangle.y], uvsLocal[(int)triangle.y]));
            verts.Add(new VertexData(verticesLocal[(int)triangle.z], uvsLocal[(int)triangle.z]));

            // compute central location
            Vector3 center = (verts[0].v + verts[1].v + verts[2].v) / 3;
            hexCenter.y = center.y;

            // sort vectors by angle, clockwise so that the normal point outside
            verts = verts.OrderBy(o => (-Mathf.Atan((o.v.x - center.x) / (o.v.z - center.z)))).ToList();

            Vector3 normal = Vector3.Cross(verts[1].v - verts[0].v, verts[2].v - verts[0].v).normalized;

            // add triangle
            foreach (VertexData v in verts) {
                vertices.Add(v.v);
                normals.Add(normal);
                triangles.Add(triangleOffset + triangles.Count);
                uvs.Add(v.uv);
            }
        }
    }
}
