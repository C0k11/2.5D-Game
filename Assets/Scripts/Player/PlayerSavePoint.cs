using System;
using UnityEngine;
using Game.Core;
using Game.Framework.Save;

namespace Game.Player
{
    /// <summary>
    /// 玩家存档贡献者。订阅 SaveCaptureRequested / SaveRestoreRequested，
    /// 把玩家位置 + 朝向序列化进 / 出 SlotSaveData.blobs（3D 版）。
    /// </summary>
    public class PlayerSavePoint : MonoBehaviour
    {
        private const string BlobId = "player";

        [Serializable]
        private class Blob
        {
            public float x, y, z;
            public float fwdX, fwdZ;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<SaveCaptureRequested>(OnCapture);
            EventBus.Subscribe<SaveRestoreRequested>(OnRestore);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SaveCaptureRequested>(OnCapture);
            EventBus.Unsubscribe<SaveRestoreRequested>(OnRestore);
        }

        private void OnCapture(SaveCaptureRequested evt)
        {
            var actor = GetComponent<Game.Framework.Actor>();
            var fwd = actor != null ? actor.State.Forward : transform.forward;
            var blob = new Blob
            {
                x = transform.position.x,
                y = transform.position.y,
                z = transform.position.z,
                fwdX = fwd.x,
                fwdZ = fwd.z,
            };
            SaveManager.WriteBlob(evt.Data, BlobId, blob);
        }

        private void OnRestore(SaveRestoreRequested evt)
        {
            var blob = SaveManager.ReadBlob<Blob>(evt.Data, BlobId);
            if (blob == null) return;
            transform.position = new Vector3(blob.x, blob.y, blob.z);
            var actor = GetComponent<Game.Framework.Actor>();
            if (actor != null) actor.SetDirection(new Vector3(blob.fwdX, 0f, blob.fwdZ));
        }
    }
}
