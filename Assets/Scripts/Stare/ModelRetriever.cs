using UnityEngine;
using GLTFast;
using System.Threading.Tasks;
using System;

public class ModelRetriever : MonoBehaviour
{
    [SerializeField] Transform _assetParent;
    private GltfImport _importer;

    private void Start()
    {
        _importer = new GltfImport();
    }

    public async Task<Tuple<GameObject, string>> Retrieve(ModelBundle models)
    {
        GameObject parent = new GameObject(models.Name);
        parent.transform.parent = _assetParent;

        Task<Tuple<GameObject, string>>[] tasks = new Task<Tuple<GameObject, string>>[models.Models.Length];

        string combinedError = null;

        for (int i = 0; i < models.Models.Length; i++)
            tasks[i] = Retrieve(models.Models[i]);

        for (int i = 0; i < models.Models.Length; i++)
        {
            (GameObject obj, string error) = await tasks[i];
            if (obj == null)
            {
                combinedError = combinedError == null ? error : combinedError + "\n" + error;   
                continue;
            }
            obj.transform.parent = parent.transform;
        }
        return new (parent, combinedError);
    }


    public async Task<Tuple<GameObject, string>> Retrieve(Model model)
    {
        // TODO caching

        var settings = new ImportSettings
        {
            GenerateMipMaps = true,
            AnisotropicFilterLevel = 3,
            NodeNameMethod = NameImportMethod.OriginalUnique
        };
        bool success = await _importer.Load(new Uri(model.Uri), settings);

        if (!success)
        {
            return new (null, "Failed to retrieve model " + model.Name + " at " + model.Uri);
        }
        else
        {
            GameObject asset = new GameObject(model.Name);
            asset.transform.parent = _assetParent;
            asset.transform.localScale = model.Scale;
            asset.transform.localPosition = Vector3.zero;
            asset.transform.localRotation = Quaternion.identity;
            await _importer.InstantiateMainSceneAsync(asset.transform);
            return new (asset, null);
        }
    }
}