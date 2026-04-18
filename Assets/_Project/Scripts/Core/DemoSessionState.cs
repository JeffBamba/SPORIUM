using System;

namespace _Project.Sporae.Core
{
    /// <summary>
    /// Stato runtime della sessione demo Alpha (non persistito come save dedicato).
    /// Registrato nel <see cref="ServiceContainer"/> da <see cref="Installers.GamePlayInstaller"/>.
    /// </summary>
    public sealed class DemoSessionState
    {
        /// <summary>
        /// Impostare a true dal menu principale prima di <c>LoadScene</c> verso la scena di gioco;
        /// consumato alla registrazione del servizio.
        /// </summary>
        public static bool StartNextSessionAsDemo { get; set; }

        public bool IsDemo { get; set; }

        /// <summary>Beat narrativo corrente (1–8 nel piano Alpha no-spoiler).</summary>
        public int CurrentBeat { get; set; }

        public bool DemoCompleted { get; set; }

        public event Action<int> BeatChanged;

        public void SetBeat(int beat)
        {
            if (beat == CurrentBeat)
                return;
            CurrentBeat = beat;
            BeatChanged?.Invoke(CurrentBeat);
        }
    }
}
