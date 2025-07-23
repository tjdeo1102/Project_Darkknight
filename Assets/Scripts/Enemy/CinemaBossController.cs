using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

public class CinemaBossController : MonoBehaviour
{
    public PlayableDirector director;
    private InGameLoop m_gameLoop;

    void OnEnable()
    {
        m_gameLoop = InGameLoop.Instance;
        m_gameLoop?.PlayerPause(true);
        if (director != null)
        {
            director.stopped += OnTimelineStopped;
            foreach (var output in director.playableAsset.outputs)
            {
                if (output.streamName.Contains("Cinemachine"))
                {
                    director.SetGenericBinding(output.sourceObject, Camera.main.GetComponent<CinemachineBrain>());
                    break;
                }
            }
            director.Play();
        }
    }

    void OnDisable()
    {
        if (director != null)
        {
            director.stopped -= OnTimelineStopped;
        }
    }

    private void OnTimelineStopped(PlayableDirector dir)
    {
        m_gameLoop?.PlayerPause(false);
        Destroy(gameObject);
    }
}
