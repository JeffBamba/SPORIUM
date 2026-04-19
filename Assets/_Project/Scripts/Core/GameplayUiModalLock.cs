namespace _Project.Sporae.Core
{
    /// <summary>
    /// Blocca input di movimento mondo (WASD / click-to-move) mentre un pannello HUD modale è aperto (es. Armadio).
    /// </summary>
    public static class GameplayUiModalLock
    {
        public static bool BlocksWorldInput { get; private set; }

        public static void SetBlockWorldInput(bool block) => BlocksWorldInput = block;
    }
}
