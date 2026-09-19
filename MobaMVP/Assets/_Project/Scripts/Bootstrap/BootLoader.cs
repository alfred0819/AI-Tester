using UnityEngine;
using UnityEngine.SceneManagement;
using Moba.Core;

namespace Moba.Bootstrap
{
    /// <summary>Entry scene: jump straight to Lobby.</summary>
    public sealed class BootLoader : MonoBehaviour
    {
        [SerializeField] private float _delaySeconds = 0.1f;

        private void Start()
        {
            Invoke(nameof(LoadLobby), _delaySeconds);
        }

        private void LoadLobby()
        {
            SceneManager.LoadScene(SceneNames.Lobby);
        }
    }
}
