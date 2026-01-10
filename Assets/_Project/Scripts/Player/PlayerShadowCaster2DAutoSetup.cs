using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _Project.Player
{
    /// <summary>
    /// Ensures the Player has a ShadowCaster2D configured for URP 2D lights.
    /// Non-destructive: does not require prefab edits (attach this script to the Player in scene/prefab).
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerShadowCaster2DAutoSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupOnAwake = true;

        [Tooltip("If true, the ShadowCaster2D is added to the same GameObject that holds the SpriteRenderer (recommended).")]
        [SerializeField] private bool attachToSpriteRendererGameObject = true;

        [Tooltip("If true, tries to configure the ShadowCaster2D to use the renderer silhouette (stable, no manual shape editing).")]
        [SerializeField] private bool tryUseRendererSilhouette = true;

        [Tooltip("If true, the caster will also cast shadow onto itself (usually OFF for character sprites).")]
        [SerializeField] private bool trySelfShadows = false;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        private void Awake()
        {
            if (autoSetupOnAwake)
                EnsureShadowCaster2D();
        }

        [ContextMenu("Ensure ShadowCaster2D Now")]
        public void EnsureShadowCaster2D()
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>(includeInactive: true);
            if (sr == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning("[PlayerShadowCaster2DAutoSetup] SpriteRenderer not found in children. Cannot setup ShadowCaster2D.", this);
                return;
            }

            GameObject targetGO = attachToSpriteRendererGameObject ? sr.gameObject : gameObject;

            ShadowCaster2D caster = targetGO.GetComponent<ShadowCaster2D>();
            if (caster == null)
                caster = targetGO.AddComponent<ShadowCaster2D>();

            if (tryUseRendererSilhouette)
                TrySetBool(caster, "useRendererSilhouette", true);

            TrySetBool(caster, "selfShadows", trySelfShadows);

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[PlayerShadowCaster2DAutoSetup] ShadowCaster2D ensured on '{targetGO.name}'. " +
                    $"useRendererSilhouette={(tryUseRendererSilhouette ? "try" : "skip")}, selfShadows={trySelfShadows}",
                    this
                );
            }
        }

        private static void TrySetBool(object target, string memberName, bool value)
        {
            if (target == null)
                return;

            Type t = target.GetType();

            // Property first
            PropertyInfo p = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(bool) && p.CanWrite)
            {
                p.SetValue(target, value);
                return;
            }

            // Field fallback
            FieldInfo f = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(bool))
            {
                f.SetValue(target, value);
            }
        }
    }
}

