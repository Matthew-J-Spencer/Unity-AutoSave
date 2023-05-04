using UnityEngine;

namespace Tarodev
{
    /// <summary>
    /// A simple ScriptableObject to keep track of whether we should automatically save our Unity scenes, and if we
    /// should then how often should we do so.
    /// </summary>
    public class AutoSaveConfig : ScriptableObject
    {
        [Tooltip("Whether to enable auto-save functionality or not - default to autosave.")]
        public bool Enabled = true;

        [Tooltip("The frequency to autosave in minutes - default is 1 minute."), Min(1)]
        public int FrequencyMins = 1;

        [Tooltip("Whether to log a message every time the scene is auto saved or not - default is false.")]
        public bool LogMsgOnAutoSave;
    }
}
