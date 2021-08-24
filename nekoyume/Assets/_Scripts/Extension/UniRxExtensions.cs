using System;
using System.Collections.Generic;
using System.Numerics;
using Libplanet.Assets;
using Nekoyume.L10n;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    public static class UniRxExtensions
    {
        public static void DisposeAll<T>(this ReactiveProperty<T> property) where T : IDisposable
        {
            property.Value?.Dispose();
            property.Dispose();
        }

        public static void DisposeAll<T>(this ReactiveProperty<List<T>> property)
            where T : IDisposable
        {
            property.Value?.DisposeAllAndClear();
            property.Dispose();
        }

        public static void DisposeAllAndClear<T>(this ReactiveCollection<T> collection)
            where T : IDisposable
        {
            foreach (var item in collection)
            {
                if (!(item is IDisposable disposable))
                {
                    continue;
                }

                disposable.Dispose();
            }

            collection.Dispose();
            collection.Clear();
        }

        public static IDisposable SubscribeTo<T>(this IObservable<T> source, T target) =>
            source.SubscribeWithState(target, (x, t) => t = x);

        public static IDisposable SubscribeTo<T>(this IObservable<T> source, ReactiveProperty<T> reactiveProperty) =>
            source.SubscribeWithState(reactiveProperty, (x, t) => t.Value = x);

        public static IDisposable SubscribeTo(this IObservable<bool> source, GameObject gameObject) =>
            source.SubscribeWithState(gameObject, (x, t) => gameObject.SetActive(x));

        public static IDisposable SubscribeTo(this IObservable<bool> source, Behaviour behaviour) =>
            source.SubscribeWithState(behaviour, (x, t) => behaviour.enabled = x);

        public static IDisposable SubscribeTo(this IObservable<Sprite> source, Image image) =>
            source.SubscribeWithState(image, (x, t) => t.sprite = x);

        public static IDisposable SubscribeTo(this IObservable<int> source, TextMeshProUGUI text) =>
            source.SubscribeWithState(text, (x, t) => t.text = x.ToString());

        public static IDisposable SubscribeTo(this IObservable<string> source, TextMeshProUGUI text) =>
            source.SubscribeWithState(text, (x, t) => t.text = x);

        public static IDisposable SubscribeTo(this IObservable<string> source, SubmitButton text) =>
            source.SubscribeWithState(text, (x, t) => t.SetSubmitText(x));

        public static IDisposable SubscribeToPrice(this IObservable<FungibleAssetValue> source, TextMeshProUGUI text) =>
            source.SubscribeWithState(text, (x, t) => t.text = x.GetQuantityString());

        public static IDisposable SubscribeL10nKeyTo(this IObservable<string> source, TextMeshProUGUI text) =>
            source.SubscribeWithState(
                text,
                (x, t) => t.text = string.IsNullOrEmpty(x) ? x : L10nManager.Localize(x));
    }
}
