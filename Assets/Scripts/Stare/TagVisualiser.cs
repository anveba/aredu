using UnityEngine;

public class TagVisualiser : System.IDisposable
{
    private Mesh _mesh;
    private Material _material;

    public TagVisualiser(Material material)
    {
        _mesh = BuildMesh();
        _material = material;
    }

    public void Dispose()
    {
        Object.Destroy(_mesh);
    }

    public void Draw(Vector3 position, Quaternion rotation, float scale)
    {
        Matrix4x4 transform = Matrix4x4.TRS(position, rotation, Vector3.one * scale);
        Graphics.DrawMesh(_mesh, transform, _material, 0);
    }

    private static Mesh BuildMesh()
    {
        Mesh mesh = new Mesh();

        float crossLength = 0.5f;
        float normalLength = 2.0f;

        mesh.vertices = new Vector3[] {
            new Vector3(-crossLength, 0, 0),
            new Vector3(crossLength, 0, 0),
            new Vector3(0, -crossLength, 0),
            new Vector3(0, crossLength, 0),
            new Vector3(0, 0, 0),
            new Vector3(0, 0, -normalLength)
        };

        int[] indices = new int[] { 0, 1, 2, 3, 4, 5 };
        mesh.SetIndices(indices, MeshTopology.Lines, 0);

        return mesh;
    }
}
