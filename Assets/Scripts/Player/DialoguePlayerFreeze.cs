using UnityEngine;
using Game.Core;
using Game.Framework;
using Game.Framework.Dialogue;

namespace Game.Player
{
    /// <summary>
    /// 对话期间冻结玩家 Actor。订阅 DialogueStarted/Ended 事件切 IsPaused，
    /// 这样对话系统与玩家模块完全不互相认识。
    /// </summary>
    [RequireComponent(typeof(Actor))]
    public class DialoguePlayerFreeze : MonoBehaviour
    {
        private Actor _actor;

        private void Awake() { _actor = GetComponent<Actor>(); }

        private void OnEnable()
        {
            EventBus.Subscribe<DialogueStarted>(OnStarted);
            EventBus.Subscribe<DialogueEnded>(OnEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DialogueStarted>(OnStarted);
            EventBus.Unsubscribe<DialogueEnded>(OnEnded);
        }

        private void OnStarted(DialogueStarted _)
        {
            _actor.IsPaused = true;
            _actor.State.Velocity = Vector3.zero;
        }

        private void OnEnded(DialogueEnded _)
        {
            _actor.IsPaused = false;
        }
    }
}
