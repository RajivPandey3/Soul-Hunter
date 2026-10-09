using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SoulHunter.Gameplay.Player;
using SoulHunter.Gameplay.UI;

namespace SoulHunter.UI
{
    /// <summary>
    /// The VS Unlocks screen (release item P3-02, 2026-10-09; wiki Main_menu: "A menu that lists various tasks that players
    /// can accomplish to unlock new items, characters, and more", "Available by default"). One row per task (UnlockTasks),
    /// grouped as Characters, PowerUps and Features, each saying in words whether it is done, what to do and what it
    /// unlocks. Done states come only from the save (GameData), never from AchievementManager's in-run set. The rows are not
    /// buttons (nothing to choose); keyboard and controller players scroll with the focusable scrollbar.
    /// </summary>
    public sealed class UnlocksMenuUI : MenuSectionScreen
    {
        public const string DoneWord = "Done", NotDoneWord = "Not done";

        private RectTransform _content;
        private readonly List<TextMeshProUGUI> _rowTexts = new List<TextMeshProUGUI>();
        private int _builtRows = -1;

        /// <summary>The row texts now shown (tests).</summary>
        public IReadOnlyList<TextMeshProUGUI> RowTexts => _rowTexts;

        protected override void Build()
        {
            _content = MakeScroll("UnlocksList", Panel, 0.05f, 0.08f, 0.95f, 0.82f, out _);
            var list = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.spacing = 8f;
            list.childControlWidth = list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
        }

        public override void Refresh()
        {
            if (_content == null) return;
            var tasks = UnlockTasks.All(Save);
            if (_builtRows != tasks.Count) BuildRows(tasks);
            int done = 0, i = 0;
            string group = null;
            foreach (var task in tasks)
            {
                if (task.Group != group)
                {
                    group = task.Group;
                    _rowTexts[i++].text = UITheme.Bold(group);
                }
                if (task.Done) done++;
                // State in words, not colour alone (ui-ux-pro-max): "Done" / "Not done", the task, then its reward.
                _rowTexts[i++].text = $"{UITheme.Bold(task.Done ? DoneWord : NotDoneWord)}    {task.Task}    ({task.Reward})";
            }
            if (CountText != null) CountText.text = $"Done {done} / {tasks.Count}";
        }

        private void BuildRows(List<UnlockTask> tasks)
        {
            foreach (Transform child in _content) Destroy(child.gameObject);
            _rowTexts.Clear();
            string group = null;
            foreach (var task in tasks)
            {
                if (task.Group != group)
                {
                    group = task.Group;
                    _rowTexts.Add(Row(52f, false));
                }
                _rowTexts.Add(Row(56f, true));
            }
            _builtRows = tasks.Count;
        }

        /// <summary>A group heading (accent text on the window) or a task row (body text on a row plate).</summary>
        private TextMeshProUGUI Row(float height, bool plate)
        {
            var go = new GameObject(plate ? "Task" : "Group", typeof(RectTransform), typeof(LayoutElement));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_content, false);
            go.GetComponent<LayoutElement>().preferredHeight = height;
            if (plate)
            {
                var image = go.AddComponent<Image>();
                UITheme.ApplySurface(image, ThemedElement.SurfaceRole.Row);
                image.raycastTarget = false;
            }
            var text = EntryText(rect, "Text", plate ? 28f : 34f, TextAlignmentOptions.MidlineLeft,
                plate ? ThemedElement.TextRole.Body : ThemedElement.TextRole.Accent, plate ? (System.Func<Color>)(() => UITheme.Row) : () => UITheme.Panel);
            text.rectTransform.offsetMin = new Vector2(20f, 2f);
            text.rectTransform.offsetMax = new Vector2(-20f, -2f);
            return text;
        }
    }
}
