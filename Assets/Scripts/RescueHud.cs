using UnityEngine;

namespace Frostline
{
    public sealed class RescueHud : MonoBehaviour
    {
        public RescueGame Game;
        readonly Color ink = new Color(0.035f, 0.07f, 0.09f, 0.95f);
        readonly Color panel = new Color(0.06f, 0.105f, 0.13f, 0.94f);
        readonly Color muted = new Color(0.59f, 0.70f, 0.73f);
        readonly Color white = new Color(0.91f, 0.96f, 0.94f);
        readonly Color orange = new Color(1, 0.47f, 0.22f);
        readonly Color teal = new Color(0.30f, 0.91f, 0.79f);
        Font font;
        GUIStyle label;
        GUIStyle button;
        bool settings;
        bool resetConfirm;
        bool practice;
        float width;
        float height;
        float scale;
        int selected;

        void Prepare()
        {
            if (font != null) return;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei UI", "Microsoft YaHei", "Segoe UI" }, 24);
            label = new GUIStyle { font = font, fontSize = 18, wordWrap = true, richText = true };
            label.normal.textColor = white;
            button = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
        }
        void OnGUI()
        {
            if (Game == null || Game.Save == null || Game.Crane == null) return;
            Prepare(); scale = Mathf.Min(Screen.width / 1440f, Screen.height / 900f);
            width = Screen.width / scale; height = Screen.height / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            if (Game.Rules.Phase == MissionPhase.Menu) Menu();
            else
            {
                Hud();
                if (Game.Rules.Phase == MissionPhase.Paused) Pause();
                if (Game.Rules.Phase == MissionPhase.Won || Game.Rules.Phase == MissionPhase.Lost) Result();
            }
            if (settings) Settings();
            GUI.matrix = Matrix4x4.identity;
        }
        void Fill(Rect area, Color color)
        {
            var previous = GUI.color; GUI.color = color; GUI.DrawTexture(area, Texture2D.whiteTexture); GUI.color = previous;
        }
        void Text(float horizontal, float vertical, float boxWidth, float boxHeight, string text, int size = 18, Color? color = null, bool bold = false)
        {
            label.fontSize = size; label.normal.textColor = color ?? white; label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            GUI.Label(new Rect(horizontal, vertical, boxWidth, boxHeight), text, label);
        }
        bool Button(Rect area, string text, bool primary = false, bool enabled = true)
        {
            bool hover = area.Contains(Event.current.mousePosition);
            Fill(area, !enabled ? new Color(0.11f, 0.16f, 0.18f) : primary ? (hover ? new Color(1, 0.61f, 0.33f) : orange) : (hover ? new Color(0.2f, 0.31f, 0.34f) : new Color(0.12f, 0.22f, 0.25f)));
            button.normal.textColor = primary ? ink : enabled ? white : muted;
            bool wasEnabled = GUI.enabled; GUI.enabled = enabled;
            bool clicked = GUI.Button(area, text, button); GUI.enabled = wasEnabled; return clicked;
        }
        void Menu()
        {
            Fill(new Rect(0, 0, 470, height), ink);
            Fill(new Rect(42, 44, 42, 4), orange);
            Text(98, 32, 330, 32, "FROSTLINE  /  RESCUE OPERATIONS", 13, muted);
            Text(40, 98, 430, 80, "霜岭索道救援", 43, white, true);
            Text(43, 166, 380, 40, "在风雪之间，把每一个人带回来。", 19, muted);
            Text(43, 230, 380, 30, "任务调度", 17, teal, true);
            for (int mission = 0; mission < 4; mission++)
            {
                var area = new Rect(40, 275 + mission * 88, 390, 74);
                bool unlocked = mission < Game.Save.unlocked;
                Fill(area, selected == mission ? new Color(0.16f, 0.25f, 0.28f) : panel);
                if (selected == mission) Fill(new Rect(area.x, area.y, 4, area.height), orange);
                Text(area.x + 18, area.y + 11, 320, 28, Game.missions[mission].title, 20, unlocked ? white : muted, true);
                Text(area.x + 18, area.y + 43, 330, 22, unlocked ? Game.missions[mission].weather : "完成上一任务后解锁", 13, muted);
                if (GUI.Button(area, GUIContent.none, GUIStyle.none) && unlocked) selected = mission;
            }
            if (Button(new Rect(40, 646, 390, 52), "开始救援  →", true, !settings)) { Game.Begin(selected, practice); return; }
            if (Button(new Rect(40, 711, 390, 38), practice ? "✓  自由练习 · 不限时，不计排名" : "自由练习 · 当前关闭", false, !settings)) practice = !practice;
            if (Button(new Rect(40, height - 94, 187, 40), "设置", false, !settings)) settings = true;
            if (Button(new Rect(243, height - 94, 187, 40), "退出游戏", false, !settings)) Application.Quit();
            Text(41, height - 40, 390, 24, "SR–7  /  ALPINE RESPONSE UNIT", 12, muted);
            Fill(new Rect(width - 346, 44, 300, 92), panel);
            Text(width - 325, 61, 260, 24, "北坡救援站   /   海拔 2,840 m", 15, white);
            Text(width - 325, 94, 260, 25, "●  装备就绪     4 项救援任务", 14, teal);
            Fill(new Rect(width - 450, height - 183, 405, 137), panel);
            Text(width - 425, height - 164, 350, 28, Game.missions[selected].weather, 16, teal, true);
            Text(width - 425, height - 125, 350, 80, Game.missions[selected].briefing, 17, white);
            if (!string.IsNullOrEmpty(Game.Store.Notice)) Text(490, height - 55, width - 520, 45, Game.Store.Notice, 16, orange);
        }
        void Hud()
        {
            Fill(new Rect(24, 24, width - 48, 83), ink);
            Fill(new Rect(24, 24, 5, 83), orange);
            Text(48, 38, 390, 32, "霜岭  /  " + Game.Mission.title, 23, white, true);
            Text(48, 75, 450, 23, Game.Mission.weather, 14, muted);
            Text(width - 380, 37, 160, 26, Game.Rules.Practice ? "自由练习" : "剩余作业时间", 14, muted);
            float remaining = Game.Rules.Practice ? Game.Rules.Elapsed : Mathf.Max(0, Game.Mission.timeLimit - Game.Rules.Elapsed);
            Text(width - 380, 65, 160, 35, ((int)remaining / 60).ToString("00") + ":" + ((int)remaining % 60).ToString("00"), 26, white, true);
            if (Button(new Rect(width - 185, 44, 130, 42), "暂停  Esc")) Game.Pause(true);
            Fill(new Rect(24, 125, 260, 216), panel);
            Text(44, 142, 220, 30, "接应进度", 15, muted);
            Text(44, 174, 220, 50, Game.Rules.Delivered + " / " + Game.Rules.Required + "   舱", 35, white, true);
            string objective = Game.Crane.Attached == null ? "寻找并锁接橙色救援舱" : Game.Mission.transfer && !Game.Crane.Attached.RelayDone ? "送至中央中继台" : "送至青色接应平台";
            Text(44, 238, 220, 65, objective, 19, teal, true);
            Text(44, 310, 220, 24, "低速落稳 → Space 解锁", 13, muted);
            float right = width - 262;
            Fill(new Rect(right, 125, 238, 290), panel);
            Text(right + 20, 143, 198, 25, "吊机遥测  /  SR–7", 15, muted);
            Metric(right + 20, 187, "绞盘长度", Game.Crane.CableLength.ToString("0.0") + " m", Game.Crane.CableLength / 18, teal);
            Metric(right + 20, 259, "载荷摆幅", Game.Crane.Swing.ToString("0.0") + "°", Game.Crane.Swing / 35, Game.Crane.Swing > 18 ? orange : teal);
            float speed = Game.Crane.Attached != null ? Game.Crane.Attached.Body.linearVelocity.magnitude : Game.Crane.Hook.linearVelocity.magnitude;
            Metric(right + 20, 331, "吊钩 / 载荷速度", speed.ToString("0.0") + " m/s", speed / 7, speed > 3 ? orange : teal);
            Fill(new Rect(right, 431, 238, 148), panel);
            Text(right + 20, 449, 200, 25, Game.GustWarning ? "!  阵风即将到达" : "风况监测", 16, Game.GustWarning ? orange : muted);
            Text(right + 20, 484, 200, 43, Game.Wind.magnitude.ToString("0.0") + "  m/s  →", 28, white, true);
            Text(right + 20, 535, 200, 25, "累计损伤  " + Mathf.RoundToInt(Game.TotalDamage) + "%", 17, Game.TotalDamage > 30 ? orange : muted);
            DrawWorldLabels();
            float bottom = height - 122;
            Fill(new Rect(24, bottom, width - 48, 98), ink);
            string[] keys = { "A / D", "W / S", "Q / E", "SPACE", "C", "R / H" };
            string[] hints = { "左 / 右回转", "抬臂 / 降臂", "收缆 / 放缆", "锁接 / 解锁", "切换视角", "重试 / 操作指南" };
            float cell = (width - 86) / 6;
            for (int control = 0; control < 6; control++)
            {
                float horizontal = 44 + control * cell;
                Text(horizontal, bottom + 17, cell, 30, keys[control], 20, white, true);
                Text(horizontal, bottom + 56, cell, 24, hints[control], 14, muted);
            }
            if (Time.unscaledTime < Game.MessageUntil)
            {
                Fill(new Rect(310, height - 190, width - 620, 52), ink);
                Text(330, height - 177, width - 660, 38, Game.Message, 16, teal);
            }
        }
        void Metric(float horizontal, float vertical, string name, string value, float fraction, Color color)
        {
            Text(horizontal, vertical, 190, 23, name, 13, muted);
            Text(horizontal, vertical + 23, 190, 32, value, 24, white, true);
            Fill(new Rect(horizontal, vertical + 59, 196, 3), new Color(0.2f, 0.29f, 0.31f));
            Fill(new Rect(horizontal, vertical + 59, 196 * Mathf.Clamp01(fraction), 3), color);
        }
        void DrawWorldLabels()
        {
            foreach (var cargo in Game.Cargo)
            {
                if (cargo.Delivered) continue;
                Vector3 point = Camera.main.WorldToScreenPoint(cargo.LatchPosition + Vector3.up * 1.5f);
                if (point.z < 0) continue;
                float horizontal = point.x / scale; float vertical = (Screen.height - point.y) / scale;
                if (horizontal < 300 || horizontal > width - 280 || vertical < 120 || vertical > height - 200) continue;
                Fill(new Rect(horizontal - 90, vertical - 16, 180, 31), ink);
                Text(horizontal - 80, vertical - 10, 170, 25, cargo.Attached ? "● 已锁接 · 缓慢移动" : "◇ 待接应舱 " + (cargo.Id + 1), 13, cargo.Attached ? teal : orange);
            }
            if (Game.Crane.Attached != null && Game.Crane.Attached.CanRelease)
            {
                Fill(new Rect(width / 2 - 170, height / 2 + 95, 340, 48), ink);
                Text(width / 2 - 150, height / 2 + 106, 310, 30, "✓ 已停稳   按 SPACE 安全解锁", 18, teal, true);
            }
        }
        void Overlay() { Fill(new Rect(0, 0, width, height), new Color(0.015f, 0.035f, 0.045f, 0.78f)); }
        void Pause()
        {
            Overlay(); float left = width / 2 - 300; float top = height / 2 - 260;
            Fill(new Rect(left, top, 600, 520), ink);
            Text(left + 35, top + 30, 530, 50, Game.Help ? "操作指南" : "作业已暂停", 32, white, true);
            Text(left + 35, top + 98, 530, 235,
                "1   对准舱顶青色吊环，按 E 缓慢放缆。\n\n2   吊钩距吊环 0.85 m 内、速度低于 1.1 m/s 时，按 Space 锁接。\n\n3   按 Q 提升载荷，再用 A/D 回转、W/S 调整半径。\n\n4   放入目标圈内，停稳 1.2 秒后按 Space 解锁。", 18, muted);
            if (Button(new Rect(left + 35, top + 365, 530, 52), "继续作业", true, !settings)) { Game.Help = false; Game.Pause(false); }
            if (Button(new Rect(left + 35, top + 437, 255, 44), "设置", false, !settings)) settings = true;
            if (Button(new Rect(left + 310, top + 437, 255, 44), "返回调度", false, !settings)) Game.Menu();
        }
        void Result()
        {
            Overlay(); bool won = Game.Rules.Phase == MissionPhase.Won; float left = width / 2 - 280; float top = height / 2 - 230;
            Fill(new Rect(left, top, 560, 460), ink); Fill(new Rect(left, top, 560, 5), won ? teal : orange);
            Text(left + 35, top + 35, 490, 50, won ? "所有救援舱已安全抵达" : "本次救援中止", 29, white, true);
            Text(left + 35, top + 106, 490, 95, won ? (Game.Rules.Practice ? "练习完成，不记录排名与解锁。" : "任务完成，新的作业区域已开放。") : Game.Rules.Failure, 19, muted);
            Text(left + 35, top + 212, 490, 42, "作业用时 " + Game.Rules.Elapsed.ToString("0") + " 秒   /   损伤 " + Game.TotalDamage.ToString("0") + "%", 19, teal);
            Text(left + 35, top + 262, 490, 38, "救援评分  " + Game.Rules.Score(Game.TotalDamage), 25, white, true);
            if (Button(new Rect(left + 35, top + 325, 490, 50), won ? "返回任务调度" : "重新尝试", true))
            {
                if (won) Game.Menu(); else Game.Begin(Game.Mission.index, Game.Rules.Practice);
            }
            if (Button(new Rect(left + 35, top + 389, 490, 36), "返回主界面")) Game.Menu();
        }
        void Settings()
        {
            Overlay(); float left = width / 2 - 265; float top = height / 2 - 255;
            Fill(new Rect(left, top, 530, 510), ink); Text(left + 30, top + 25, 470, 50, "设备与操作设置", 30, white, true);
            Text(left + 30, top + 99, 470, 30, "总音量   " + Mathf.RoundToInt(Game.Save.volume * 100) + "%", 19);
            Game.Save.volume = GUI.HorizontalSlider(new Rect(left + 30, top + 140, 470, 25), Game.Save.volume, 0, 1);
            Text(left + 30, top + 185, 470, 30, "操作灵敏度   " + Game.Save.sensitivity.ToString("0.0") + "×", 19);
            Game.Save.sensitivity = GUI.HorizontalSlider(new Rect(left + 30, top + 226, 470, 25), Game.Save.sensitivity, 0.4f, 1.6f);
            string[] qualityNames = { "节能", "标准", "精细" };
            for (int quality = 0; quality < 3; quality++)
                if (Button(new Rect(left + 30 + quality * 160, top + 280, 150, 40), (Game.Save.quality == quality ? "✓ " : "") + qualityNames[quality])) Game.Save.quality = quality;
            if (Button(new Rect(left + 30, top + 350, 470, 40), resetConfirm ? "再次点击确认清除全部关卡进度" : "重置关卡进度"))
            {
                if (resetConfirm) { Game.ResetProgress(); selected = 0; resetConfirm = false; }
                else resetConfirm = true;
            }
            if (Button(new Rect(left + 30, top + 422, 470, 52), "保存并返回", true))
            {
                Game.ApplySettings(); Game.Store.Write(Game.Save); settings = false; resetConfirm = false;
            }
        }
    }
}
