using DV.UI;
using DV.UIFramework;
using DV.Utils;
using DVDispatcherMod.DispatcherHints;
using DVDispatcherMod.Extensions;
using UnityEngine;

namespace DVDispatcherMod.DispatcherHintShowers {
    public class DispatcherHintShower : IDispatcherHintShower {
        private readonly GameObject _attentionLineGameObject;
        private readonly NotificationManager _notificationManager;

        private GameObject _notification;

        public DispatcherHintShower(NotificationManager notificationManager) {
            _notificationManager = notificationManager;

            // transforms cannot be instantiated directly, they always live within a game object. thus we create a single (unnecessary) game object and keep it's transform
            _attentionLineGameObject = new GameObject("ObjectForTransform");
        }

        public void SetDispatcherHint(DispatcherHint dispatcherHintOrNull) {
            if (_notification != null) {
                _notificationManager.ClearNotification(_notification);
                _notification = null;
            }

            if (dispatcherHintOrNull != null) {
                var transform = GetAttentionTransform(dispatcherHintOrNull.AttentionPoint);

                _notification = _notificationManager.ShowNotification(dispatcherHintOrNull.Text, pointAt: transform, localize: false, clearExisting: false);
            }
        }

        private Transform GetAttentionTransform(Vector3? attentionPoint) {
            if (attentionPoint == null || Main.Settings.ShowAttentionLine == false) {
                return null;
            } else {
                _attentionLineGameObject.transform.position = attentionPoint.Value;
                return _attentionLineGameObject.transform;
            }
        }

        public void Dispose() {
            if (_notification != null) {
                _notificationManager.ClearNotification(_notification);
                _notification = null;
            }

            _attentionLineGameObject.DestroyIfAlive();
        }
    }
}