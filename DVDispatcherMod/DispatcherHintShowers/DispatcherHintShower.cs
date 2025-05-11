using DV.UI;
using DV.UIFramework;
using DV.Utils;
using DVDispatcherMod.DispatcherHints;
using UnityEngine;

namespace DVDispatcherMod.DispatcherHintShowers {
    public class DispatcherHintShower : IDispatcherHintShower {
        private readonly Transform _attentionLineTransform;
        private readonly NotificationManager _notificationManager;

        private GameObject _notification;
        private GameObject _locoNotification;

        public DispatcherHintShower() {
            _notificationManager = SingletonBehaviour<ACanvasController<CanvasController.ElementType>>.Instance.NotificationManager;

            // transforms cannot be instantiated directly, they always live within a game object. thus we create a single (unnecessary) game object and keep it's transform
            var transformGivingGameObject = new GameObject("ObjectForTransform");
            _attentionLineTransform = transformGivingGameObject.transform;
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

        public void SetLocoLocationHint(string text) {
            if (_locoNotification != null) {
                _notificationManager.ClearNotification(_locoNotification);
                _locoNotification = null;
            }

            if (text != null) {
                _locoNotification = _notificationManager.ShowNotification(text, pointAt: null, localize: false, clearExisting: false);
            }
        }

        private Transform GetAttentionTransform(Vector3? attentionPoint) {
            if (attentionPoint == null || Main.Settings.ShowAttentionLine == false) {
                return null;
            } else {
                _attentionLineTransform.position = attentionPoint.Value;
                return _attentionLineTransform;
            }
        }

        public void Dispose() {
            if (_notification != null) {
                _notificationManager.ClearNotification(_notification);
                _notification = null;
            }

            var gameObject = _attentionLineTransform?.gameObject;
            if (gameObject != null) {
                Object.Destroy(gameObject);
            }
        }
    }
}