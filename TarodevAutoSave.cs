using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tarodev {
    /// <summary>
    /// Unity has probably discussed an auto-save feature countless times over the years
    /// and decided not to implement... so take that information as you'd like. I personally
    /// like the idea and it's worked well for me during my limited testing. If you find any bugs
    /// please report them on the repo: https://github.com/Matthew-J-Spencer/Unity-AutoSave
    /// 
    /// Love your friendly neighborhood Tarodev
    /// </summary>
    [CustomEditor(typeof(AutoSaveConfig))]
    public class TarodevAutoSave : Editor {
        private static AutoSaveConfig _config;
        private static CancellationTokenSource _tokenSource;
        private static Task _task;
        private static bool _savedWhileInactive;

        [InitializeOnLoadMethod]
        private static void OnEnable() {
            FetchConfig();
            CancelTask();

            _tokenSource = new CancellationTokenSource();
            _task = SaveInterval(_tokenSource.Token);
        }

        private static void FetchConfig() {
            while (true) {
                if (_config != null) return;

                var configGuids = AssetDatabase.FindAssets(nameof(AutoSaveConfig));
                if (configGuids.Length > 1) Debug.LogWarning("Multiple auto save config assets found. Delete one.");

                if (configGuids.Length == 0) {
                    AssetDatabase.CreateAsset(CreateInstance<AutoSaveConfig>(), $"Assets/{nameof(AutoSaveConfig)}.asset");
                    Debug.Log("A config file has been created at the root of your project.<b> You can move this anywhere you'd like.</b>");
                    continue;
                }

                var path = AssetDatabase.GUIDToAssetPath(configGuids[0]);
                _config = AssetDatabase.LoadAssetAtPath<AutoSaveConfig>(path);

                break;
            }
        }

        private static void CancelTask() {
            if (_task == null) return;
            _tokenSource.Cancel();
            _task.Wait();
        }

        private static async Task SaveInterval(CancellationToken token) {
            while (!token.IsCancellationRequested) {
                await Task.Delay(_config.Frequency * 1000, token);
                if (!_config.Enabled || Application.isPlaying) continue;

                // Don't continuously save while unfocused. Once is enough
                if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive) {
                    if (_savedWhileInactive) continue;
                    _savedWhileInactive = true;
                }
                else _savedWhileInactive = false;

                EditorSceneManager.SaveOpenScenes();
                if (_config.Logging) Debug.Log($"Auto-saved at {DateTime.Now:h:mm:ss tt}");
            }
        }

        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("You can move this asset where ever you'd like.\nWith ❤, Tarodev.", MessageType.Info);
        }
    }

    public class AutoSaveConfig : ScriptableObject {
        [Tooltip("Enable auto save functionality")]
        public bool Enabled = true;

        [Tooltip("The frequency in seconds auto save will activate"), Min(10)]
        public int Frequency = 10;

        [Tooltip("Log a message every time the scene is auto saved")]
        public bool Logging = false;
    }
}