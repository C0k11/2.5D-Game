using UnityEngine;
using Game.Framework;
using Game.Framework.Input;

namespace Game.Player
{
    /// <summary>
    /// 玩家 Actor。在 Actor 基础上注入 IPlayerInput，方便模块强类型访问。
    /// 未来若做多角色切换，可以在这里加 ActiveCharacter 切换逻辑。
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerActor : Actor
    {
        public IPlayerInput Input { get; private set; }

        protected override void Awake()
        {
            Input = GetComponent<PlayerInputReader>();
            base.Awake();
        }
    }
}
