namespace _Project.Sporae.Core
{
    /// <summary>
    /// Richiede un fade-in da nero all'ingresso in VaultMap (Nuova partita / Gioca demo).
    /// Impostato dal menu prima di <see cref="UnityEngine.SceneManagement.SceneManager.LoadSceneAsync"/>.
    /// </summary>
    public static class VaultMapEntryFade
    {
        public static bool RequestFadeInOnNextLoad { get; set; }
    }
}
