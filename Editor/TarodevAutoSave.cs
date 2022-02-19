using System;
using System.Linq;
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

        [InitializeOnLoadMethod]
        private static void OnInitialize() {
            FetchConfig();
            CancelTask();

            _tokenSource = new CancellationTokenSource();
            _task = SaveInterval(_tokenSource.Token);
        }

        private static void FetchConfig() {
            while (true) {
                if (_config != null) return;

                var path = GetConfigPath();

                if (path == null) {
                    AssetDatabase.CreateAsset(CreateInstance<AutoSaveConfig>(), $"Assets/{nameof(AutoSaveConfig)}.asset");
                    Debug.Log("A config file has been created at the root of your project.<b> You can move this anywhere you'd like.</b>");
                    continue;
                }

                _config = AssetDatabase.LoadAssetAtPath<AutoSaveConfig>(path);

                break;
            }
        }

        private static string GetConfigPath() {
            var paths = AssetDatabase.FindAssets(nameof(AutoSaveConfig)).Select(AssetDatabase.GUIDToAssetPath).Where(c => c.EndsWith(".asset")).ToList();
            if (paths.Count > 1) Debug.LogWarning("Multiple auto save config assets found. Delete one.");
            return paths.FirstOrDefault();
        }

        private static void CancelTask() {
            if (_task == null) return;
            _tokenSource.Cancel();
            _task.Wait();
        }

        private static async Task SaveInterval(CancellationToken token) {
            while (!token.IsCancellationRequested) {
                await Task.Delay(_config.Frequency * 1000 * 60, token);
                if (_config == null) FetchConfig();

                if (!_config.Enabled || Application.isPlaying || BuildPipeline.isBuildingPlayer || EditorApplication.isCompiling) continue;
                if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive) continue;

                EditorSceneManager.SaveOpenScenes();
                if (_config.Logging) Debug.Log($"Auto-saved at {DateTime.Now:h:mm:ss tt}");
            }
        }
        
        [MenuItem("Window/Auto save/Find config")]
        public static void ShowConfig() {
            FetchConfig();

            var path = GetConfigPath();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<AutoSaveConfig>(path).GetInstanceID());
        }

        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("You can move this asset where ever you'd like.\nWith ❤, Tarodev.", MessageType.Info);
        }
    }
}