using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tarodev
{
    /// <summary>
    /// Unity has probably discussed an auto-save feature countless times over the years
    /// and decided not to implement... so take that information as you'd like. I personally
    /// like the idea and it's worked well for me during my limited testing. If you find any bugs
    /// please report them on the repo: https://github.com/Matthew-J-Spencer/Unity-AutoSave
    /// 
    /// Love,
    /// Your friendly neighborhood Tarodev
    /// </summary>
    [CustomEditor(typeof(AutoSaveConfig))]
    public class TarodevAutoSave : Editor
    {
        /// <summary>
        /// The configuration file that says if we should auto-save, and if so how often.
        /// </summary>
        private static AutoSaveConfig _autoSaveConfig;
        
        /// <summary>
        /// Saving is performed as a task (i.e., while other things may be happening) - as such, we need to provide it
        /// with an acceptable source for cancelling the task in the form of a "Cancel Token". It's super unlikely that
        /// we'd cancel saving, but every task needs to be "cancel-able" so we have to play nice and provide it with the
        /// details it needs.
        /// Further reading: https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource 
        /// </summary>
        private static CancellationTokenSource _autoSaveCancellationTokenSource;
        
        /// <summary>
        /// As mentioned, saving our Unity data is a task that doesn't happen instantly - it takes a small amount of
        /// time, so this task represents the saving process while it's happening.
        /// </summary>
        private static Task _autoSaveTask;

        /// <summary>
        /// When our project loads - we execute this initialization method.
        /// </summary>
        /// <returns></returns>
        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            // Grab the AutoSaveConfig (how often to save), but cancel it right now because we don't want to save
            // immediately - we'll only save every `frequencyMins` interval.
            FetchAutoSaveConfig();
            CancelTask();

            // Create a task cancellation source (tasks require this) and then start the auto-save task!
            _autoSaveCancellationTokenSource = new CancellationTokenSource();
            _autoSaveTask = SaveInterval(_autoSaveCancellationTokenSource.Token);
        }

        /// <summary>
        /// Method to fetch the current AutoSaveConfig, i.e., the ScriptableObject with details of how often we should
        /// save!
        /// </summary>
        private static void FetchAutoSaveConfig()
	{		            
            while (true)
            {
                // If we already have an AutoSaveConfig then we can exit this method
                if (_autoSaveConfig != null)
                {
                    Debug.LogWarning("AutoSaveConfig exists - auto save frequency is currently: " + _autoSaveConfig.FrequencyMins);
                    return;
                }

                // If we didn't already have one, then attempt to find the path to an AutoSaveConfig
                var path = GetConfigPath();

                // No joy? Create a new AutoSaveConfig using the default values to auto-save once per minute
                if (path == null) {
                    AssetDatabase.CreateAsset(CreateInstance<AutoSaveConfig>(), $"Assets/{nameof(AutoSaveConfig)}.asset");
                    AssetDatabase.Refresh();
                    Debug.Log("A AutoSaveConfig file has been created at the root of your project. <b>You can move this anywhere you'd like.</b>");
                    continue;
                }

                // Assumed else - we found a path to an AutoSaveConfig, so load it and exit this while loop
                _autoSaveConfig = AssetDatabase.LoadAssetAtPath<AutoSaveConfig>(path);
                break;
            }
        }
        
        /// <summary>
        /// Method to return the AutoSaveConfig path as string. 
        /// </summary>
        /// <returns>The AutoSaveConfig path as a string, or null if no AutoSaveConfig.asset file could be found.</returns>
        private static string GetConfigPath() {
            var paths = AssetDatabase.FindAssets(nameof(AutoSaveConfig)).Select(AssetDatabase.GUIDToAssetPath).Where(c => c.EndsWith(".asset")).ToList();
            if (paths.Count > 1) { Debug.LogWarning("Multiple auto save config assets found. Delete one."); }
            return paths.FirstOrDefault(); // Note: `FirstOrDefault` will return null if no config was found.
        }

        /// <summary>
        /// Method to cancel the auto-save task. 
        /// </summary>
        private static void CancelTask()
        {
            if (_autoSaveTask == null) return;
            _autoSaveCancellationTokenSource.Cancel();
            _autoSaveTask.Wait();
        }
        
        /// <summary>
        /// Method to save all your Unity data.
        /// </summary>
        /// <param name="token">The cancellation token required to perform the task (rarely used!)</param>
        private static async Task SaveInterval(CancellationToken token)
        {
            // The save frequency in milliseconds (the original config value is stored in minutes)
            int saveFrequencyMS = _autoSaveConfig.FrequencyMins * 1000 * 60; 
            
            // While the task hasn't been cancelled perform the save operation
            while (!token.IsCancellationRequested)
            {
                Debug.Log("Waiting to save: " + DateTime.Now);
                
                await Task.Delay(saveFrequencyMS, token);
                if (_autoSaveConfig == null) { FetchAutoSaveConfig(); }

                // Don't save if AutoSave is disabled, or the application is playing or we're building or compiling
                if (!_autoSaveConfig.Enabled || Application.isPlaying || BuildPipeline.isBuildingPlayer || EditorApplication.isCompiling) continue;
                
                // Also don't save when the Unity editor is just sitting in the background inactive!
                if (!UnityEditorInternal.InternalEditorUtility.isApplicationActive) continue;

                // Otherwise perform the save operation (phew!)
                EditorSceneManager.SaveOpenScenes();
                if (_autoSaveConfig.LogMsgOnAutoSave) { Debug.Log($"Auto-saved at {DateTime.Now:h:mm:ss tt}"); }
            }
        }
        
        /// <summary>
        /// Method to add a menu item to the `Window` main Unity menu that shows your current AutoSaveConfig.
        /// </summary>
        [MenuItem("Window/AutoSave/FindAutoSaveConfig")]
        public static void ShowConfig()
        {
            FetchAutoSaveConfig();
            var path = GetConfigPath();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<AutoSaveConfig>(path).GetInstanceID());
        }

        /// <summary>
        /// Method to provide details about
        /// </summary>
        /// <returns></returns>
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("You can move this asset where ever you'd like.\nWith ❤, Tarodev.", MessageType.Info);
        }
    }
}
