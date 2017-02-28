using UnityEngine;

public class Hexagon {

    // gap around the edges
    public static float hexScaleFactor = 1f;

    /* flat topped hexagon.
    /* 3d hexagon vertices:

    *top surface*

      6   1
    5   0   2
    | 4   3 |
    |       |
    |       |
    | 12  7 |
    11      8
      10  9

    *bottom surface*

    hardcoded triangles 
    */
    public static readonly Vector3[] topTriangles = new Vector3[]
    {
        // top
        new Vector3(0,6,1 ),
        new Vector3(0,1,2 ),
        new Vector3(0,2,3 ),
        new Vector3(0,3,4 ),
        new Vector3(0,4,5 ),
        new Vector3(0,5,6 )
    };

    public static readonly Vector2[] uvTop = new Vector2[]
    {
        // top
        new Vector2(0.5f, 0.5f),
        new Vector2(0.75f, 1f),
        new Vector2(1f, 0.5f),
        new Vector2(0.75f, 0),
        new Vector2(0.25f, 0),
        new Vector2(0, 0.5f),
        new Vector2(0.25f, 1)
    };

    public static readonly Vector3[] sidesTriangles = new Vector3[]
    {
        // sides
        new Vector3(6,12,7),
        new Vector3(6,7,1 ),
        new Vector3(1,7,8 ),
        new Vector3(1,8,2 ),
        new Vector3(2,8,9 ),
        new Vector3(2,9,3 ),
        new Vector3(3,9,10),
        new Vector3(3,10,4),
        new Vector3(4,10,11),
        new Vector3(4,11,5),
        new Vector3(5,11,12),
        new Vector3(5,12,6)
    };

    public static readonly Vector2[] uvSides = new Vector2[]
{
        // center
        new Vector2(0, 0), // doesn't matter
        // top
        new Vector2(0, 1),
        new Vector2(1, 1),
        new Vector2(0, 1),
        new Vector2(1, 1),
        new Vector2(0, 1),
        new Vector2(1, 1),
        // sides
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(0, 0),
        new Vector2(1, 0)
};

    static int numVertices = 13;
    private Vector3[] vertices;

    public Hexagon(Vector3 central, float height, float size) {
        vertices = new Vector3[numVertices];
        initializeVertices(central, hexScaleFactor * height, hexScaleFactor * size);
    }

    public Vector3[] getVertices() {
        return this.vertices;
    }

    private void initializeVertices(Vector3 central, float height, float size) {
        vertices[0] = central;
        float halfSize = size / 2f;
        // top surface
        vertices[1] = new Vector3(central.x + halfSize, central.y, central.z + height); // top right
        vertices[2] = new Vector3(central.x + size, central.y, central.z); // right;
        vertices[3] = new Vector3(central.x + halfSize, central.y, central.z - height); // bottom right;
        vertices[4] = new Vector3(central.x - halfSize, central.y, central.z - height); // bottom left;
        vertices[5] = new Vector3(central.x - size, central.y, central.z); // left;
        vertices[6] = new Vector3(central.x - halfSize, central.y, central.z + height); // top left;
        // bottom surface
        vertices[7] = new Vector3(central.x + halfSize, 0, central.z + height); // top right
        vertices[8] = new Vector3(central.x + size, 0, central.z); // right;
        vertices[9] = new Vector3(central.x + halfSize, 0, central.z - height); // bottom right;
        vertices[10] = new Vector3(central.x - halfSize, 0, central.z - height); // bottom left;
        vertices[11] = new Vector3(central.x - size, 0, central.z); // left;
        vertices[12] = new Vector3(central.x - halfSize, 0, central.z + height); // top lef;
    }
}