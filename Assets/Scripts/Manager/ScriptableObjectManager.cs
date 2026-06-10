using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ScriptableObjectManager : ManagerBase<ScriptableObjectManager>
{
    [SerializeField]
    private List<CSVScriptableObject> csvSO = new();

    private AsyncOperationHandle<IList<CSVScriptableObject>> m_handle;
    private bool m_isDestroyed;
    private bool m_hasBackups;
    private bool m_isReleased;

    private async void Start()
    {
        m_handle = Addressables.LoadAssetsAsync<CSVScriptableObject>("SOManager", null);

        await m_handle.Task;
        if (m_isDestroyed) return;

        if (m_handle.Status == AsyncOperationStatus.Succeeded)
        {
            csvSO.AddRange(m_handle.Result);

            foreach (var so in csvSO)
            {
                so.Backup();
            }

            m_hasBackups = true;
            IsReady = true;
        }
        else
        {
            Debug.LogError("Failed to load");
        }
    }

    private void OnApplicationQuit()
    {
        RestoreAndRelease();
    }

    private void OnDestroy()
    {
        m_isDestroyed = true;
        RestoreAndRelease();
    }

    private void RestoreAndRelease()
    {
        if (m_hasBackups)
        {
            foreach (var item in csvSO)
            {
                item.Restore();
            }

            m_hasBackups = false;
        }

        if (m_isReleased || m_handle.IsValid() == false) return;

        Addressables.Release(m_handle);
        m_isReleased = true;
    }
}
