using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Framework.AI;

namespace Game.Framework.UI.Views
{
    /// <summary>
    /// 玩家血量 HUD。非模态常驻，不走 UIManager 栈，显示风格和 InteractPromptView 一致。
    ///
    /// 绑定方式（优先级从高到低）：
    ///   1. 外部 Assign(Health)：适合跨场景传入
    ///   2. 场景里按 Tag="Player" 自动找一个带 Health 的 GameObject
    ///   3. 订阅 EventBus.ActorSpawned，发现 id == targetActorId 的 Actor 再 attach
    ///
    /// 视觉：fillBar 是一个 Image(FillMethod=Horizontal)，value=current/max。
    /// 扣血时 trailingBar 延迟追平，模仿"白条"视觉（尼尔/战神常见）。
    /// </summary>
    public class PlayerHealthView : UIView
    {
        [Header("References")]
        public Image fillBar;          // 主血条
        public Image trailingBar;      // 延迟追平的白条（可选）
        public Text  label;            // 可选："HP 23 / 30"

        [Header("Tracking")]
        [Tooltip("绑定目标 Actor 的 Id（Actor.Id 默认为 GameObject 名）。留空则按 Tag 找 Player。")]
        public string targetActorId = "Player";
        [Tooltip("白条追平速度（单位：HP/秒）。")]
        public float trailingSpeed = 8f;

        private Health _health;
        private float _trailingHp;

        protected override void Awake()
        {
            base.Awake();
            gameObject.SetActive(true); // 常驻 HUD
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ActorSpawned>(OnActorSpawned);
            EventBus.Subscribe<ActorDied>(OnActorDied);
            TryAutoAttach();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ActorSpawned>(OnActorSpawned);
            EventBus.Unsubscribe<ActorDied>(OnActorDied);
            Detach();
        }

        public void Assign(Health h)
        {
            Detach();
            _health = h;
            if (_health != null)
            {
                _health.OnDamaged += OnDamaged;
                _trailingHp = _health.Hp;
                Refresh(instant: true);
            }
        }

        private void Detach()
        {
            if (_health != null) _health.OnDamaged -= OnDamaged;
            _health = null;
        }

        private void TryAutoAttach()
        {
            if (_health != null) return;
            if (string.IsNullOrEmpty(targetActorId)) return;

            // 按 Tag 找最快：Unity 里 Player GameObject 通常 Tag=Player
            GameObject go = null;
            if (targetActorId == "Player")
            {
                try { go = GameObject.FindGameObjectWithTag("Player"); }
                catch { /* Tag 未定义时 Unity 会抛异常；无 Player Tag 则跳过 */ }
            }
            if (go == null)
            {
                // 退路：遍历场景找同名 Actor
                var actors = FindObjectsByType<Game.Framework.Actor>(FindObjectsSortMode.None);
                for (int i = 0; i < actors.Length; i++)
                    if (actors[i].Id == targetActorId) { go = actors[i].gameObject; break; }
            }

            if (go != null)
            {
                var h = go.GetComponent<Health>();
                if (h != null) Assign(h);
            }
        }

        private void OnActorSpawned(ActorSpawned evt)
        {
            if (_health != null) return;
            if (evt.Id != targetActorId) return;
            TryAutoAttach();
        }

        private void OnActorDied(ActorDied evt)
        {
            if (_health == null || evt.Id != targetActorId) return;
            Refresh(instant: true);
        }

        private void OnDamaged(DamageInfo info) => Refresh(instant: false);

        private void Update()
        {
            if (_health == null) return;
            // 白条追平
            if (_trailingHp > _health.Hp)
            {
                _trailingHp = Mathf.Max(_health.Hp, _trailingHp - trailingSpeed * Time.unscaledDeltaTime);
                UpdateBars();
            }
            else if (_trailingHp < _health.Hp)
            {
                _trailingHp = _health.Hp;
                UpdateBars();
            }
        }

        private void Refresh(bool instant)
        {
            if (_health == null) return;
            if (instant) _trailingHp = _health.Hp;
            UpdateBars();
        }

        private void UpdateBars()
        {
            if (_health == null) return;
            float max = Mathf.Max(1f, _health.maxHp);
            float cur = _health.Hp / max;
            float trail = _trailingHp / max;

            if (fillBar != null) fillBar.fillAmount = cur;
            if (trailingBar != null) trailingBar.fillAmount = trail;
            if (label != null) label.text = $"HP {Mathf.CeilToInt(_health.Hp)} / {Mathf.CeilToInt(_health.maxHp)}";
        }
    }
}
