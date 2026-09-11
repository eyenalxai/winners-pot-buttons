using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BlackJacket.WinnersPotButtons
{
    /// <summary>
    /// Runtime + / - buttons next to the in-match winners pot.
    /// "+" spawns coins in the pot, "-" removes coins from it.
    /// </summary>
    internal sealed class WinnersPotButtonsOverlay : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _row;
        private Button _plus;
        private Button _minus;
        private bool _errorLogged;

        private void Update()
        {
            try
            {
                Tick();
            }
            catch (Exception e)
            {
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    WinnersPotButtonsPlugin.Log?.LogWarning($"WinnersPotButtons error (further errors suppressed): {e}");
                }
                SetVisible(false);
            }
        }

        private void Tick()
        {
            var cfg = WinnersPotButtonsPlugin.Cfg;
            var gc = GameController.Instance;

            if (cfg == null || !cfg.Enabled.Value || gc == null || gc.UI == null || gc.CurrentMatch == null)
            {
                SetVisible(false);
                return;
            }

            var pot = gc.UI.WinnersPot;
            if (pot == null || !pot.gameObject.activeInHierarchy)
            {
                SetVisible(false);
                return;
            }

            EnsureUi(gc, pot);
            if (_row == null)
            {
                return;
            }

            Reposition(cfg, gc, pot);
            SetVisible(true);
        }

        private void EnsureUi(GameController gc, CoinZone pot)
        {
            if (_row != null && _canvas != null)
            {
                return;
            }

            if (_canvas == null)
            {
                var canvasGo = new GameObject("WinnersPotButtonsCanvas");
                DontDestroyOnLoad(canvasGo);
                _canvas = canvasGo.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 30001;
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            var rowGo = new GameObject("WinnersPotButtonsRow", typeof(RectTransform));
            rowGo.transform.SetParent(_canvas.transform, false);
            _row = (RectTransform)rowGo.transform;
            _row.anchorMin = new Vector2(0.5f, 0.5f);
            _row.anchorMax = new Vector2(0.5f, 0.5f);
            _row.pivot = new Vector2(0.5f, 0.5f);
            _row.sizeDelta = new Vector2(120f, 60f);

            TMP_FontAsset font = null;
            if (gc.UI.WinnersPotCountText != null)
            {
                var countText = gc.UI.WinnersPotCountText.GetComponent<TextMeshProUGUI>();
                if (countText != null)
                {
                    font = countText.font;
                }
            }
            if (font == null && gc.UI.TableValueDisplay != null && gc.UI.TableValueDisplay.PlayerCounter != null
                && gc.UI.TableValueDisplay.PlayerCounter.CountText != null)
            {
                font = gc.UI.TableValueDisplay.PlayerCounter.CountText.font;
            }

            _plus = CreateButton("Plus", "+", new Color(0.24f, 0.58f, 0.31f, 0.92f), font, OnPlus);
            _minus = CreateButton("Minus", "-", new Color(0.68f, 0.27f, 0.27f, 0.92f), font, OnMinus);

            LayoutButtons();
        }

        private Button CreateButton(string name, string label, Color color, TMP_FontAsset font, UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_row, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;

            button.onClick.AddListener(onClick);

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;

            var textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            return button;
        }

        private void LayoutButtons()
        {
            var cfg = WinnersPotButtonsPlugin.Cfg;
            if (cfg == null || _plus == null || _minus == null || _row == null)
            {
                return;
            }

            float size = Mathf.Max(16f, cfg.ButtonSize.Value);
            float spacing = Mathf.Max(0f, cfg.Spacing.Value);
            float fontSize = Mathf.Max(8f, cfg.FontSize.Value);

            var plusRt = (RectTransform)_plus.transform;
            plusRt.sizeDelta = new Vector2(size, size);
            plusRt.anchoredPosition = new Vector2(-(size + spacing) * 0.5f, 0f);

            var minusRt = (RectTransform)_minus.transform;
            minusRt.sizeDelta = new Vector2(size, size);
            minusRt.anchoredPosition = new Vector2((size + spacing) * 0.5f, 0f);

            _row.sizeDelta = new Vector2(size * 2f + spacing, size);

            foreach (var text in _row.GetComponentsInChildren<TextMeshProUGUI>())
            {
                text.fontSize = fontSize;
            }
        }

        private void Reposition(Settings cfg, GameController gc, CoinZone pot)
        {
            if (_row == null)
            {
                return;
            }

            RectTransform anchor = null;
            if (gc.UI.WinnersPotCountText != null)
            {
                anchor = gc.UI.WinnersPotCountText.transform as RectTransform;
            }
            if (anchor == null && pot.CoinRoot != null)
            {
                anchor = pot.CoinRoot;
            }
            if (anchor == null)
            {
                return;
            }

            var anchorCanvas = anchor.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (anchorCanvas != null && anchorCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = anchorCanvas.worldCamera != null ? anchorCanvas.worldCamera : Camera.main;
            }

            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, anchor.position);
            var canvasRt = (RectTransform)_canvas.transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out Vector2 local))
            {
                _row.anchoredPosition = local + new Vector2(cfg.OffsetX.Value, cfg.OffsetY.Value);
            }
        }

        private static CoinZone GetPot()
        {
            var gc = GameController.Instance;
            return gc != null && gc.UI != null ? gc.UI.WinnersPot : null;
        }

        private void OnPlus()
        {
            var pot = GetPot();
            if (pot == null)
            {
                return;
            }

            var cfg = WinnersPotButtonsPlugin.Cfg;
            int amount = Mathf.Max(1, cfg.CoinsPerClick.Value);

            if (!cfg.IgnorePotCap.Value && pot.MaxCoinAmount != -1)
            {
                amount = Mathf.Min(amount, pot.RemainingSpace);
            }
            if (amount <= 0)
            {
                return;
            }

            pot.StartCoroutine(pot.CreateCoinsWithEffect(amount, null, false, CoinAnimator.AudioType.CreateInWinnersPot));
        }

        private void OnMinus()
        {
            var pot = GetPot();
            if (pot == null)
            {
                return;
            }

            var cfg = WinnersPotButtonsPlugin.Cfg;
            int amount = Mathf.Max(1, cfg.CoinsPerClick.Value);

            var coins = pot.AllCoins;
            if (coins == null || coins.Length == 0)
            {
                return;
            }

            var toRemove = new List<CoinVisual>(amount);

            // Prefer removing regular coins, top of the stack first.
            for (int i = coins.Length - 1; i >= 0 && toRemove.Count < amount; i--)
            {
                var coin = coins[i];
                if (coin == null || coin.coinScriptableObject == null)
                {
                    continue;
                }
                if (!coin.coinScriptableObject.IsSoulCoin)
                {
                    toRemove.Add(coin);
                }
            }

            // Only fall back to soul coins if there is nothing else left.
            for (int i = coins.Length - 1; i >= 0 && toRemove.Count < amount; i--)
            {
                var coin = coins[i];
                if (coin == null || coin.coinScriptableObject == null)
                {
                    continue;
                }
                if (coin.coinScriptableObject.IsSoulCoin && !toRemove.Contains(coin))
                {
                    toRemove.Add(coin);
                }
            }

            if (toRemove.Count == 0)
            {
                return;
            }

            pot.StartCoroutine(pot.DestroyCoinsWithAnimation(toRemove.ToArray()));
        }

        private void SetVisible(bool visible)
        {
            if (_row != null && _row.gameObject.activeSelf != visible)
            {
                _row.gameObject.SetActive(visible);
            }
        }
    }
}