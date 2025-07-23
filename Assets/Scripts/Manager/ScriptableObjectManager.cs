using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ScriptableObjectManager : ManagerBase<ScriptableObjectManager>
{
    [SerializeField]
    private List<CSVScriptableObject> csvSO = new();

    private async void Start()
    {
        AsyncOperationHandle<IList<CSVScriptableObject>> handle =
            Addressables.LoadAssetsAsync<CSVScriptableObject>("SOManager", null);

        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            csvSO.AddRange(handle.Result);

            foreach (var so in csvSO)
            {
                so.Backup();
            }

            IsReady = true;
        }
        else
        {
            Debug.LogError("Failed to load");
        }
    }

    private void OnApplicationQuit()
    {
        foreach (var item in csvSO)
        {
            item.Restore();
        }
    }
}
