using DV.UI;
using DV.UIFramework;
using DV.Utils;
using DVDispatcherMod.DispatcherHints;
using System;
using UnityEngine;

namespace DVDispatcherMod.DispatcherHintShowers
{
    public class DispatcherHintShower : IDispatcherHintShower, IDisposable
    {
        private readonly Transform _attentionLineTransform;
        private readonly GameObject _transformGivingGameObject;
        private GameObject _notification;

        public DispatcherHintShower()
        {
            _transformGivingGameObject = new GameObject("DispatcherHint_AttentionLineHolder");
            _transformGivingGameObject.SetActive(false);
            _attentionLineTransform = _transformGivingGameObject.transform;
            Main.ModEntry.Logger.Log("DispatcherHintShower instance created.");
        }

        public void SetDispatcherHint(DispatcherHint dispatcherHintOrNull)
        {
            var notificationManager = SingletonBehaviour<ACanvasController<CanvasController.ElementType>>.Instance.NotificationManager;
            if (_notification != null)
            {
                GameObject notificationToClear = _notification;
                _notification = null;
                notificationManager.ClearNotification(notificationToClear);
            }

            if (dispatcherHintOrNull != null)
            {
                if (notificationManager != null)
                {
                    var transform = GetAttentionTransform(dispatcherHintOrNull.AttentionPoint);
                    _notification = notificationManager.ShowNotification(dispatcherHintOrNull.Text, pointAt: transform, localize: false, clearExisting: false);
                }
            }
        }

        private Transform GetAttentionTransform(Vector3? attentionPoint)
        {
            if (attentionPoint == null)
            {
                if (_transformGivingGameObject != null && _transformGivingGameObject.activeSelf)
                {
                    _transformGivingGameObject.SetActive(false);
                }
                return null;
            }
            else
            {
                if (_transformGivingGameObject != null)
                {
                    _attentionLineTransform.position = attentionPoint.Value;
                    if (!_transformGivingGameObject.activeSelf)
                    {
                        _transformGivingGameObject.SetActive(true);
                    }
                    return _attentionLineTransform;
                }
                return null;
            }
        }
        public void Dispose()
        {
            SetDispatcherHint(null);
            if (_transformGivingGameObject != null)
            {
                GameObject.Destroy(_transformGivingGameObject);
            }
        }
    }
}